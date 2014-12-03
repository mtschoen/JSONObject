using System.Security.Policy;
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
	string URL = "";
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
		EditorGUILayout.Separator();
		EditorGUILayout.TextField("URL", URL);
		if (GUILayout.Button("Get JSON")) {
			WWW test = new WWW(URL);
			while (!test.isDone) ;
			if (!string.IsNullOrEmpty(test.error)) {
				Debug.Log(test.error);
			} else {
				j = new JSONObject(test.text);
				Debug.Log(j.ToString(true));
			}
		}
		if(j) {
			if(j.type == JSONObject.Type.NULL)
				GUILayout.Label("JSON fail:\n" + j.ToString(true));
			else
				GUILayout.Label("JSON success:\n" + j.ToString(true));
		}
	}
}
