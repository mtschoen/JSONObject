/*
Copyright (c) 2010-2021 Matt Schoen

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

#define PRETTY		//Comment out when you no longer need to read JSON to disable pretty Print system-wide
//#define JSONOBJECT_USE_FLOAT	//Use floats for numbers instead of doubles (enable if you don't need support for doubles and want to cut down on significant digits in output)
//#define POOLING	//Currently using a build setting for this one (also it's experimental)

#if UNITY_2 || UNITY_3 || UNITY_4 || UNITY_5 || UNITY_5_3_OR_NEWER
using UnityEngine;
using Debug = UnityEngine.Debug;
#endif
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace Defective.JSON {
	public class JSONObject : IEnumerable {
#if POOLING
		const int MAX_POOL_SIZE = 10000;
		public static Queue<JSONObject> releaseQueue = new Queue<JSONObject>();
#endif

		const int MaxDepth = 100;
		const string Infinity = "Infinity";
		const string NegativeInfinity = "-Infinity";
		const string NaN = "NaN";
		const string Newline = "\r\n";

		const float MaxFrameTime = 0.008f;
		static readonly Stopwatch PrintWatch = new Stopwatch();
		public static readonly char[] Whitespace = { ' ', '\r', '\n', '\t', '\uFEFF', '\u0009' };

		public enum Type {
			NULL,
			STRING,
			NUMBER,
			OBJECT,
			ARRAY,
			BOOL,
			BAKED
		}

		public bool isContainer {
			get { return type == Type.ARRAY || type == Type.OBJECT; }
		}

		public Type type = Type.NULL;

		public int Count {
			get {
				if (list == null)
					return -1;
				return list.Count;
			}
		}

		public List<JSONObject> list;
		public List<string> keys;
		public string str;
#if JSONOBJECT_USE_FLOAT
		public float n;
		public float f {
			get { return n; }
		}
#else
		public double n;
		public float f {
			get {
				return (float)n;
			}
		}
#endif
		public bool useInt;
		public long i;
		public bool b;

		public delegate void AddJSONContents(JSONObject self);

		public static JSONObject nullJO {
			get { return Create(Type.NULL); }
		} //an empty, null object

		public static JSONObject obj {
			get { return Create(Type.OBJECT); }
		} //an empty object

		public static JSONObject arr {
			get { return Create(Type.ARRAY); }
		} //an empty array

		public JSONObject(Type type) {
			this.type = type;
			switch (type) {
				case Type.ARRAY:
					list = new List<JSONObject>();
					break;
				case Type.OBJECT:
					list = new List<JSONObject>();
					keys = new List<string>();
					break;
			}
		}

		public JSONObject(bool value) {
			type = Type.BOOL;
			b = value;
		}

		public JSONObject(float value) {
			type = Type.NUMBER;
			n = value;
		}

		public JSONObject(double value) {
			type = Type.NUMBER;
#if JSONOBJECT_USE_FLOAT
			n = (float)value;
#else
			n = value;
#endif
		}

		public JSONObject(int value) {
			type = Type.NUMBER;
			i = value;
			useInt = true;
			n = value;
		}

		public JSONObject(long value) {
			type = Type.NUMBER;
			i = value;
			useInt = true;
			n = value;
		}

		public JSONObject(Dictionary<string, string> dictionary) {
			type = Type.OBJECT;
			keys = new List<string>();
			list = new List<JSONObject>();
			//Not sure if it's worth removing the foreach here
			foreach (KeyValuePair<string, string> kvp in dictionary) {
				keys.Add(kvp.Key);
				list.Add(CreateStringObject(kvp.Value));
			}
		}

		public JSONObject(Dictionary<string, JSONObject> dictionary) {
			type = Type.OBJECT;
			keys = new List<string>();
			list = new List<JSONObject>();
			//Not sure if it's worth removing the foreach here
			foreach (KeyValuePair<string, JSONObject> kvp in dictionary) {
				keys.Add(kvp.Key);
				list.Add(kvp.Value);
			}
		}

		public JSONObject(AddJSONContents content) {
			content.Invoke(this);
		}

		public JSONObject(JSONObject[] objects) {
			type = Type.ARRAY;
			list = new List<JSONObject>(objects);
		}

		//Convenience function for creating a JSONObject containing a string.  This is not part of the constructor so that malformed JSON data doesn't just turn into a string object
		public static JSONObject StringObject(string value) {
			return CreateStringObject(value);
		}

		public void Absorb(JSONObject jsonObject) {
			list.AddRange(jsonObject.list);
			keys.AddRange(jsonObject.keys);
			str = jsonObject.str;
			n = jsonObject.n;
			useInt = jsonObject.useInt;
			i = jsonObject.i;
			b = jsonObject.b;
			type = jsonObject.type;
		}

		public static JSONObject Create() {
#if POOLING
			JSONObject result = null;
			while(result == null && releaseQueue.Count > 0) {
				result = releaseQueue.Dequeue();
#if DEV
				//The following cases should NEVER HAPPEN (but they do...)
				if(result == null)
					Debug.WriteLine("wtf " + releaseQueue.Count);
				else if(result.list != null)
					Debug.WriteLine("wtflist " + result.list.Count);
#endif
			}
			if(result != null)
				return result;
#endif
			return new JSONObject();
		}

		public static JSONObject Create(Type type) {
			var jsonObject = Create();
			jsonObject.type = type;
			switch (type) {
				case Type.ARRAY:
					jsonObject.list = new List<JSONObject>();
					break;
				case Type.OBJECT:
					jsonObject.list = new List<JSONObject>();
					jsonObject.keys = new List<string>();
					break;
			}

			return jsonObject;
		}

		public static JSONObject Create(bool value) {
			var jsonObject = Create();
			jsonObject.type = Type.BOOL;
			jsonObject.b = value;
			return jsonObject;
		}

		public static JSONObject Create(float value) {
			var jsonObject = Create();
			jsonObject.type = Type.NUMBER;
			jsonObject.n = value;
			return jsonObject;
		}

		public static JSONObject Create(double value) {
			var jsonObject = Create();
			jsonObject.type = Type.NUMBER;
#if JSONOBJECT_USE_FLOAT
			jsonObject.n = (float)value;
#else
			jsonObject.n = value;
#endif
			return jsonObject;
		}

		public static JSONObject Create(int value) {
			var jsonObject = Create();
			jsonObject.type = Type.NUMBER;
			jsonObject.n = value;
			jsonObject.useInt = true;
			jsonObject.i = value;
			return jsonObject;
		}

		public static JSONObject Create(long value) {
			var jsonObject = Create();
			jsonObject.type = Type.NUMBER;
			jsonObject.n = value;
			jsonObject.useInt = true;
			jsonObject.i = value;
			return jsonObject;
		}

		public static JSONObject CreateStringObject(string value) {
			var jsonObject = Create();
			jsonObject.type = Type.STRING;
			jsonObject.str = value;
			return jsonObject;
		}

		public static JSONObject CreateBakedObject(string value) {
			var bakedObject = Create();
			bakedObject.type = Type.BAKED;
			bakedObject.str = value;
			return bakedObject;
		}

		/// <summary>
		/// Create a JSONObject by parsing string data
		/// </summary>
		/// <param name="val">The string to be parsed</param>
		/// <param name="maxDepth">The maximum depth for the parser to search.  Set this to to 1 for the first level,
		/// 2 for the first 2 levels, etc.  It defaults to -2 because -1 is the depth value that is parsed (see below)</param>
		/// <param name="storeExcessLevels">Whether to store levels beyond maxDepth in baked JSONObjects</param>
		/// <param name="strict">Whether to be strict in the parsing. For example, non-strict parsing will successfully
		/// parse "a string" into a string-type </param>
		/// <returns></returns>
		public static JSONObject Create(string val, int maxDepth = -2, bool storeExcessLevels = false,
			bool strict = false) {
			var jsonObject = Create();
			jsonObject.Parse(val, maxDepth, storeExcessLevels, strict);
			return jsonObject;
		}

		public static JSONObject Create(AddJSONContents content) {
			var jsonObject = Create();
			content.Invoke(jsonObject);
			return jsonObject;
		}

		public static JSONObject Create(Dictionary<string, string> dictionary) {
			var jsonObject = Create();
			jsonObject.type = Type.OBJECT;
			jsonObject.keys = new List<string>();
			jsonObject.list = new List<JSONObject>();
			//Not sure if it's worth removing the foreach here
			foreach (KeyValuePair<string, string> kvp in dictionary) {
				jsonObject.keys.Add(kvp.Key);
				jsonObject.list.Add(CreateStringObject(kvp.Value));
			}

			return jsonObject;
		}

		public JSONObject() {
		}

#region PARSE

		public JSONObject(string str, int maxDepth = -2, bool storeExcessLevels = false, bool strict = false) {
			//create a new JSONObject from a string (this will also create any children, and parse the whole string)
			Parse(str, maxDepth, storeExcessLevels, strict);
		}

		void Parse(string inputString, int maxDepth = -2, bool storeExcessLevels = false, bool strict = false) {
			if (!string.IsNullOrEmpty(inputString)) {
				inputString = inputString.Trim(Whitespace);
				if (strict) {
					if (inputString[0] != '[' && inputString[0] != '{') {
						type = Type.NULL;
#if UNITY_2 || UNITY_3 || UNITY_4 || UNITY_5 || UNITY_5_3_OR_NEWER
						Debug.LogWarning
#else
						Debug.WriteLine
#endif
							("Improper (strict) JSON formatting.  First character must be [ or {");
						return;
					}
				}

				if (inputString.Length > 0) {
#if UNITY_WP8 || UNITY_WSA
					if (inputString == "true") {
						type = Type.BOOL;
						b = true;
					} else if (inputString == "false") {
						type = Type.BOOL;
						b = false;
					} else if (inputString == "null") {
						type = Type.NULL;
#else
					if (string.Compare(inputString, "true", true, CultureInfo.InvariantCulture) == 0) {
						type = Type.BOOL;
						b = true;
					} else if (string.Compare(inputString, "false", true, CultureInfo.InvariantCulture) == 0) {
						type = Type.BOOL;
						b = false;
					} else if (string.Compare(inputString, "null", true, CultureInfo.InvariantCulture) == 0) {
						type = Type.NULL;
#endif
#if JSONOBJECT_USE_FLOAT
					} else if (inputString == Infinity) {
						type = Type.NUMBER;
						n = float.PositiveInfinity;
					} else if (inputString == NegativeInfinity) {
						type = Type.NUMBER;
						n = float.NegativeInfinity;
					} else if (inputString == NaN) {
						type = Type.NUMBER;
						n = float.NaN;
#else
					} else if(inputString == Infinity) {
						type = Type.NUMBER;
						n = double.PositiveInfinity;
					} else if(inputString == NegativeInfinity) {
						type = Type.NUMBER;
						n = double.NegativeInfinity;
					} else if(inputString == NaN) {
						type = Type.NUMBER;
						n = double.NaN;
#endif
					} else if (inputString[0] == '"') {
						type = Type.STRING;
						str = UnEscapeString(inputString.Substring(1, inputString.Length - 2));
					} else {
						var tokenTmp = 1;
						/*
						 * Checking for the following formatting (www.json.org)
						 * object - {"field1":value,"field2":value}
						 * array - [value,value,value]
						 * value - string	- "string"
						 *		 - number	- 0.0
						 *		 - bool		- true -or- false
						 *		 - null		- null
						 */
						var offset = 0;
						switch (inputString[offset]) {
							case '{':
								type = Type.OBJECT;
								keys = new List<string>();
								list = new List<JSONObject>();
								break;
							case '[':
								type = Type.ARRAY;
								list = new List<JSONObject>();
								break;
							default:
								try {
#if JSONOBJECT_USE_FLOAT
									n = System.Convert.ToSingle(inputString, CultureInfo.InvariantCulture);
#else
									n = System.Convert.ToDouble(inputString, CultureInfo.InvariantCulture);
#endif
									if (!inputString.Contains(".")) {
										i = System.Convert.ToInt64(inputString, CultureInfo.InvariantCulture);
										useInt = true;
									}

									type = Type.NUMBER;
								} catch (System.FormatException) {
									type = Type.NULL;
#if UNITY_2 || UNITY_3 || UNITY_4 || UNITY_5 || UNITY_5_3_OR_NEWER
									Debug.LogWarning
#else
									Debug.WriteLine
#endif
										("improper JSON formatting:" + inputString);
								} catch (System.OverflowException) {
									type = Type.NUMBER;
									n = inputString.StartsWith("-")
										?
#if JSONOBJECT_USE_FLOAT
										float.NegativeInfinity
										: float.PositiveInfinity;
#else
										double.NegativeInfinity
										: double.PositiveInfinity;
#endif
								}

								return;
						}

						var propName = "";
						var openQuote = false;
						var inProp = false;
						var depth = 0;
						while (++offset < inputString.Length) {
							if (System.Array.IndexOf(Whitespace, inputString[offset]) > -1)
								continue;

							if (inputString[offset] == '\\') {
								offset += 1;
								continue;
							}

							if (inputString[offset] == '"') {
								if (openQuote) {
									if (!inProp && depth == 0 && type == Type.OBJECT)
										propName = inputString.Substring(tokenTmp + 1, offset - tokenTmp - 1);
									openQuote = false;
								} else {
									if (depth == 0 && type == Type.OBJECT)
										tokenTmp = offset;
									openQuote = true;
								}
							}

							if (openQuote)
								continue;

							if (type == Type.OBJECT && depth == 0) {
								if (inputString[offset] == ':') {
									tokenTmp = offset + 1;
									inProp = true;
								}
							}

							if (inputString[offset] == '[' || inputString[offset] == '{') {
								depth++;
							} else if (inputString[offset] == ']' || inputString[offset] == '}') {
								depth--;
							}

							//if  (encounter a ',' at top level)  || a closing ]/}
							if (inputString[offset] == ',' && depth == 0 || depth < 0) {
								inProp = false;
								var inner = inputString.Substring(tokenTmp, offset - tokenTmp).Trim(Whitespace);
								if (inner.Length > 0) {
									if (type == Type.OBJECT)
										keys.Add(propName);
									if (maxDepth != -1) //maxDepth of -1 is the end of the line
										list.Add(Create(inner, (maxDepth < -1) ? -2 : maxDepth - 1, storeExcessLevels));
									else if (storeExcessLevels)
										list.Add(CreateBakedObject(inner));

								}

								tokenTmp = offset + 1;
							}
						}
					}
				} else type = Type.NULL;
			} else type = Type.NULL; //If the string is missing, this is a null
			//Profiler.EndSample();
		}

