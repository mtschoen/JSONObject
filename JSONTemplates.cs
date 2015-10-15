using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

/*
Copyright (c) 2015 Matt Schoen

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

public static partial class JSONTemplates {
	static readonly HashSet<object> touched = new HashSet<object>();

	public static JSONObject TOJSON(object obj) {		//For a generic guess
		if(touched.Add(obj)) {
			JSONObject result = JSONObject.obj;
			//Fields
			FieldInfo[] fieldinfo = obj.GetType().GetFields();
			foreach(FieldInfo fi in fieldinfo) {
				JSONObject val = JSONObject.nullJO;
				if(!fi.GetValue(obj).Equals(null)) {
					MethodInfo info = typeof(JSONTemplates).GetMethod("From" + fi.FieldType.Name);
					if(info != null) {
						object[] parms = new object[1];
						parms[0] = fi.GetValue(obj);
						val = (JSONObject)info.Invoke(null, parms);
					} else if(fi.FieldType == typeof(string))
						val = JSONObject.CreateStringObject(fi.GetValue(obj).ToString());
					else
						val = JSONObject.Create(fi.GetValue(obj).ToString());
				}
				if(val) {
					if(val.type != JSONObject.Type.NULL)
						result.AddField(fi.Name, val);
					else Debug.LogWarning("Null for this non-null object, property " + fi.Name + " of class " + obj.GetType().Name + ". Object type is " + fi.FieldType.Name);
				}
			}
			//Properties
			PropertyInfo[] propertyInfo = obj.GetType().GetProperties();
			foreach(PropertyInfo pi in propertyInfo) {
				//This section should mirror part of AssetFactory.AddScripts()
				JSONObject val = JSONObject.nullJO;
				if(!pi.GetValue(obj, null).Equals(null)) {
					MethodInfo info = typeof(JSONTemplates).GetMethod("From" + pi.PropertyType.Name);
					if(info != null) {
						object[] parms = new object[1];
						parms[0] = pi.GetValue(obj, null);
						val = (JSONObject)info.Invoke(null, parms);
					} else if(pi.PropertyType == typeof(string))
						val = JSONObject.CreateStringObject(pi.GetValue(obj, null).ToString());
					else
						val = JSONObject.Create(pi.GetValue(obj, null).ToString());
				}
				if(val) {
					if(val.type != JSONObject.Type.NULL)
						result.AddField(pi.Name, val);
					else Debug.LogWarning("Null for this non-null object, property " + pi.Name + " of class " + obj.GetType().Name + ". Object type is " + pi.PropertyType.Name);
				}
			}
			return result;
		} 
		Debug.LogWarning("trying to save the same data twice");
		return JSONObject.nullJO;
	}
}

/*
 * Some helpful code templates for the JSON class
 * 
 * LOOP THROUGH OBJECT
for(int i = 0; i < obj.Count; i++){
	if(obj.keys[i] != null){
		switch((string)obj.keys[i]){
			case "key1":
				do stuff with (JSONObject)obj.list[i];
				break;
			case "key2":
				do stuff with (JSONObject)obj.list[i];
				break;		
		}
	}
}
 *
 * LOOP THROUGH ARRAY
foreach(JSONObject ob in obj.list)
	do stuff with ob;
 */
