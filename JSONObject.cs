//#define READABLE
#define USEFLOAT	//Use floats for numbers instead of doubles

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
 * http://www.opensource.org/licenses/lgpl-2.1.php
 * JSONObject class
 * for use with Unity
 * Copyright Matt Schoen 2010
 */

public class JSONObject {
	const int MAX_DEPTH = 1000;
	const string INFINITY = "\"INFINITY\"";
	const string NEGINFINITY = "\"NEGINFINITY\"";
	public static char[]  WHITESPACE = new char[] { ' ', '\r', '\n', '\t' };
	public enum Type { NULL, STRING, NUMBER, OBJECT, ARRAY, BOOL }
	public bool isContainer { get { return (type == Type.ARRAY || type == Type.OBJECT); } }
	public JSONObject parent;
	public Type type = Type.NULL;
	public int Count { get { return list.Count; } }
	//TODO: Switch to list
	public List<JSONObject> list;
	public List<string> keys;
	public string str;
#if USEFLOAT
	public float n;
#else
	public double n;
#endif
	public bool b;
	public delegate void AddJSONConents(JSONObject self);

	public static JSONObject nullJO { get { return new JSONObject(JSONObject.Type.NULL); } }
	public static JSONObject obj { get { return new JSONObject(JSONObject.Type.OBJECT); } }
	public static JSONObject arr { get { return new JSONObject(JSONObject.Type.ARRAY); } }

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
		Debug.Log(dic);
		foreach(KeyValuePair<string, string> kvp in dic) {
			keys.Add(kvp.Key);
			list.Add(new JSONObject { type = Type.STRING, str = kvp.Value });
		}
	}
	public JSONObject(Dictionary<string, JSONObject> dic) {
		type = Type.OBJECT;
		keys = new List<string>();
		list = new List<JSONObject>();
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
	public void Absorb(JSONObject obj) {
		list.AddRange(obj.list);
		keys.AddRange(obj.keys);
		str = obj.str;
		n = obj.n;
		b = obj.b;
		type = obj.type;
	}
	public JSONObject() { }
	public JSONObject(string str) {	//create a new JSONObject from a string (this will also create any children, and parse the whole string)
		if(str != null) {
			string strtmp = str.Trim(WHITESPACE);
			if(strtmp.Length > 0) {
				if(string.Compare(strtmp, "true", true) == 0) {
					type = Type.BOOL;
					b = true;
				} else if(string.Compare(strtmp, "false", true) == 0) {
					type = Type.BOOL;
					b = false;
				} else if(strtmp == "null") {
					type = Type.NULL;
				} else if(strtmp == INFINITY) {
					type = Type.NUMBER;
					n = Mathf.Infinity;
				} else if(strtmp == NEGINFINITY) {
					type = Type.NUMBER;
					n = Mathf.NegativeInfinity;
				} else if(strtmp[0] == '"') {
					type = Type.STRING;
					this.str = str.Substring(1, str.Length - 2);
				} else {
					try {
#if USEFLOAT
						n = System.Convert.ToSingle(str);
#else
						n = System.Convert.ToDouble(str);
#endif
						type = Type.NUMBER;
					} catch(System.FormatException) {
						int token_tmp = 0;
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
						//TODO: Fix this logic
						do {
							bool stop = true;
							foreach(char c in WHITESPACE)
								if(str[offset] == c)
									stop = false;
							if(stop)
								break;
						} while(offset++ < str.Length);
						if(offset == str.Length) {
							type = Type.NULL;
							Debug.LogWarning("improper JSON formatting:" + str);
							return;
						}
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
								type = Type.NULL;
								Debug.LogWarning("improper JSON formatting:" + str);
								return;
						}
						int depth = 0;
						bool openquote = false;
						bool inProp = false;
						for(int i = offset + 1; i < str.Length; i++) {
							if(str[i] == '"')
								openquote = !openquote;
							else if(str[i] == '[' || str[i] == '{')
								depth++;
							if(depth == 0 && !openquote) {
								if(str[i] == ':' && !inProp) {
									inProp = true;
									try {
										keys.Add(str.Substring(token_tmp + 2, i - token_tmp - 3));
									} catch { Debug.Log(i + " - " + str.Length + " - " + str); }
									token_tmp = i;
								}
								if(str[i] == ',') {
									inProp = false;
									list.Add(new JSONObject(str.Substring(token_tmp + 1, i - token_tmp - 1)));
									token_tmp = i;
								}
								if(str[i] == ']' || str[i] == '}')
									list.Add(new JSONObject(str.Substring(token_tmp + 1, i - token_tmp - 1)));
							}
							if(str[i] == ']' || str[i] == '}')
								depth--;
						}
					}
				}
			} else type = Type.NULL;
		} else type = Type.NULL;	//If the string is missing, this is a null
	}
	public bool IsNumber { get { return type == Type.NUMBER; } }
	public bool IsNull { get { return type == Type.NULL; } }
	public bool IsString { get { return type == Type.STRING; } }
	public bool IsBool { get { return type == Type.BOOL; } }
	public bool IsArray { get { return type == Type.ARRAY; } }
	public bool IsObject { get { return type == Type.OBJECT; } }
	public void Add(bool val) { Add(new JSONObject(val)); }
	public void Add(float val) { Add(new JSONObject(val)); }
	public void Add(int val) { Add(new JSONObject(val)); }
	public void Add(AddJSONConents content) { Add(new JSONObject(content)); }
	public void Add(JSONObject obj) {
		if(obj) {		//Don't do anything if the object is null
			if(type != JSONObject.Type.ARRAY) {
				type = JSONObject.Type.ARRAY;		//Congratulations, son, you're an ARRAY now
				if(list == null)
					list = new List<JSONObject>();
				for(int i = 0; i < list.Count; i++)
					keys.Add(i + "");
			}
			list.Add(obj);
		}
	}
	public void AddField(string name, bool val) { AddField(name, new JSONObject(val)); }
	public void AddField(string name, float val) { AddField(name, new JSONObject(val)); }
	public void AddField(string name, int val) { AddField(name, new JSONObject(val)); }
	public void AddField(string name, AddJSONConents content) { AddField(name, new JSONObject(content)); }
	public void AddField(string name, string val) {
		AddField(name, new JSONObject { type = JSONObject.Type.STRING, str = val });
	}
	public void AddField(string name, JSONObject obj) {
		if(obj) {		//Don't do anything if the object is null
			if(type != JSONObject.Type.OBJECT) {
				type = JSONObject.Type.OBJECT;		//Congratulations, son, you're an OBJECT now
				if(list == null)
					list = new List<JSONObject>();
				for(int i = 0; i < list.Count; i++)
					keys.Add(i + "");
				keys = new List<string>();
			}
			keys.Add(name);
			list.Add(obj);
		}
	}
	public void SetField(string name, bool val) { SetField(name, new JSONObject(val)); }
	public void SetField(string name, float val) { SetField(name, new JSONObject(val)); }
	public void SetField(string name, int val) { SetField(name, new JSONObject(val)); }
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
			if(index >= 0)
				field = list[index].b;
			else if(fail != null) fail.Invoke(name);
		}
	}
	public void GetField(ref float field, string name, FieldNotFound fail = null) {
		if(type == JSONObject.Type.OBJECT) {
			int index = keys.IndexOf(name);
			if(index >= 0)
				field = list[index].n;
			else if(fail != null) fail.Invoke(name);
		}
	}
	public void GetField(ref int field, string name, FieldNotFound fail = null) {
		if(type == JSONObject.Type.OBJECT) {
			int index = keys.IndexOf(name);
			if(index >= 0)
				field = (int)list[index].n;
			else if(fail != null) fail.Invoke(name);
		}
	}
	public void GetField(ref uint field, string name, FieldNotFound fail = null) {
		if(type == JSONObject.Type.OBJECT) {
			int index = keys.IndexOf(name);
			if(index >= 0)
				field = (uint)list[index].n;
			else if(fail != null) fail.Invoke(name);
		}
	}
	public void GetField(ref string field, string name, FieldNotFound fail = null) {
		if(type == JSONObject.Type.OBJECT) {
			int index = keys.IndexOf(name);
			if(index >= 0)
				field = list[index].str;
			else if(fail != null) fail.Invoke(name);
		}
	}
	public void GetField(string name, GetFieldResponse response, FieldNotFound fail = null) {
		if(response != null && type == Type.OBJECT) {
			int index = keys.IndexOf(name);
			if(index >= 0)
				response.Invoke(list[index]);
			else if(fail != null) fail.Invoke(name);
		}
	}
	public JSONObject GetField(string name) {
		if(type == JSONObject.Type.OBJECT)
			for(int i = 0; i < keys.Count; i++)
				if((string)keys[i] == name)
					return (JSONObject)list[i];
		return null;
	}
	public bool HasFields(string[] names) {
		foreach(string name in names)
			if(!keys.Contains(name))
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
		list.Clear();
		keys.Clear();
		str = "";
		n = 0;
		b = false;
	}
	public JSONObject Copy() {
		return new JSONObject(print());
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
	public string print() {
		return print(0);
	}
	public string print(int depth) {	//Convert the JSONObject into a stiring
		if(depth++ > MAX_DEPTH) {
			Debug.Log("reached max depth!");
			return "";
		}
		string str = "";
		switch(type) {
			case Type.STRING:
				str = "\"" + this.str + "\"";
				break;
			case Type.NUMBER:
				if(n == Mathf.Infinity)
					str = INFINITY;
				else if(n == Mathf.NegativeInfinity)
					str = NEGINFINITY;
				else
					str += n;
				break;

			case JSONObject.Type.OBJECT:
				if(list.Count > 0) {
					str = "{";
#if(READABLE)	//for a bit more readability, comment the define above to save space
				str += "\n";
				depth++;
#endif
					for(int i = 0; i < list.Count; i++) {
						string key = (string)keys[i];
						JSONObject obj = (JSONObject)list[i];
						if(obj) {
#if(READABLE)
						for(int j = 0; j < depth; j++)
							str += "\t"; //for a bit more readability
#endif
							str += "\"" + key + "\":";
							str += obj.print(depth) + ",";
#if(READABLE)
						str += "\n";
#endif
						}
					}
#if(READABLE)
				str = str.Substring(0, str.Length - 1);
#endif
					str = str.Substring(0, str.Length - 1);
					str += "}";
				} else str = "null";
				break;
			case JSONObject.Type.ARRAY:
				if(list.Count > 0) {
					str = "[";
#if(READABLE)
				str += "\n"; //for a bit more readability
				depth++;
#endif
					foreach(JSONObject obj in list) {
						if(obj) {
#if(READABLE)
						for(int j = 0; j < depth; j++)
							str += "\t"; //for a bit more readability
#endif
							str += obj.print(depth) + ",";
#if(READABLE)
						str += "\n"; //for a bit more readability
#endif
						}
					}
#if(READABLE)
				str = str.Substring(0, str.Length - 1);
#endif
					str = str.Substring(0, str.Length - 1);
					str += "]";
				} else str = "null";
				break;
			case Type.BOOL:
				if(b)
					str = "true";
				else
					str = "false";
				break;
			case Type.NULL:
				str = "null";
				break;
		}
		return str;
	}
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
}