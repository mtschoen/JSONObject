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

//#define JSONOBJECT_DISABLE_PRETTY_PRINT // Use when you no longer need to read JSON to disable pretty Print system-wide
//#define JSONOBJECT_USE_FLOAT //Use floats for numbers instead of doubles (enable if you don't need support for doubles and want to cut down on significant digits in output)
//#define JSONOBJECT_POOLING //Create JSONObjects from a pool and prevent finalization by returning objects to the pool

#if UNITY_2 || UNITY_3 || UNITY_4 || UNITY_5 || UNITY_5_3_OR_NEWER
#define USING_UNITY
#endif

using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

#if USING_UNITY
using UnityEngine;
using Debug = UnityEngine.Debug;
#endif

namespace Defective.JSON {
	public class JSONObject : IEnumerable {
#if JSONOBJECT_POOLING
		const int MaxPoolSize = 10000;
		static readonly Queue<JSONObject> Pool = new Queue<JSONObject>();
		static bool poolingEnabled = true;

		bool isPooled;
#endif

#if !JSONOBJECT_DISABLE_PRETTY_PRINT
		const string Newline = "\r\n";
		const string Tab = "\t";
#endif

		const string Infinity = "Infinity";
		const string NegativeInfinity = "-Infinity";
		const string NaN = "NaN";
		const string True = "true";
		const string False = "false";
		const string Null = "null";

		const float MaxFrameTime = 0.008f;
		static readonly Stopwatch PrintWatch = new Stopwatch();
		public static readonly char[] Whitespace = { ' ', '\r', '\n', '\t', '\uFEFF', '\u0009' };

		public enum Type {
			Null,
			String,
			Number,
			Object,
			Array,
			Bool,
			Baked
		}

		public bool isContainer {
			get { return type == Type.Array || type == Type.Object; }
		}

		public Type type = Type.Null;

		public int count {
			get {
				if (list == null)
					return -1;

				return list.Count;
			}
		}

		public List<JSONObject> list;
		public List<string> keys;
		public string stringValue;
		public bool isInteger;
		public long longValue;
		public bool boolValue;
#if JSONOBJECT_USE_FLOAT
		public float floatValue;
		public double doubleValue {
			get {
				return floatValue;
			}
			set {
				floatValue = (float) value;
			}
		}
#else
		public double doubleValue;
		public float floatValue {
			get {
				return (float) doubleValue;
			}
			set {
				doubleValue = value;
			}
		}
#endif

		public int intValue {
			get {
				return (int) longValue;
			}
			set {
				longValue = value;
			}
		}

		public delegate void AddJSONContents(JSONObject self);

		public static JSONObject nullObject {
			get { return Create(Type.Null); }
		}

		public static JSONObject emptyObject {
			get { return Create(Type.Object); }
		}

		public static JSONObject emptyArray {
			get { return Create(Type.Array); }
		}

		public JSONObject(Type type) {
			this.type = type;
			switch (type) {
				case Type.Array:
					list = new List<JSONObject>();
					break;
				case Type.Object:
					list = new List<JSONObject>();
					keys = new List<string>();
					break;
			}
		}

		public JSONObject(bool value) {
			type = Type.Bool;
			boolValue = value;
		}

		public JSONObject(float value) {
			type = Type.Number;
#if JSONOBJECT_USE_FLOAT
			floatValue = value;
#else
			doubleValue = value;
#endif
		}

		public JSONObject(double value) {
			type = Type.Number;
#if JSONOBJECT_USE_FLOAT
			floatValue = (float)value;
#else
			doubleValue = value;
#endif
		}

		public JSONObject(int value) {
			type = Type.Number;
			longValue = value;
			isInteger = true;
#if JSONOBJECT_USE_FLOAT
			floatValue = value;
#else
			doubleValue = value;
#endif
		}

		public JSONObject(long value) {
			type = Type.Number;
			longValue = value;
			isInteger = true;
#if JSONOBJECT_USE_FLOAT
			floatValue = value;
#else
			doubleValue = value;
#endif
		}

		public JSONObject(Dictionary<string, string> dictionary) {
			type = Type.Object;
			keys = new List<string>();
			list = new List<JSONObject>();
			foreach (KeyValuePair<string, string> kvp in dictionary) {
				keys.Add(kvp.Key);
				list.Add(CreateStringObject(kvp.Value));
			}
		}

		public JSONObject(Dictionary<string, JSONObject> dictionary) {
			type = Type.Object;
			keys = new List<string>();
			list = new List<JSONObject>();
			foreach (KeyValuePair<string, JSONObject> kvp in dictionary) {
				keys.Add(kvp.Key);
				list.Add(kvp.Value);
			}
		}