#endregion

		public bool IsNumber {
			get { return type == Type.NUMBER; }
		}

		public bool IsNull {
			get { return type == Type.NULL; }
		}

		public bool IsString {
			get { return type == Type.STRING; }
		}

		public bool IsBool {
			get { return type == Type.BOOL; }
		}

		public bool IsArray {
			get { return type == Type.ARRAY; }
		}

		public bool IsObject {
			get { return type == Type.OBJECT || type == Type.BAKED; }
		}

		public void Add(bool value) {
			Add(Create(value));
		}

		public void Add(float value) {
			Add(Create(value));
		}

		public void Add(double value) {
			Add(Create(value));
		}

		public void Add(long value) {
			Add(Create(value));
		}

		public void Add(int value) {
			Add(Create(value));
		}

		public void Add(string value) {
			Add(CreateStringObject(value));
		}

		public void Add(AddJSONContents content) {
			Add(Create(content));
		}

		public void Add(JSONObject jsonObject) {
			if (jsonObject) {
				//Don't do anything if the object is null
				if (type != Type.ARRAY) {
					type = Type.ARRAY; //Congratulations, son, you're an ARRAY now
					if (list == null)
						list = new List<JSONObject>();
				}

				list.Add(jsonObject);
			}
		}

		public void AddField(string name, bool value) {
			AddField(name, Create(value));
		}

		public void AddField(string name, float value) {
			AddField(name, Create(value));
		}

		public void AddField(string name, double value) {
			AddField(name, Create(value));
		}

		public void AddField(string name, int value) {
			AddField(name, Create(value));
		}

		public void AddField(string name, long value) {
			AddField(name, Create(value));
		}

		public void AddField(string name, AddJSONContents content) {
			AddField(name, Create(content));
		}

		public void AddField(string name, string value) {
			AddField(name, CreateStringObject(value));
		}

		public void AddField(string name, JSONObject jsonObject) {
			if (jsonObject) {
				//Don't do anything if the object is null
				if (type != Type.OBJECT) {
					if (keys == null)
						keys = new List<string>();

					if (type == Type.ARRAY) {
						for (var index = 0; index < list.Count; index++)
							keys.Add(index.ToString(CultureInfo.InvariantCulture));
					} else if (list == null)
						list = new List<JSONObject>();

					type = Type.OBJECT; //Congratulations, son, you're an OBJECT now
				}

				keys.Add(name);
				list.Add(jsonObject);
			}
		}

		public void SetField(string name, string value) {
			SetField(name, CreateStringObject(value));
		}

		public void SetField(string name, bool value) {
			SetField(name, Create(value));
		}

		public void SetField(string name, float value) {
			SetField(name, Create(value));
		}

		public void SetField(string name, double value) {
			SetField(name, Create(value));
		}

		public void SetField(string name, long value) {
			SetField(name, Create(value));
		}

		public void SetField(string name, int value) {
			SetField(name, Create(value));
		}

		public void SetField(string name, JSONObject jsonObject) {
			if (HasField(name)) {
				list.Remove(this[name]);
				keys.Remove(name);
			}

			AddField(name, jsonObject);
		}

		public void RemoveField(string name) {
			if (keys.IndexOf(name) > -1) {
				list.RemoveAt(keys.IndexOf(name));
				keys.Remove(name);
			}
		}

		public delegate void FieldNotFound(string name);

		public delegate void GetFieldResponse(JSONObject jsonObject);

		public bool GetField(out bool field, string name, bool fallback) {
			field = fallback;
			return GetField(ref field, name);
		}

		public bool GetField(ref bool field, string name, FieldNotFound fail = null) {
			if (type == Type.OBJECT) {
				var index = keys.IndexOf(name);
				if (index >= 0) {
					field = list[index].b;
					return true;
				}
			}

			if (fail != null) fail.Invoke(name);
			return false;
		}

		public bool GetField(out double field, string name, double fallback) {
			field = fallback;
			return GetField(ref field, name);
		}

		public bool GetField(ref double field, string name, FieldNotFound fail = null) {
			if (type == Type.OBJECT) {
				var index = keys.IndexOf(name);
				if (index >= 0) {
					field = list[index].n;
					return true;
				}
			}

			if (fail != null) fail.Invoke(name);
			return false;
		}

		public bool GetField(out float field, string name, float fallback) {
			field = fallback;
			return GetField(ref field, name);
		}

		public bool GetField(ref float field, string name, FieldNotFound fail = null) {
			if (type == Type.OBJECT) {
				var index = keys.IndexOf(name);
				if (index >= 0) {
#if JSONOBJECT_USE_FLOAT
					field = list[index].n;
#else
					field = (float) list[index].n;
#endif
					return true;
				}
			}

			if (fail != null) fail.Invoke(name);
			return false;
		}

		public bool GetField(out int field, string name, int fallback) {
			field = fallback;
			return GetField(ref field, name);
		}

		public bool GetField(ref int field, string name, FieldNotFound fail = null) {
			if (IsObject) {
				var index = keys.IndexOf(name);
				if (index >= 0) {
					field = (int) list[index].i;
					return true;
				}
			}

			if (fail != null) fail.Invoke(name);
			return false;
		}

		public bool GetField(out long field, string name, long fallback) {
			field = fallback;
			return GetField(ref field, name);
		}

		public bool GetField(ref long field, string name, FieldNotFound fail = null) {
			if (IsObject) {
				var index = keys.IndexOf(name);
				if (index >= 0) {
					field = list[index].i;
					return true;
				}
			}

			if (fail != null) fail.Invoke(name);
			return false;
		}

		public bool GetField(out uint field, string name, uint fallback) {
			field = fallback;
			return GetField(ref field, name);
		}

		public bool GetField(ref uint field, string name, FieldNotFound fail = null) {
			if (IsObject) {
				var index = keys.IndexOf(name);
				if (index >= 0) {
					field = (uint) list[index].i;
					return true;
				}
			}

			if (fail != null) fail.Invoke(name);
			return false;
		}

		public bool GetField(out string field, string name, string fallback) {
			field = fallback;
			return GetField(ref field, name);
		}

		public bool GetField(ref string field, string name, FieldNotFound fail = null) {
			if (IsObject) {
				var index = keys.IndexOf(name);
				if (index >= 0) {
					field = list[index].str;
					return true;
				}
			}

			if (fail != null) fail.Invoke(name);
			return false;
		}

		public void GetField(string name, GetFieldResponse response, FieldNotFound fail = null) {
			if (response != null && IsObject) {
				var index = keys.IndexOf(name);
				if (index >= 0) {
					response.Invoke(list[index]);
					return;
				}
			}

			if (fail != null) fail.Invoke(name);
		}

		public JSONObject GetField(string name) {
			if (IsObject)
				for (var index = 0; index < keys.Count; index++)
					if (keys[index] == name)
						return list[index];
			return null;
		}

		public bool HasFields(string[] names) {
			if (!IsObject)
				return false;

			foreach (var name in names)
				if (!keys.Contains(name))
					return false;

			return true;
		}

		public bool HasField(string name) {
			if (!IsObject)
				return false;

			foreach (var fieldName in keys)
				if (fieldName == name)
					return true;

			return false;
		}

		public void Clear() {
			type = Type.NULL;
			if (list != null)
				list.Clear();

			if (keys != null)
				keys.Clear();

			str = "";
			n = 0;
			b = false;
		}

		/// <summary>
		/// Copy a JSONObject. This could be more efficient
		/// </summary>
		/// <returns></returns>
		public JSONObject Copy() {
			return Create(Print());
		}

		/*
		 * The Merge function is experimental. Use at your own risk.
		 */
		public void Merge(JSONObject jsonObject) {
			MergeRecur(this, jsonObject);
		}

		/// <summary>
		/// Merge object right into left recursively
		/// </summary>
		/// <param name="left">The left (base) object</param>
		/// <param name="right">The right (new) object</param>
		static void MergeRecur(JSONObject left, JSONObject right) {
			if (left.type == Type.NULL) {
				left.Absorb(right);
			} else if (left.type == Type.OBJECT && right.type == Type.OBJECT) {
				for (var i = 0; i < right.list.Count; i++) {
					var key = right.keys[i];
					if (right[i].isContainer) {
						if (left.HasField(key))
							MergeRecur(left[key], right[i]);
						else
							left.AddField(key, right[i]);
					} else {
						if (left.HasField(key))
							left.SetField(key, right[i]);
						else
							left.AddField(key, right[i]);
					}
				}
			} else if (left.type == Type.ARRAY && right.type == Type.ARRAY) {
				if (right.Count > left.Count) {
#if UNITY_2 || UNITY_3 || UNITY_4 || UNITY_5 || UNITY_5_3_OR_NEWER
					Debug.LogError
#else
				Debug.WriteLine
#endif
						("Cannot merge arrays when right object has more elements");
					return;
				}

				for (var i = 0; i < right.list.Count; i++) {
					if (left[i].type == right[i].type) {
						//Only overwrite with the same type
						if (left[i].isContainer)
							MergeRecur(left[i], right[i]);
						else {
							left[i] = right[i];
						}
					}
				}
			}
		}

		public void Bake() {
			if (type != Type.BAKED) {
				str = Print();
				type = Type.BAKED;
			}
		}

		public IEnumerable BakeAsync() {
			if (type != Type.BAKED) {
				foreach (var s in PrintAsync()) {
					if (s == null)
						yield return null;
					else
						str = s;
				}

				type = Type.BAKED;
			}
		}
