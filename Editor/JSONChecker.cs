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
	long totalMem;
	void OnGUI() {
		JSON = EditorGUILayout.TextArea(JSON);
		GUI.enabled = !string.IsNullOrEmpty(JSON);
		if(GUILayout.Button("Check JSON")) {
			j = JSONObject.Create(JSON);
			Debug.Log(j.ToString(true));
		}
		if(j) {
			totalMem = System.GC.GetTotalMemory(false);
			//Debug.Log(System.GC.GetTotalMemory(false) + "");
			if(j.type == JSONObject.Type.NULL)
				GUILayout.Label("JSON fail:\n" + j.ToString(true));
			else
				GUILayout.Label("JSON success:\n" + j.ToString(true));
			Debug.Log((System.GC.GetTotalMemory(false) - totalMem) + "");
		}
	}
}