		public JSONObject(AddJSONContents content) {
			content.Invoke(this);
		}

		public JSONObject(JSONObject[] objects) {
			type = Type.Array;
			list = new List<JSONObject>(objects);
		}

		/// <summary>
		/// Convenience function for creating a JSONObject containing a string.
		/// This is not part of the constructor so that malformed JSON data doesn't just turn into a string object
		/// </summary>
		/// <param name="value">The string value for the new JSONObject</param>
		/// <returns>Thew new JSONObject</returns>
		public static JSONObject StringObject(string value) {
			return CreateStringObject(value);
		}

		public void Absorb(JSONObject jsonObject) {
			list.AddRange(jsonObject.list);
			keys.AddRange(jsonObject.keys);
			stringValue = jsonObject.stringValue;
#if JSONOBJECT_USE_FLOAT
			floatValue = jsonObject.floatValue;
#else
			doubleValue = jsonObject.doubleValue;
#endif

			isInteger = jsonObject.isInteger;
			longValue = jsonObject.longValue;
			boolValue = jsonObject.boolValue;
			type = jsonObject.type;
		}

		public static JSONObject Create() {
#if JSONOBJECT_POOLING
			lock (Pool) {
				if (Pool.Count > 0) {
					var result = Pool.Dequeue();

					result.isPooled = false;
					return result;
				}
			}
#endif

			return new JSONObject();
		}

		public static JSONObject Create(Type type) {
			var jsonObject = Create();
			jsonObject.type = type;
			switch (type) {
				case Type.Array:
					jsonObject.list = new List<JSONObject>();
					break;
				case Type.Object:
					jsonObject.list = new List<JSONObject>();
					jsonObject.keys = new List<string>();
					break;
			}

			return jsonObject;
		}

		public static JSONObject Create(bool value) {
			var jsonObject = Create();
			jsonObject.type = Type.Bool;
			jsonObject.boolValue = value;
			return jsonObject;
		}

		public static JSONObject Create(float value) {
			var jsonObject = Create();
			jsonObject.type = Type.Number;
#if JSONOBJECT_USE_FLOAT
			jsonObject.floatValue = value;
#else
			jsonObject.doubleValue = value;
#endif

			return jsonObject;
		}

		public static JSONObject Create(double value) {
			var jsonObject = Create();
			jsonObject.type = Type.Number;
#if JSONOBJECT_USE_FLOAT
			jsonObject.floatValue = (float)value;
#else
			jsonObject.doubleValue = value;
#endif

			return jsonObject;
		}

		public static JSONObject Create(int value) {
			var jsonObject = Create();
			jsonObject.type = Type.Number;
			jsonObject.isInteger = true;
			jsonObject.longValue = value;
#if JSONOBJECT_USE_FLOAT
			jsonObject.floatValue = value;
#else
			jsonObject.doubleValue = value;
#endif

			return jsonObject;
		}

		public static JSONObject Create(long value) {
			var jsonObject = Create();
			jsonObject.type = Type.Number;
			jsonObject.isInteger = true;
			jsonObject.longValue = value;
#if JSONOBJECT_USE_FLOAT
			jsonObject.floatValue = value;
#else
			jsonObject.doubleValue = value;
#endif

			return jsonObject;
		}

		public static JSONObject CreateStringObject(string value) {
			var jsonObject = Create();
			jsonObject.type = Type.String;
			jsonObject.stringValue = value;
			return jsonObject;
		}

		public static JSONObject CreateBakedObject(string value) {
			var bakedObject = Create();
			bakedObject.type = Type.Baked;
			bakedObject.stringValue = value;
			return bakedObject;
		}

		/// <summary>
		/// Create a JSONObject (using pooling if enabled) using a string containing valid JSON
		/// </summary>
		/// <param name="jsonString">A string containing valid JSON to be parsed into objects</param>
		/// <param name="maxDepth">The maximum depth for the parser to search.  Set this to to 1 for the first level,
		/// 2 for the first 2 levels, etc.  It defaults to -2 because -1 is the depth value that is parsed (see below)</param>
		/// <param name="storeExcessLevels">Whether to store levels beyond maxDepth in baked JSONObjects</param>
		/// <param name="strict">Whether to be strict in the parsing. For example, non-strict parsing will successfully
		/// parse "a string" into a string-type </param>
		/// <returns>A JSONObject containing the parsed data</returns>
		public static JSONObject Create(string jsonString, int maxDepth = -2, bool storeExcessLevels = false,
			bool strict = false) {
			var jsonObject = Create();
			jsonObject.Parse(jsonString, maxDepth, storeExcessLevels, strict);
			return jsonObject;
		}