#pragma warning disable 219
		public string Print(bool pretty = false) {
			var builder = new StringBuilder();
			Stringify(0, builder, pretty);
			return builder.ToString();
		}

		static string EscapeString(string input) {
			var escaped = input.Replace("\\", "\\\\");
			escaped = escaped.Replace("\b", "\\b");
			escaped = escaped.Replace("\f", "\\f");
			escaped = escaped.Replace("\n", "\\n");
			escaped = escaped.Replace("\r", "\\r");
			escaped = escaped.Replace("\t", "\\t");
			escaped = escaped.Replace("\"", "\\\"");
			return escaped;
		}

		static string UnEscapeString(string input) {
			var escaped = input.Replace("\\\"", "\"");
			escaped = escaped.Replace("\\b", "\b");
			escaped = escaped.Replace("\\f", "\f");
			escaped = escaped.Replace("\\n", "\n");
			escaped = escaped.Replace("\\r", "\r");
			escaped = escaped.Replace("\\t", "\t");
			escaped = escaped.Replace("\\\\", "\\");
			return escaped;
		}

		public IEnumerable<string> PrintAsync(bool pretty = false) {
			var builder = new StringBuilder();
			PrintWatch.Reset();
			PrintWatch.Start();
			var enumerator = StringifyAsync(0, builder, pretty).GetEnumerator();
			while (enumerator.MoveNext()){
				yield return null;
			}

			yield return builder.ToString();
		}
