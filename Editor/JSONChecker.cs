using UnityEngine;
using UnityEditor;
using System.Collections;

public class JSONChecker : EditorWindow {
	string JSON = @"{
	""TestObject"": {
		""SomeText"": ""Blah"",
		""SomeObject"": {
			""SomeNumber"": 42,
			""SomeBool"": true,
			""SomeNull"": null
		},
		""SomeEmptyObject"": { },
		""SomeEmptyArray"": [ ]
	}
}";
	JSONObject j;
	[MenuItem("Window/JSONChecker")]
	static void Init() {
		GetWindow(typeof(JSONChecker));
	}
	void OnGUI() {
		JSON = EditorGUILayout.TextArea(JSON);
		GUI.enabled = JSON != "";
		if(GUILayout.Button("Check JSON")) {
			j = new JSONObject(JSON);
			Debug.Log(j.ToString(true));
		}
		if(j) {
			if(j.type == JSONObject.Type.NULL)
				GUILayout.Label("JSON fail:\n" + j.ToString(true));
			else
				GUILayout.Label("JSON success:\n" + j.ToString(true));
		}
	}
}