		public static JSONObject Create(AddJSONContents content) {
			var jsonObject = Create();
			content.Invoke(jsonObject);
			return jsonObject;
		}

		public static JSONObject Create(Dictionary<string, string> dictionary) {
			var jsonObject = Create();
			jsonObject.type = Type.Object;
			jsonObject.keys = new List<string>();
			jsonObject.list = new List<JSONObject>();
			foreach (var kvp in dictionary) {
				jsonObject.keys.Add(kvp.Key);
				jsonObject.list.Add(CreateStringObject(kvp.Value));
			}

			return jsonObject;
		}

		/// <summary>
		/// Create a JSONObject (using pooling if enabled) using a string containing valid JSON
		/// </summary>
		/// <param name="jsonString">A string containing valid JSON to be parsed into objects</param>
		/// <param name="maxDepth">The maximum depth for the parser to search.  Set this to to 1 for the first level,
		/// 2 for the first 2 levels, etc.  It defaults to -2 because -1 is the depth value that is parsed (see below)</param>
		/// <param name="storeExcessLevels">Whether to store levels beyond maxDepth in baked JSONObjects</param>
		/// <param name="strict">Whether to be strict in the parsing. For example, non-strict parsing will successfully
		/// parse "a string" into a string-type </param>
		/// <returns>A JSONObject containing the parsed data</returns>
		public static IEnumerable<JSONObject> CreateAsync(string jsonString, int maxDepth = -2, bool storeExcessLevels = false,
			bool strict = false) {
			var jsonObject = Create();
			PrintWatch.Reset();
			PrintWatch.Start();
			foreach (var e in jsonObject.ParseAsync(jsonString, maxDepth, storeExcessLevels, strict)) {
				yield return e;
			}

			yield return jsonObject;
		}

		public JSONObject() { }

		/// <summary>
		/// Construct a new JSONObject using a string containing valid JSON
		/// </summary>
		/// <param name="jsonString">A string containing valid JSON to be parsed into objects</param>
		/// <param name="maxDepth">The maximum depth for the parser to search.  Set this to to 1 for the first level,
		/// 2 for the first 2 levels, etc.  It defaults to -2 because -1 is the depth value that is parsed (see below)</param>
		/// <param name="storeExcessLevels">Whether to store levels beyond maxDepth in baked JSONObjects</param>
		/// <param name="strict">Whether to be strict in the parsing. For example, non-strict parsing will successfully
		/// parse "a string" into a string-type </param>
		public JSONObject(string jsonString, int maxDepth = -2, bool storeExcessLevels = false, bool strict = false) {
			Parse(jsonString, maxDepth, storeExcessLevels, strict);
		}

		void Parse(string inputString, int maxDepth = -2, bool storeExcessLevels = false, bool strict = false) {
			if (!BeginParse(inputString, strict))
				return;

			var tokenTmp = 1;
			var inputStringLength = inputString.Length;
			var openQuote = false;
			var inProp = false;
			string propName = null;
			var depth = 0;
			var offset = 0;
			while (++offset < inputStringLength) {
				string inner;
				if (!ContinueParse(inputString, maxDepth, storeExcessLevels, ref offset, ref propName, ref openQuote, ref inProp, ref depth, ref tokenTmp, out inner))
					continue;

				list.Add(Create(inner, maxDepth < -1 ? -2 : maxDepth - 1, storeExcessLevels));
			}
		}