#pragma warning restore 219

#region STRINGIFY

		IEnumerable StringifyAsync(int depth, StringBuilder builder, bool pretty = false) {
			//Convert the JSONObject into a string
			//Profiler.BeginSample("JSONprint");
			if (depth++ > MaxDepth) {
#if UNITY_2 || UNITY_3 || UNITY_4 || UNITY_5 || UNITY_5_3_OR_NEWER
				Debug.Log
#else
				Debug.WriteLine
#endif
					("reached max depth!");

				yield break;
			}

			if (PrintWatch.Elapsed.TotalSeconds > MaxFrameTime) {
				PrintWatch.Reset();
				yield return null;
				PrintWatch.Start();
			}

			switch (type) {
				case Type.BAKED:
					builder.Append(str);
					break;
				case Type.STRING:
					builder.AppendFormat("\"{0}\"", EscapeString(str));
					break;
				case Type.NUMBER:
					if (useInt) {
						builder.Append(i.ToString(CultureInfo.InvariantCulture));
					} else {
#if JSONOBJECT_USE_FLOAT
						if (float.IsNegativeInfinity(n))
							builder.Append(NegativeInfinity);
						else if (float.IsInfinity(n))
							builder.Append(Infinity);
						else if (float.IsNaN(n))
							builder.Append(NaN);
#else
						if (double.IsNegativeInfinity(n))
							builder.Append(NegativeInfinity);
						else if (double.IsInfinity(n))
							builder.Append(Infinity);
						else if (double.IsNaN(n))
							builder.Append(NaN);
#endif
						else
							builder.Append(n.ToString("R", CultureInfo.InvariantCulture));
					}

					break;
				case Type.OBJECT:
					builder.Append("{");
					if (list.Count > 0) {
#if (PRETTY) //for a bit more readability, comment the define above to disable system-wide
						if (pretty)
							builder.Append(Newline);
#endif
						for (var index = 0; index < list.Count; index++) {
							var key = keys[index];
							var jsonObject = list[index];
							if (jsonObject) {
#if (PRETTY)
								if (pretty)
									for (var j = 0; j < depth; j++)
										builder.Append("\t"); //for a bit more readability
#endif
								builder.AppendFormat("\"{0}\":", key);
								foreach (IEnumerable e in jsonObject.StringifyAsync(depth, builder, pretty))
									yield return e;

								builder.Append(",");
#if (PRETTY)
								if (pretty)
									builder.Append(Newline);
#endif
							}
						}
#if (PRETTY)
						if (pretty)
							builder.Length -= 2;
						else
#endif
							builder.Length--;
					}
#if (PRETTY)
					if (pretty && list.Count > 0) {
						builder.Append(Newline);
						for (var j = 0; j < depth - 1; j++)
							builder.Append("\t"); //for a bit more readability
					}
#endif
					builder.Append("}");
					break;
				case Type.ARRAY:
					builder.Append("[");
					if (list.Count > 0) {
#if (PRETTY)
						if (pretty)
							builder.Append(Newline); //for a bit more readability
#endif
						for (var index = 0; index < list.Count; index++) {
							if (list[index]) {
#if (PRETTY)
								if (pretty)
									for (var j = 0; j < depth; j++)
										builder.Append("\t"); //for a bit more readability
#endif
								foreach (IEnumerable e in list[index].StringifyAsync(depth, builder, pretty))
									yield return e;
								builder.Append(",");
#if (PRETTY)
								if (pretty)
									builder.Append(Newline); //for a bit more readability
#endif
							}
						}
#if (PRETTY)
						if (pretty)
							builder.Length -= 2;
						else
#endif
							builder.Length--;
					}
#if (PRETTY)
					if (pretty && list.Count > 0) {
						builder.Append(Newline);
						for (var j = 0; j < depth - 1; j++)
							builder.Append("\t"); //for a bit more readability
					}
#endif
					builder.Append("]");
					break;
				case Type.BOOL:
					if (b)
						builder.Append("true");
					else
						builder.Append("false");
					break;
				case Type.NULL:
					builder.Append("null");
					break;
			}
			//Profiler.EndSample();
		}

		//TODO: Refactor Stringify functions to share core logic
		/*
		 * I know, I know, this is really bad form.  It turns out that there is a
		 * significant amount of garbage created when calling as a coroutine, so this
		 * method is duplicated.  Hopefully there won't be too many future changes, but
		 * I would still like a more elegant way to optionaly yield
		 */
		void Stringify(int depth, StringBuilder builder, bool pretty = false) {
			//Convert the JSONObject into a string
			//Profiler.BeginSample("JSONprint");
			if (depth++ > MaxDepth) {
#if UNITY_2 || UNITY_3 || UNITY_4 || UNITY_5 || UNITY_5_3_OR_NEWER
				Debug.Log
#else
			Debug.WriteLine
#endif
					("reached max depth!");
				return;
			}

			switch (type) {
				case Type.BAKED:
					builder.Append(str);
					break;
				case Type.STRING:
					builder.AppendFormat("\"{0}\"", EscapeString(str));
					break;
				case Type.NUMBER:
					if (useInt) {
						builder.Append(i.ToString(CultureInfo.InvariantCulture));
					} else {
#if JSONOBJECT_USE_FLOAT
						if (float.IsNegativeInfinity(n))
							builder.Append(NegativeInfinity);
						else if (float.IsInfinity(n))
							builder.Append(Infinity);
						else if (float.IsNaN(n))
							builder.Append(NaN);
#else
						if (double.IsNegativeInfinity(n))
							builder.Append(NegativeInfinity);
						else if (double.IsInfinity(n))
							builder.Append(Infinity);
						else if (double.IsNaN(n))
							builder.Append(NaN);
#endif
						else
							builder.Append(n.ToString("R", CultureInfo.InvariantCulture));
					}

					break;
				case Type.OBJECT:
					builder.Append("{");
					if (list.Count > 0) {
#if (PRETTY) //for a bit more readability, comment the define above to disable system-wide
						if (pretty)
							builder.Append("\n");
#endif
						for (var index = 0; index < list.Count; index++) {
							var key = keys[index];
							JSONObject jsonObject = list[index];
							if (jsonObject) {
#if (PRETTY)
								if (pretty)
									for (var j = 0; j < depth; j++)
										builder.Append("\t"); //for a bit more readability
#endif
								builder.AppendFormat("\"{0}\":", key);
								jsonObject.Stringify(depth, builder, pretty);
								builder.Append(",");
#if (PRETTY)
								if (pretty)
									builder.Append("\n");
#endif
							}
						}
#if (PRETTY)
						if (pretty)
							builder.Length -= 2;
						else
#endif
							builder.Length--;
					}
#if (PRETTY)
					if (pretty && list.Count > 0) {
						builder.Append("\n");
						for (var j = 0; j < depth - 1; j++)
							builder.Append("\t"); //for a bit more readability
					}
#endif
					builder.Append("}");
					break;
				case Type.ARRAY:
					builder.Append("[");
					if (list.Count > 0) {
#if (PRETTY)
						if (pretty)
							builder.Append("\n"); //for a bit more readability
#endif
						foreach (var jsonObject in list)
						{
							if (jsonObject) {
#if (PRETTY)
								if (pretty)
									for (var j = 0; j < depth; j++)
										builder.Append("\t"); //for a bit more readability
#endif
								jsonObject.Stringify(depth, builder, pretty);
								builder.Append(",");
#if (PRETTY)
								if (pretty)
									builder.Append("\n"); //for a bit more readability
#endif
							}
						}
#if (PRETTY)
						if (pretty)
							builder.Length -= 2;
						else
#endif
							builder.Length--;
					}
#if (PRETTY)
					if (pretty && list.Count > 0) {
						builder.Append("\n");
						for (var j = 0; j < depth - 1; j++)
							builder.Append("\t"); //for a bit more readability
					}
#endif
					builder.Append("]");
					break;
				case Type.BOOL:
					if (b)
						builder.Append("true");
					else
						builder.Append("false");
					break;
				case Type.NULL:
					builder.Append("null");
					break;
			}
			//Profiler.EndSample();
		}

