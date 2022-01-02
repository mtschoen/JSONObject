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

//#define JSONOBJECT_PERFORMANCE_TEST //For testing performance of parse/stringify.  Turn on editor profiling to see how we're doing

using UnityEngine;
using UnityEditor;
#if UNITY_2017_1_OR_NEWER
using UnityEngine.Networking;
#endif

#if JSONOBJECT_PERFORMANCE_TEST && UNITY_5_6_OR_NEWER
using UnityEngine.Profiling;
#endif

namespace Defective.JSON {
	public class JSONChecker : EditorWindow {
		string testJsonString =  "{\r\n\t\"TestObject\":{\r\n\t\t\"SomeText\":\"Blah\",\r\n\t\t\"SomeObject\":{\r\n\t\t\t\"SomeNumber\":42,\r\n\t\t\t\"SomeFloat\":13.37,\r\n\t\t\t\"SomeBool\":true,\r\n\t\t\t\"SomeNull\":null\r\n\t\t},\r\n\t\t\"SomeEmptyObject\":{},\r\n\t\t\"SomeEmptyArray\":[],\r\n\t\t\"EmbeddedObject\":\"{\\\"field\\\":\\\"Value with \\\\\\\"escaped quotes\\\\\\\"\\\"}\"\r\n\t}\r\n}";
		string url = "";
		JSONObject jsonObject;

		[MenuItem("Window/JSONChecker")]
		static void Init() {
			GetWindow(typeof(JSONChecker)).Show();
		}

		void OnGUI() {
			testJsonString = EditorGUILayout.TextArea(testJsonString);
			GUI.enabled = !string.IsNullOrEmpty(testJsonString);
			if (GUILayout.Button("Check JSON")) {
#if JSONOBJECT_PERFORMANCE_TEST
				Profiler.BeginSample("JSONParse");
				jsonObject = JSONObject.Create(testJsonString);
				Profiler.EndSample();
				Profiler.BeginSample("JSONStringify");
				jsonObject.ToString(true);
				Profiler.EndSample();
#else
				jsonObject = JSONObject.Create(testJsonString);
#endif

				Debug.Log(jsonObject.ToString(true));
			}

			EditorGUILayout.Separator();
			url = EditorGUILayout.TextField("URL", url);
			if (GUILayout.Button("Get JSON")) {
				Debug.Log(url);
#if UNITY_2017_1_OR_NEWER
				var test = new UnityWebRequest(url);

#if UNITY_2017_2_OR_NEWER
				test.SendWebRequest();
#else
				test.Send();
#endif

#if UNITY_2020_1_OR_NEWER
				while (!test.isDone && test.result != UnityWebRequest.Result.ConnectionError) { }
#else
				while (!test.isDone && !test.isNetworkError) { }
#endif

#else
				var test = new WWW(url);
 				while (!test.isDone) { }
#endif

				if (!string.IsNullOrEmpty(test.error)) {
					Debug.Log(test.error);
				} else {
#if UNITY_2017_1_OR_NEWER
					var text = test.downloadHandler.text;
#else
					var text = test.text;
#endif

					Debug.Log(text);
					jsonObject = new JSONObject(text);
					Debug.Log(jsonObject.ToString(true));
				}
			}

			if (jsonObject) {
				GUILayout.Label(jsonObject.type == JSONObject.Type.Null
					? string.Format("JSON fail:\n{0}", jsonObject.ToString(true))
					: string.Format("JSON success:\n{0}", jsonObject.ToString(true)));
			}
		}
	}
}