		bool BeginParse(string inputString, bool strict) {
			if (string.IsNullOrEmpty(inputString)) {
				type = Type.Null; //If the string is missing, this is a null
				return false;
			}
			
			var whitespaceLength = Whitespace.Length;
			var offset = 0;
			bool isWhitespace;
			char firstCharacter;
			do {
				isWhitespace = false;
				firstCharacter = inputString[offset++];
				for (var i = 0; i < whitespaceLength; i++) {
					if (Whitespace[i] == firstCharacter) {
						isWhitespace = true;
						break;
					}
				}
			} while (isWhitespace);

			var inputStringLength = inputString.Length;
			if (offset > inputStringLength)
				return false;

			if (strict) {
				if (firstCharacter != '[' && firstCharacter != '{') {
					type = Type.Null;
#if USING_UNITY
					Debug.LogWarning
#else
					Debug.WriteLine
#endif
						("Improper (strict) JSON formatting.  First character must be [ or {");
					return false;
				}
			}

#if UNITY_WP8 || UNITY_WSA
			if (inputString == True) {
				type = Type.BOOL;
				b = true;
			} else if (inputString == False) {
				type = Type.BOOL;
				b = false;
			} else if (inputString == Null) {
				type = Type.NULL;
#else

			// Use character comparison instead of string compare as performance optimization
			if (firstCharacter == 't') {
				type = Type.Bool;
				boolValue = true;
			} else if (firstCharacter == 'f') {
				type = Type.Bool;
				boolValue = false;
			} else if (firstCharacter == 'n') {
				type = Type.Null;
#endif
#if JSONOBJECT_USE_FLOAT
			} else if (inputString == Infinity) {
				type = Type.Number;
				floatValue = float.PositiveInfinity;
			} else if (inputString == NegativeInfinity) {
				type = Type.Number;
				floatValue = float.NegativeInfinity;
			} else if (inputString == NaN) {
				type = Type.Number;
				floatValue = float.NaN;
#else
			} else if (inputString == Infinity) {
				type = Type.Number;
				doubleValue = double.PositiveInfinity;
			} else if (inputString == NegativeInfinity) {
				type = Type.Number;
				doubleValue = double.NegativeInfinity;
			} else if (inputString == NaN) {
				type = Type.Number;
				doubleValue = double.NaN;
#endif
			} else if (firstCharacter == '"') {
				type = Type.String;
				offset = inputStringLength - 1;
				do {
					isWhitespace = false;
					var lastCharacter = inputString[offset--];
					for (var i = 0; i < whitespaceLength; i++) {
						if (Whitespace[i] == lastCharacter) {
							isWhitespace = true;
							break;
						}
					}
				} while (isWhitespace);
				stringValue = UnEscapeString(inputString.Substring(1, offset - 2));
			} else {
				/*
				 * Checking for the following formatting (www.json.org)
				 * object - {"field1":value,"field2":value}
				 * array - [value,value,value]
				 * value - string	- "string"
				 *		 - number	- 0.0
				 *		 - bool		- true -or- false
				 *		 - null		- null
				 */
				
				switch (firstCharacter) {
					case '{':
						type = Type.Object;
						keys = new List<string>();
						list = new List<JSONObject>();
						break;
					case '[':
						type = Type.Array;
						list = new List<JSONObject>();
						break;
					default:
						try {
#if JSONOBJECT_USE_FLOAT
							floatValue = Convert.ToSingle(inputString, CultureInfo.InvariantCulture);
#else
							doubleValue = Convert.ToDouble(inputString, CultureInfo.InvariantCulture);
#endif
							if (!inputString.Contains(".")) {
								longValue = Convert.ToInt64(inputString, CultureInfo.InvariantCulture);
								isInteger = true;
							}

							type = Type.Number;
						} catch (FormatException) {
							type = Type.Null;
#if USING_UNITY
							Debug.LogWarning
#else
							Debug.WriteLine
#endif
								(string.Format("Improper JSON formatting:{0}", inputString));
						} catch (OverflowException) {
							type = Type.Number;

#if JSONOBJECT_USE_FLOAT
							floatValue = inputString.StartsWith("-") ? float.NegativeInfinity : float.PositiveInfinity;
#else
							doubleValue = inputString.StartsWith("-") ? double.NegativeInfinity : double.PositiveInfinity;
#endif
						}

						return false;
				}

				return true;
			}

			return false;
		}

		bool ContinueParse(string inputString, int maxDepth, bool storeExcessLevels, ref int offset, ref string propName,
			ref bool openQuote, ref bool inProp, ref int depth, ref int tokenTmp, out string inner) {
			inner = null;

			var nextCharacter = inputString[offset];
			if (Array.IndexOf(Whitespace, nextCharacter) > -1)
				return false;

			if (nextCharacter == '\\') {
				offset += 1;
				return false;
			}

			if (nextCharacter == '"') {
				if (openQuote) {
					if (!inProp && depth == 0 && type == Type.Object)
						propName = inputString.Substring(tokenTmp + 1, offset - tokenTmp - 1);
					openQuote = false;
				} else {
					if (depth == 0 && type == Type.Object)
						tokenTmp = offset;
					openQuote = true;
				}
			}

			if (openQuote)
				return false;

			if (type == Type.Object && depth == 0) {
				if (nextCharacter == ':') {
					tokenTmp = offset + 1;
					inProp = true;
				}
			}

			if (nextCharacter == '[' || nextCharacter == '{')
				depth++;
			else if (nextCharacter == ']' || nextCharacter == '}')
				depth--;

			// If we encounter a ',' at the top level) or a closing ] or }
			if (nextCharacter == ',' && depth == 0 || depth < 0) {
				inProp = false;
				inner = inputString.Substring(tokenTmp, offset - tokenTmp).Trim(Whitespace);
				tokenTmp = offset + 1;
				if (inner.Length > 0) {
					if (type == Type.Object)
						keys.Add(propName);

					//maxDepth of -1 is the end of the line
					if (maxDepth != -1) {
						tokenTmp = offset + 1;
						return true;
					}

					if (storeExcessLevels) {
						list.Add(CreateBakedObject(inner));
					}
				}
			}

			return false;
		}

		IEnumerable<JSONObject> ParseAsync(string inputString, int maxDepth = -2, bool storeExcessLevels = false, bool strict = false) {
			if (!BeginParse(inputString, strict))
				yield break;

			var tokenTmp = 1;
			var inputStringLength = inputString.Length;
			var openQuote = false;
			var inProp = false;
			string propName = null;
			var depth = 0;
			var offset = 0;
			while (++offset < inputStringLength) {
				if (PrintWatch.Elapsed.TotalSeconds > MaxFrameTime) {
					PrintWatch.Reset();
					yield return this;
					PrintWatch.Start();
				}

				string inner;
				if (!ContinueParse(inputString, maxDepth, storeExcessLevels, ref offset, ref propName, ref openQuote, ref inProp, ref depth, ref tokenTmp, out inner))
					continue;

				var jsonObject = Create();
				using (var enumerator = jsonObject.ParseAsync(inner, maxDepth < -1 ? -2 : maxDepth - 1, storeExcessLevels).GetEnumerator()) {
					while (enumerator.MoveNext()) {
						if (!(PrintWatch.Elapsed.TotalSeconds > MaxFrameTime))
							continue;

						PrintWatch.Reset();
						yield return this;
						PrintWatch.Start();
					}

					list.Add(jsonObject);
				}
			}
		}

		public bool isNumber {
			get { return type == Type.Number; }
		}

		public bool isNull {
			get { return type == Type.Null; }
		}

		public bool isString {
			get { return type == Type.String; }
		}

		public bool isBool {
			get { return type == Type.Bool; }
		}

		public bool isArray {
			get { return type == Type.Array; }
		}

		public bool isObject {
			get { return type == Type.Object || type == Type.Baked; }
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
			if (jsonObject == null)
				return; //Don't do anything if the object is null

			if (type != Type.Array) {
				type = Type.Array;
				if (list == null)
					list = new List<JSONObject>();
			}

			list.Add(jsonObject);
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
				if (type != Type.Object) {
					if (keys == null)
						keys = new List<string>();

					if (type == Type.Array) {
						for (var index = 0; index < list.Count; index++)
							keys.Add(index.ToString(CultureInfo.InvariantCulture));
					} else if (list == null)
						list = new List<JSONObject>();

					type = Type.Object; //Congratulations, son, you're an OBJECT now
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
			if (type == Type.Object) {
				var index = keys.IndexOf(name);
				if (index >= 0) {
					field = list[index].boolValue;
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
			if (type == Type.Object) {
				var index = keys.IndexOf(name);
				if (index >= 0) {
#if JSONOBJECT_USE_FLOAT
					field = list[index].floatValue;
#else
					field = list[index].doubleValue;
#endif
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
			if (type == Type.Object) {
				var index = keys.IndexOf(name);
				if (index >= 0) {
#if JSONOBJECT_USE_FLOAT
					field = list[index].floatValue;
#else
					field = (float) list[index].doubleValue;
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
			if (isObject) {
				var index = keys.IndexOf(name);
				if (index >= 0) {
					field = (int) list[index].longValue;
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
			if (isObject) {
				var index = keys.IndexOf(name);
				if (index >= 0) {
					field = list[index].longValue;
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
			if (isObject) {
				var index = keys.IndexOf(name);
				if (index >= 0) {
					field = (uint) list[index].longValue;
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
			if (isObject) {
				var index = keys.IndexOf(name);
				if (index >= 0) {
					field = list[index].stringValue;
					return true;
				}
			}

			if (fail != null) fail.Invoke(name);
			return false;
		}

		public void GetField(string name, GetFieldResponse response, FieldNotFound fail = null) {
			if (response != null && isObject) {
				var index = keys.IndexOf(name);
				if (index >= 0) {
					response.Invoke(list[index]);
					return;
				}
			}

			if (fail != null)
				fail.Invoke(name);
		}

		public JSONObject GetField(string name) {
			if (isObject)
				for (var index = 0; index < keys.Count; index++)
					if (keys[index] == name)
						return list[index];

			return null;
		}

		public bool HasFields(string[] names) {
			if (!isObject)
				return false;

			foreach (var name in names)
				if (!keys.Contains(name))
					return false;

			return true;
		}

		public bool HasField(string name) {
			if (!isObject)
				return false;

			foreach (var fieldName in keys)
				if (fieldName == name)
					return true;

			return false;
		}

		public void Clear() {
			type = Type.Null;
			if (list != null)
				list.Clear();

			if (keys != null)
				keys.Clear();

			stringValue = "";
			longValue = 0;
			boolValue = false;
			isInteger = false;
#if JSONOBJECT_USE_FLOAT
			floatValue = 0;
#else
			doubleValue = 0;
#endif
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
			if (left.type == Type.Null) {
				left.Absorb(right);
			} else if (left.type == Type.Object && right.type == Type.Object) {
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
			} else if (left.type == Type.Array && right.type == Type.Array) {
				if (right.count > left.count) {
#if USING_UNITY
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
			if (type != Type.Baked) {
				stringValue = Print();
				type = Type.Baked;
			}
		}

		public IEnumerable BakeAsync() {
			if (type != Type.Baked) {
				foreach (var s in PrintAsync()) {
					if (s == null)
						yield return null;
					else
						stringValue = s;
				}

				type = Type.Baked;
			}
		}

		public string Print(bool pretty = false) {
			var builder = new StringBuilder();
			Stringify(0, builder, pretty);
			return builder.ToString();
		}

		static string EscapeString(string input) {
			var escaped = input.Replace("\b", "\\b");
			escaped = escaped.Replace("\f", "\\f");
			escaped = escaped.Replace("\n", "\\n");
			escaped = escaped.Replace("\r", "\\r");
			escaped = escaped.Replace("\t", "\\t");
			escaped = escaped.Replace("\"", "\\\"");
			return escaped;
		}

		static string UnEscapeString(string input) {
			var unescaped = input.Replace("\\\"", "\"");
			unescaped = unescaped.Replace("\\b", "\b");
			unescaped = unescaped.Replace("\\f", "\f");
			unescaped = unescaped.Replace("\\n", "\n");
			unescaped = unescaped.Replace("\\r", "\r");
			unescaped = unescaped.Replace("\\t", "\t");
			return unescaped;
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

		/// <summary>
		/// Convert the JSONObject into a string
		/// </summary>
		/// <param name="depth">How many containers deep this run has reached</param>
		/// <param name="builder">The StringBuilder used to build the string</param>
		/// <param name="pretty">Whether this string should be "pretty" and include whitespace for readability</param>
		/// <returns>An enumerator for this function</returns>
		IEnumerable StringifyAsync(int depth, StringBuilder builder, bool pretty = false) {
			if (PrintWatch.Elapsed.TotalSeconds > MaxFrameTime) {
				PrintWatch.Reset();
				yield return null;
				PrintWatch.Start();
			}

			switch (type) {
				case Type.Baked:
					builder.Append(stringValue);
					break;
				case Type.String:
					StringifyString(builder);
					break;
				case Type.Number:
					StringifyNumber(builder);
					break;
				case Type.Object:
					var fieldCount = list.Count;
					if (fieldCount <= 0) {
						StringifyEmptyObject(builder);
						break;
					}

					depth++;

					BeginStringifyObjectContainer(builder, pretty);
					for (var index = 0; index < fieldCount; index++) {
						var jsonObject = list[index];
						if (jsonObject == null)
							continue;

						var key = keys[index];
						BeginStringifyObjectField(builder, pretty, depth, key);
						foreach (IEnumerable e in jsonObject.StringifyAsync(depth, builder, pretty))
							yield return e;

						EndStringifyObjectField(builder, pretty);
					}

					EndStringifyObjectContainer(builder, pretty, depth);
					break;
				case Type.Array:
					var arraySize = list.Count;
					if (arraySize <= 0) {
						StringifyEmptyArray(builder);
						break;
					}

					BeginStringifyArrayContainer(builder, pretty);
					for (var index = 0; index < arraySize; index++) {
						var jsonObject = list[index];
						if (jsonObject == null)
							continue;

						BeginStringifyArrayElement(builder, pretty, depth);
						foreach (IEnumerable e in list[index].StringifyAsync(depth, builder, pretty))
							yield return e;

						EndStringifyArrayElement(builder, pretty);
					}

					EndStringifyArrayContainer(builder, pretty, depth);
					break;
				case Type.Bool:
					StringifyBool(builder);
					break;
				case Type.Null:
					StringifyNull(builder);
					break;
			}
		}

		/// <summary>
		/// Convert the JSONObject into a string
		/// </summary>
		/// <param name="depth">How many containers deep this run has reached</param>
		/// <param name="builder">The StringBuilder used to build the string</param>
		/// <param name="pretty">Whether this string should be "pretty" and include whitespace for readability</param>
		void Stringify(int depth, StringBuilder builder, bool pretty = false) {
			depth++;
			switch (type) {
				case Type.Baked:
					builder.Append(stringValue);
					break;
				case Type.String:
					StringifyString(builder);
					break;
				case Type.Number:
					StringifyNumber(builder);
					break;
				case Type.Object:
					var fieldCount = list.Count;
					if (fieldCount <= 0) {
						StringifyEmptyObject(builder);
						break;
					}

					BeginStringifyObjectContainer(builder, pretty);
					for (var index = 0; index < fieldCount; index++) {
						var jsonObject = list[index];
						if (jsonObject == null)
							continue;

						var key = keys[index];
						BeginStringifyObjectField(builder, pretty, depth, key);
						jsonObject.Stringify(depth, builder, pretty);
						EndStringifyObjectField(builder, pretty);
					}

					EndStringifyObjectContainer(builder, pretty, depth);
					break;
				case Type.Array:
					if (list.Count <= 0) {
						StringifyEmptyArray(builder);
						break;
					}

					BeginStringifyArrayContainer(builder, pretty);
					foreach (var jsonObject in list) {
						if (jsonObject == null)
							continue;

						BeginStringifyArrayElement(builder, pretty, depth);
						jsonObject.Stringify(depth, builder, pretty);
						EndStringifyArrayElement(builder, pretty);
					}

					EndStringifyArrayContainer(builder, pretty, depth);
					break;
				case Type.Bool:
					StringifyBool(builder);
					break;
				case Type.Null:
					StringifyNull(builder);
					break;
			}
		}

		void StringifyString(StringBuilder builder)
		{
			builder.AppendFormat("\"{0}\"", EscapeString(stringValue));
		}

		void BeginStringifyObjectContainer(StringBuilder builder, bool pretty) {
			builder.Append("{");

#if !JSONOBJECT_DISABLE_PRETTY_PRINT
			if (pretty)
				builder.Append(Newline);
#endif
		}

		static void StringifyEmptyObject(StringBuilder builder) {
			builder.Append("{}");
		}

		void BeginStringifyObjectField(StringBuilder builder, bool pretty, int depth, string key) {
#if !JSONOBJECT_DISABLE_PRETTY_PRINT
			if (pretty)
				for (var j = 0; j < depth; j++)
					builder.Append(Tab); //for a bit more readability
#endif

			builder.AppendFormat("\"{0}\":", key);
		}

		void EndStringifyObjectField(StringBuilder builder, bool pretty) {
			builder.Append(",");
#if !JSONOBJECT_DISABLE_PRETTY_PRINT
			if (pretty)
				builder.Append(Newline);
#endif
		}

		void EndStringifyObjectContainer(StringBuilder builder, bool pretty, int depth) {
#if !JSONOBJECT_DISABLE_PRETTY_PRINT
			if (pretty)
				builder.Length -= 3;
			else
#endif
				builder.Length--;

#if !JSONOBJECT_DISABLE_PRETTY_PRINT
			if (pretty && list.Count > 0) {
				builder.Append(Newline);
				for (var j = 0; j < depth - 1; j++)
					builder.Append(Tab);
			}
#endif

			builder.Append("}");
		}

		static void StringifyEmptyArray(StringBuilder builder) {
			builder.Append("[]");
		}

		void BeginStringifyArrayContainer(StringBuilder builder, bool pretty) {
			builder.Append("[");
#if !JSONOBJECT_DISABLE_PRETTY_PRINT
			if (pretty)
				builder.Append(Newline);
#endif

		}

		void BeginStringifyArrayElement(StringBuilder builder, bool pretty, int depth) {
#if !JSONOBJECT_DISABLE_PRETTY_PRINT
			if (pretty)
				for (var j = 0; j < depth; j++)
					builder.Append(Tab); //for a bit more readability
#endif
		}

		void EndStringifyArrayElement(StringBuilder builder, bool pretty) {
			builder.Append(",");
#if !JSONOBJECT_DISABLE_PRETTY_PRINT
			if (pretty)
				builder.Append(Newline);
#endif
		}

		void EndStringifyArrayContainer(StringBuilder builder, bool pretty, int depth) {
#if !JSONOBJECT_DISABLE_PRETTY_PRINT
			if (pretty)
				builder.Length -= 3;
			else
#endif
				builder.Length--;

#if !JSONOBJECT_DISABLE_PRETTY_PRINT
			if (pretty && list.Count > 0) {
				builder.Append(Newline);
				for (var j = 0; j < depth - 1; j++)
					builder.Append(Tab);
			}
#endif

			builder.Append("]");
		}

		void StringifyNumber(StringBuilder builder) {
			if (isInteger) {
				builder.Append(longValue.ToString(CultureInfo.InvariantCulture));
			} else {
#if JSONOBJECT_USE_FLOAT
				if (float.IsNegativeInfinity(floatValue))
					builder.Append(NegativeInfinity);
				else if (float.IsInfinity(floatValue))
					builder.Append(Infinity);
				else if (float.IsNaN(floatValue))
					builder.Append(NaN);
				else
					builder.Append(floatValue.ToString("R", CultureInfo.InvariantCulture));
#else
				if (double.IsNegativeInfinity(doubleValue))
					builder.Append(NegativeInfinity);
				else if (double.IsInfinity(doubleValue))
					builder.Append(Infinity);
				else if (double.IsNaN(doubleValue))
					builder.Append(NaN);
				else
					builder.Append(doubleValue.ToString("R", CultureInfo.InvariantCulture));
#endif
			}
		}

		void StringifyBool(StringBuilder builder) {
			builder.Append(boolValue ? True : False);
		}

		static void StringifyNull(StringBuilder builder) {
			builder.Append(Null);
		}

#if USING_UNITY
		public static implicit operator WWWForm(JSONObject jsonObject) {
			var form = new WWWForm();
			for (var i = 0; i < jsonObject.list.Count; i++) {
				var key = i.ToString(CultureInfo.InvariantCulture);
				if (jsonObject.type == Type.Object)
					key = jsonObject.keys[i];

				var val = jsonObject.list[i].ToString();
				if (jsonObject.list[i].type == Type.String)
					val = val.Replace("\"", "");

				form.AddField(key, val);
			}

			return form;
		}
#endif
		public JSONObject this[int index] {
			get {
				return list.Count > index ? list[index] : null;
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
			if (type != Type.Object) {
#if USING_UNITY
				Debug.Log
#else
				Debug.WriteLine
#endif
					("Tried to turn non-Object JSONObject into a dictionary");

				return null;
			}

			var result = new Dictionary<string, string>();
			for (var index = 0; index < list.Count; index++) {
				var val = list[index];
				switch (val.type) {
					case Type.String:
						result.Add(keys[index], val.stringValue);
						break;
					case Type.Number:
#if JSONOBJECT_USE_FLOAT
						result.Add(keys[index], val.floatValue.ToString(CultureInfo.InvariantCulture));
#else
						result.Add(keys[index], val.doubleValue.ToString(CultureInfo.InvariantCulture));
#endif

						break;
					case Type.Bool:
						result.Add(keys[index], val.boolValue.ToString(CultureInfo.InvariantCulture));
						break;
					default:
#if USING_UNITY
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

		public static implicit operator bool(JSONObject o) {
			return o != null;
		}

#if JSONOBJECT_POOLING
		public static void ClearPool() {
			poolingEnabled = false;
			poolingEnabled = true;
			lock (Pool) {
				Pool.Clear();
			}
		}

		~JSONObject() {
			lock (Pool) {
				if (!poolingEnabled || isPooled || Pool.Count >= MaxPoolSize)
					return;

				Clear();
				isPooled = true;
				Pool.Enqueue(this);
				GC.ReRegisterForFinalize(this);
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

		// Enumerators are positioned before the first element until the first MoveNext() call.
		int position = -1;

		public JSONObjectEnumerator(JSONObject jsonObject) {
			if (!jsonObject.isContainer)
				throw new InvalidOperationException("JSONObject must be an array or object to provide an iterator");

			target = jsonObject;
		}

		public bool MoveNext() {
			position++;
			return position < target.count;
		}

		public void Reset() {
			position = -1;
		}

		object IEnumerator.Current {
			get { return Current; }
		}

		// ReSharper disable once InconsistentNaming
		public JSONObject Current {
			get {
				if (target.isArray)
					return target[position];

				var key = target.keys[position];
				return target[key];
			}
		}
	}
}