#endregion

#if UNITY_2 || UNITY_3 || UNITY_4 || UNITY_5 || UNITY_5_3_OR_NEWER
		public static implicit operator WWWForm(JSONObject jsonObject) {
			WWWForm form = new WWWForm();
			for (var i = 0; i < jsonObject.list.Count; i++) {
				var key = i.ToString(CultureInfo.InvariantCulture);
				if (jsonObject.type == Type.OBJECT)
					key = jsonObject.keys[i];
				var val = jsonObject.list[i].ToString();
				if (jsonObject.list[i].type == Type.STRING)
					val = val.Replace("\"", "");
				form.AddField(key, val);
			}

			return form;
		}
#endif
		public JSONObject this[int index] {
			get {
				if (list.Count > index) return list[index];
				return null;
			}
			set {
				if (list.Count > index)
					list[index] = value;
			}
		}

		public JSONObject this[string index] {
			get { return GetField(index); }
			set { SetField(index, value); }
		}

		public override string ToString() {
			return Print();
		}

		public string ToString(bool pretty) {
			return Print(pretty);
		}

		public Dictionary<string, string> ToDictionary() {
			if (type == Type.OBJECT) {
				var result = new Dictionary<string, string>();
				for (var index = 0; index < list.Count; index++) {
					var val = list[index];
					switch (val.type) {
						case Type.STRING:
							result.Add(keys[index], val.str);
							break;
						case Type.NUMBER:
							result.Add(keys[index], val.n.ToString(CultureInfo.InvariantCulture));
							break;
						case Type.BOOL:
							result.Add(keys[index], val.b.ToString(CultureInfo.InvariantCulture));
							break;
						default:
#if UNITY_2 || UNITY_3 || UNITY_4 || UNITY_5 || UNITY_5_3_OR_NEWER
							Debug.LogWarning
#else
						Debug.WriteLine
#endif
								("Omitting object: " + keys[index] + " in dictionary conversion");
							break;
					}
				}

				return result;
			}
#if UNITY_2 || UNITY_3 || UNITY_4 || UNITY_5 || UNITY_5_3_OR_NEWER
			Debug.Log
#else
		Debug.WriteLine
#endif
				("Tried to turn non-Object JSONObject into a dictionary");
			return null;
		}

		public static implicit operator bool(JSONObject o) {
			return o != null;
		}
#if POOLING
	static bool pool = true;
	public static void ClearPool() {
		pool = false;
		releaseQueue.Clear();
		pool = true;
	}

	~JSONObject() {
		if(pool && releaseQueue.Count < MAX_POOL_SIZE) {
			type = Type.NULL;
			list = null;
			keys = null;
			str = "";
			n = 0;
			b = false;
			releaseQueue.Enqueue(this);
		}
	}
#endif

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

		public JSONObjectEnumerator GetEnumerator() {
			return new JSONObjectEnumerator(this);
		}
	}

	public class JSONObjectEnumerator : IEnumerator {
		public JSONObject target;

		// Enumerators are positioned before the first element
		// until the first MoveNext() call.
		int position = -1;

		public JSONObjectEnumerator(JSONObject jsonObject) {
			Debug.Assert(jsonObject.isContainer); //must be an array or object to iterate
			target = jsonObject;
		}

		public bool MoveNext() {
			position++;
			return (position < target.Count);
		}

		public void Reset() {
			position = -1;
		}

		object IEnumerator.Current {
			get { return Current; }
		}

		public JSONObject Current {
			get {
				if (target.IsArray) {
					return target[position];
				} else {
					var key = target.keys[position];
					return target[key];
				}
			}
		}
	}
}