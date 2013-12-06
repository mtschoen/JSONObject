#define PRETTY		//Comment out when you no longer need to read JSON to disable pretty print system-wide
#define USEFLOAT	//Use floats for numbers instead of doubles	(enable if you're getting too many significant digits in string output)
//#define POOLING	//Currently using a build setting for this one (also it's experimental)

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;

/*
 * http://www.opensource.org/licenses/lgpl-2.1.php
 * JSONObject class
 * for use with Unity
 * Copyright Matt Schoen 2010 - 2013
 */

public class JSONObject {
#if POOLING
	const int MAX_POOL_SIZE = 10000;
	public static Queue<JSONObject> releaseQueue = new Queue<JSONObject>();
#endif

	const int MAX_DEPTH = 100;
	const string INFINITY = "\"INFINITY\"";
	const string NEGINFINITY = "\"NEGINFINITY\"";
	const string NaN = "\"NaN\"";
	static char[]  WHITESPACE = new char[] { ' ', '\r', '\n', '\t' };
	public enum Type { NULL, STRING, NUMBER, OBJECT, ARRAY, BOOL }
	public bool isContainer { get { return (type == Type.ARRAY || type == Type.OBJECT); } }
	public JSONObject parent;
	public Type type = Type.NULL;
	public int Count { 
		get { 
			if(list == null)
				return -1;
			return list.Count; 
		} 
	}
	public List<JSONObject> list;
	public List<string> keys;
	public string str;
#if USEFLOAT
	public float n;
	public float f {
		get {
			return n;
		}
	}
#else
	public double n;
	public float f {
		get {
			return (float)n;
		}
	}
#endif
	public bool b;
	public delegate void AddJSONConents(JSONObject self);

	public static JSONObject nullJO { get { return Create(JSONObject.Type.NULL); } }	//an empty, null object
	public static JSONObject obj { get { return Create(JSONObject.Type.OBJECT); } }		//an empty object
	public static JSONObject arr { get { return Create(JSONObject.Type.ARRAY); } }		//an empty array

	public JSONObject(JSONObject.Type t) {
		type = t;
		switch(t) {
			case Type.ARRAY:
				list = new List<JSONObject>();
				break;
			case Type.OBJECT:
				list = new List<JSONObject>();
				keys = new List<string>();
				break;
		}
	}
	public JSONObject(bool b) {
		type = Type.BOOL;
		this.b = b;
	}
	public JSONObject(float f) {
		type = Type.NUMBER;
		this.n = f;
	}
	public JSONObject(Dictionary<string, string> dic) {
		type = Type.OBJECT;
		keys = new List<string>();
		list = new List<JSONObject>();
		//Not sure if it's worth removing the foreach here
		foreach(KeyValuePair<string, string> kvp in dic) {
			keys.Add(kvp.Key);
			list.Add(JSONObject.CreateStringObject(str = kvp.Value));
		}
	}
	public JSONObject(Dictionary<string, JSONObject> dic) {
		type = Type.OBJECT;
		keys = new List<string>();
		list = new List<JSONObject>();
		//Not sure if it's worth removing the foreach here
		foreach(KeyValuePair<string, JSONObject> kvp in dic) {
			keys.Add(kvp.Key);
			list.Add(kvp.Value);
		}
	}
	public JSONObject(AddJSONConents content) {
		content.Invoke(this);
	}
	public JSONObject(JSONObject[] objs) {
		type = Type.ARRAY;
		list = new List<JSONObject>(objs);
	}
	//Convenience function for creating a JSONObject containing a string.  This is not part of the constructor so that malformed JSON data doesn't just turn into a string object
	public static JSONObject StringObject(string val) { return JSONObject.CreateStringObject(val); }
	public void Absorb(JSONObject obj) {
		list.AddRange(obj.list);
		keys.AddRange(obj.keys);
		str = obj.str;
		n = obj.n;
		b = obj.b;
		type = obj.type;
	}
	public static JSONObject Create() {
#if POOLING
		JSONObject result = null;
		while(result == null && releaseQueue.Count > 0) {
			result = releaseQueue.Dequeue();
			if(result == null)
				Debug.Log("wtf" + releaseQueue.Count);
			else if(result.list != null)
				Debug.Log("wtf");
		}
		if(result != null)
			return result;
#endif
		return new JSONObject();
	}
	public static JSONObject Create(Type t) {
		JSONObject obj = Create();
		obj.type = t;
		switch(t) {
			case Type.ARRAY:
				obj.list = new List<JSONObject>();
				break;
			case Type.OBJECT:
				obj.list = new List<JSONObject>();
				obj.keys = new List<string>();
				break;
		}
		return obj;
	}
	public static JSONObject Create(bool val) {
		JSONObject obj = Create();
		obj.type = Type.BOOL;
		obj.b = val;
		return obj;
	}
	public static JSONObject Create(float val) {
		JSONObject obj = Create();
		obj.type = Type.NUMBER;
		obj.n = val;
		return obj;
	}
	public static JSONObject Create(int val) {
		JSONObject obj = Create();
		obj.type = Type.NUMBER;
		obj.n = val;
		return obj;
	}
	public static JSONObject CreateStringObject(string val) {
		JSONObject obj = Create();
		obj.type = Type.STRING;
		obj.str = val;
		return obj;
	}
	public static JSONObject Create(string val, bool strict = false) {
		JSONObject obj = Create();
		obj.Parse(val, strict);
		return obj;
	}
	public static JSONObject Create(AddJSONConents content) {
		JSONObject obj = Create();
		content.Invoke(obj);
		return obj;
	}
	public static JSONObject Create(Dictionary<string, string> dic) {
		JSONObject obj = Create();
		obj.type = Type.OBJECT;
		obj.keys = new List<string>();
		obj.list = new List<JSONObject>();
		//Not sure if it's worth removing the foreach here
		foreach(KeyValuePair<string, string> kvp in dic) {
			obj.keys.Add(kvp.Key);
			obj.list.Add(CreateStringObject(kvp.Value));
		}
		return obj;
	}
	public JSONObject() { }
	#region PARSE
	public JSONObject(string str, bool strict = false) {	//create a new JSONObject from a string (this will also create any children, and parse the whole string)
		Parse(str, strict);
	}
	void Parse(string str, bool strict = false){
		if(str != null) {
			str = str.Trim(WHITESPACE);
			if(strict) {
				if(str[0] != '[' && str[0] != '{') {
					type = Type.NULL;
					Debug.LogWarning("Improper (strict) JSON formatting.  First character must be [ or {");
					return;
				}
			}
			if(str.Length > 0) {
				if(string.Compare(str, "true", true) == 0) {
					type = Type.BOOL;
					b = true;
				} else if(string.Compare(str, "false", true) == 0) {
					type = Type.BOOL;
					b = false;
				} else if(string.Compare(str, "null", true) == 0) {
					type = Type.NULL;
#if USEFLOAT
				} else if(str == INFINITY) {
					type = Type.NUMBER;
					n = float.PositiveInfinity;
				} else if(str == NEGINFINITY) {
					type = Type.NUMBER;
					n = float.NegativeInfinity;
				} else if(str == NaN) {
					type = Type.NUMBER;
					n = float.NaN;
#else
				} else if(str == INFINITY) {
					type = Type.NUMBER;
					n = double.PositiveInfinity;
				} else if(str == NEGINFINITY) {
					type = Type.NUMBER;
					n = double.NegativeInfinity;
				} else if(str == NaN) {
					type = Type.NUMBER;
					n = double.NaN;
#endif
				} else if(str[0] == '"') {
					type = Type.STRING;
					this.str = str.Substring(1, str.Length - 2);
				} else {
					int token_tmp = 1;
					/*
					 * Checking for the following formatting (www.json.org)
					 * object - {"field1":value,"field2":value}
					 * array - [value,value,value]
					 * value - string	- "string"
					 *		 - number	- 0.0
					 *		 - bool		- true -or- false
					 *		 - null		- null
					 */
					int offset = 0;
					switch(str[offset]) {
						case '{':
							type = Type.OBJECT;
							keys = new List<string>();
							list = new List<JSONObject>();
							break;
						case '[':
							type = JSONObject.Type.ARRAY;
							list = new List<JSONObject>();
							break;
						default:
							try {
#if USEFLOAT
								n = System.Convert.ToSingle(str);
#else
								n = System.Convert.ToDouble(str);				 
#endif
								type = Type.NUMBER;
							} catch(System.FormatException) {
								type = Type.NULL;
								Debug.LogWarning("improper JSON formatting:" + str);
							}
							return;
					}
					string propName = "";
					bool openQuote = false;
					bool inProp = false;
					int depth = 0;
					while(++offset < str.Length) {
						if(System.Array.IndexOf<char>(WHITESPACE, str[offset]) > -1)
							continue;
						if(str[offset] == '\"') {
							if(openQuote) {
								if(!inProp && depth == 0 && type == Type.OBJECT)
									propName = str.Substring(token_tmp + 1, offset - token_tmp - 1);
								openQuote = false;
							} else {
								if(depth == 0 && type == Type.OBJECT)
									token_tmp = offset;
								openQuote = true;
							}
						}
						if(openQuote)
							continue;
						if(type == Type.OBJECT && depth == 0) {
							if(str[offset] == ':') {
								token_tmp = offset + 1;
								inProp = true;
							}
						}

						if(str[offset] == '[' || str[offset] == '{') {
							depth++;
						} else if(str[offset] == ']' || str[offset] == '}') {
							depth--;
						}
						//if  (encounter a ',' at top level)  || a closing ]/}
						if((str[offset] == ',' && depth == 0) || depth < 0) {
							inProp = false;
							string inner = str.Substring(token_tmp, offset - token_tmp).Trim(WHITESPACE);
							if(inner.Length > 0) {
								if(type == Type.OBJECT)
									keys.Add(propName);
								list.Add(Create(inner, strict));
							}
							token_tmp = offset + 1;
						}
					}

				}
			} else type = Type.NULL;
		} else type = Type.NULL;	//If the string is missing, this is a null
	}
	#endregion
	public bool IsNumber { get { return type == Type.NUMBER; } }
	public bool IsNull { get { return type == Type.NULL; } }
	public bool IsString { get { return type == Type.STRING; } }
	public bool IsBool { get { return type == Type.BOOL; } }
	public bool IsArray { get { return type == Type.ARRAY; } }
	public bool IsObject { get { return type == Type.OBJECT; } }
	public void Add(bool val) {
		Add(Create(val));
	}
	public void Add(float val) {
		Add(Create(val));
	}
	public void Add(int val) {
		Add(Create(val));
	}
	public void Add(string str) {
		Add(CreateStringObject(str));
	}
	public void Add(AddJSONConents content) {
		Add(Create(content));
	}
	public void Add(JSONObject obj) {
		if(obj) {		//Don't do anything if the object is null
			if(type != JSONObject.Type.ARRAY) {
				type = JSONObject.Type.ARRAY;		//Congratulations, son, you're an ARRAY now
				if(list == null)
					list = new List<JSONObject>();
			}
			list.Add(obj);
		}
	}
	public void AddField(string name, bool val) {
		AddField(name, Create(val));
	}
	public void AddField(string name, float val) {
		AddField(name, Create(val));
	}
	public void AddField(string name, int val) {
		AddField(name, Create(val));
	}
	public void AddField(string name, AddJSONConents content) {
		AddField(name, Create(content));
	}
	public void AddField(string name, string val) {
		AddField(name, CreateStringObject(val));
	}
	public void AddField(string name, JSONObject obj) {
		if(obj) {		//Don't do anything if the object is null
			if(type != JSONObject.Type.OBJECT) {
				if(keys == null)
					keys = new List<string>();
				if(type == Type.ARRAY) {
					for(int i = 0; i < list.Count; i++)
						keys.Add(i + "");
				} else
					if(list == null)
						list = new List<JSONObject>();
				type = JSONObject.Type.OBJECT;		//Congratulations, son, you're an OBJECT now
			}
			keys.Add(name);
			list.Add(obj);
		}
	}
	public void SetField(string name, bool val) { SetField(name, JSONObject.Create(val)); }
	public void SetField(string name, float val) { SetField(name, JSONObject.Create(val)); }
	public void SetField(string name, int val) { SetField(name, JSONObject.Create(val)); }
	public void SetField(string name, JSONObject obj) {
		if(HasField(name)) {
			list.Remove(this[name]);
			keys.Remove(name);
		}
		AddField(name, obj);
	}
	public void RemoveField(string name) {
		if(keys.IndexOf(name) > -1) {
			list.RemoveAt(keys.IndexOf(name));
			keys.Remove(name);
		}
	}
	public delegate void FieldNotFound(string name);
	public delegate void GetFieldResponse(JSONObject obj);
	public void GetField(ref bool field, string name, FieldNotFound fail = null) {
		if(type == JSONObject.Type.OBJECT) {
			int index = keys.IndexOf(name);
			if(index >= 0) {
				field = list[index].b;
				return;
			}
		} 
		if(fail != null) fail.Invoke(name);
	}
#if USEFLOAT
	public void GetField(ref float field, string name, FieldNotFound fail = null) {
#else
	public void GetField(ref double field, string name, FieldNotFound fail = null) {
#endif
		if(type == JSONObject.Type.OBJECT) {
			int index = keys.IndexOf(name);
			if(index >= 0){
				field = list[index].n;
				return;
			}
		}
		if(fail != null) fail.Invoke(name);
	}
	public void GetField(ref int field, string name, FieldNotFound fail = null) {
		if(type == JSONObject.Type.OBJECT) {
			int index = keys.IndexOf(name);
			if(index >= 0) {
				field = (int)list[index].n;
				return;
			}
		}
		if(fail != null) fail.Invoke(name);
	}
	public void GetField(ref uint field, string name, FieldNotFound fail = null) {
		if(type == JSONObject.Type.OBJECT) {
			int index = keys.IndexOf(name);
			if(index >= 0) {
				field = (uint)list[index].n;
				return;
			}
		}
		if(fail != null) fail.Invoke(name);
	}
	public void GetField(ref string field, string name, FieldNotFound fail = null) {
		if(type == JSONObject.Type.OBJECT) {
			int index = keys.IndexOf(name);
			if(index >= 0) {
				field = list[index].str;
				return;
			}
		}
		if(fail != null) fail.Invoke(name);
	}
	public void GetField(string name, GetFieldResponse response, FieldNotFound fail = null) {
		if(response != null && type == Type.OBJECT) {
			int index = keys.IndexOf(name);
			if(index >= 0) {
				response.Invoke(list[index]);
				return;
			}
		}
		if(fail != null) fail.Invoke(name);
	}
	public JSONObject GetField(string name) {
		if(type == JSONObject.Type.OBJECT)
			for(int i = 0; i < keys.Count; i++)
				if((string)keys[i] == name)
					return (JSONObject)list[i];
		return null;
	}
	public bool HasFields(string[] names) {
		for(int i = 0; i < names.Length; i++)
			if(!keys.Contains(names[i]))
				return false;
		return true;
	}
	public bool HasField(string name) {
		if(type == JSONObject.Type.OBJECT)
			for(int i = 0; i < keys.Count; i++)
				if((string)keys[i] == name)
					return true;
		return false;
	}
	public void Clear() {
		type = JSONObject.Type.NULL;
		if(list != null)
			list.Clear();
		if (keys != null)
			keys.Clear();
		str = "";
		n = 0;
		b = false;
	}
	/// <summary>
	/// Copy a JSONObject. This could probably work better
	/// </summary>
	/// <returns></returns>
	public JSONObject Copy() {
		return JSONObject.Create(print());
	}
	/*
	 * The Merge function is experimental. Use at your own risk.
	 */
	public void Merge(JSONObject obj) {
		MergeRecur(this, obj);
	}
	/// <summary>
	/// Merge object right into left recursively
	/// </summary>
	/// <param name="left">The left (base) object</param>
	/// <param name="right">The right (new) object</param>
	static void MergeRecur(JSONObject left, JSONObject right) {
		if(left.type == JSONObject.Type.NULL)
			left.Absorb(right);
		else if(left.type == Type.OBJECT && right.type == Type.OBJECT) {
			for(int i = 0; i < right.list.Count; i++) {
				string key = (string)right.keys[i];
				if(right[i].isContainer) {
					if(left.HasField(key))
						MergeRecur(left[key], right[i]);
					else
						left.AddField(key, right[i]);
				} else {
					if(left.HasField(key))
						left.SetField(key, right[i]);
					else
						left.AddField(key, right[i]);
				}
			}
		} else if(left.type == Type.ARRAY && right.type == Type.ARRAY) {
			if(right.Count > left.Count) {
				Debug.LogError("Cannot merge arrays when right object has more elements");
				return;
			}
			for(int i = 0; i < right.list.Count; i++) {
				if(left[i].type == right[i].type) {			//Only overwrite with the same type
					if(left[i].isContainer)
						MergeRecur(left[i], right[i]);
					else {
						left[i] = right[i];
					}
				}
			}
		}
	}
	public string print(bool pretty = false) {
		return print(0, pretty);
	}
	#region STRINGIFY
	public string print(int depth, bool pretty = false) {	//Convert the JSONObject into a string
		if(depth++ > MAX_DEPTH) {
			Debug.Log("reached max depth!");
			return "";
		}
		StringBuilder builder = new StringBuilder();
		switch(type) {
			case Type.STRING:
				return builder.Append('"').Append(str).Append('"').ToString();
			case Type.NUMBER:
#if USEFLOAT
				if(float.IsInfinity(n))
					return INFINITY;
				else if(float.IsNegativeInfinity(n))
					return NEGINFINITY;
				else if(float.IsNaN(n))
					return NaN;
#else
				if(double.IsInfinity(n))
					return INFINITY;
				else if(double.IsNegativeInfinity(n))
					return NEGINFINITY;
				else if(double.IsNaN(n))
					return NaN;
#endif
				else
					return n.ToString();
			case JSONObject.Type.OBJECT:
				builder.Append("{");
				if(list.Count > 0) {
#if(PRETTY)	//for a bit more readability, comment the define above to disable system-wide
					if(pretty)
						builder.Append("\n");
#endif
					for(int i = 0; i < list.Count; i++) {
						string key = (string)keys[i];
						JSONObject obj = (JSONObject)list[i];
						if(obj) {
#if(PRETTY)
							if(pretty)
								for(int j = 0; j < depth; j++)
									builder.Append("\t"); //for a bit more readability
#endif
							builder.Append(string.Format("\"{0}\":{1},", key, obj.print(depth, pretty)));
#if(PRETTY)
							if(pretty)
								builder.Append("\n");
#endif
						}
					}
#if(PRETTY)
					if(pretty)
						builder.Length -= 2;
					else
#endif
					builder.Length--;
				}
#if(PRETTY)
				if(pretty && list.Count > 0) {
					builder.Append("\n");
					for(int j = 0; j < depth - 1; j++)
						builder.Append("\t"); //for a bit more readability
				}
#endif
				builder.Append("}");
				break;
			case JSONObject.Type.ARRAY:
				builder.Append("[");
				if(list.Count > 0) {
#if(PRETTY)
					if(pretty)
						builder.Append("\n"); //for a bit more readability
#endif
					for(int i = 0; i < list.Count; i++){
						if(list[i]){
#if(PRETTY)
							if(pretty)
								for(int j = 0; j < depth; j++)
									builder.Append("\t"); //for a bit more readability
#endif
							builder.Append(list[i].print(depth, pretty)).Append(",");
#if(PRETTY)
							if(pretty)
								builder.Append("\n"); //for a bit more readability
#endif
						}
					}
#if(PRETTY)
					if(pretty)
						builder.Length -= 2;
					else
#endif
						builder.Length--;
				}
#if(PRETTY)
				if(pretty && list.Count > 0) {
					builder.Append("\n");
					for(int j = 0; j < depth - 1; j++)
						builder.Append("\t"); //for a bit more readability
				}
#endif
				builder.Append("]");
				break;
			case Type.BOOL:
				if(b)
					builder.Append("true");
				else
					builder.Append("false");
				break;
			case Type.NULL:
				builder.Append("null");
				break;
		}
		return builder.ToString();
	}
	#endregion
	public static implicit operator WWWForm(JSONObject obj){
		WWWForm form = new WWWForm();
		for(int i = 0; i < obj.list.Count; i++){
			string key = i + "";
			if(obj.type == Type.OBJECT)
				key = obj.keys[i];
			string val = obj.list[i].ToString();
			if(obj.list[i].type == Type.STRING)
				val = val.Replace("\"", "");
			form.AddField(key, val);
		}
		return form;
	}
	public JSONObject this[int index] {
		get {
			if(list.Count > index) return (JSONObject)list[index];
			else return null;
		}
		set {
			if(list.Count > index)
				list[index] = value;
		}
	}
	public JSONObject this[string index] {
		get {
			return GetField(index);
		}
		set {
			SetField(index, value);
		}
	}
	public override string ToString() {
		return print();
	}
	public string ToString(bool pretty) {
		return print(pretty);
	}
	public Dictionary<string, string> ToDictionary() {
		if(type == Type.OBJECT) {
			Dictionary<string, string> result = new Dictionary<string, string>();
			for(int i = 0; i < list.Count; i++) {
				JSONObject val = (JSONObject)list[i];
				switch(val.type) {
					case Type.STRING: result.Add((string)keys[i], val.str); break;
					case Type.NUMBER: result.Add((string)keys[i], val.n + ""); break;
					case Type.BOOL: result.Add((string)keys[i], val.b + ""); break;
					default: Debug.LogWarning("Omitting object: " + (string)keys[i] + " in dictionary conversion"); break;
				}
			}
			return result;
		} else Debug.LogWarning("Tried to turn non-Object JSONObject into a dictionary");
		return null;
	}
	public static implicit operator bool(JSONObject o) {
		return (object)o != null;
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
			parent = null;
			type = Type.NULL;
			list = null;
			keys = null;
			str = "";
			n = 0;
			b = false;
			if(this == null)
				Debug.Log("??");
			releaseQueue.Enqueue(this);
		}
	}
#endif
}