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

//#define JSONOBJECT_USE_FLOAT	//Use floats for numbers instead of doubles (enable if you don't need support for doubles and want to cut down on significant digits in output)
// ReSharper disable UseStringInterpolation

#if UNITY_5_6_OR_NEWER && JSONOBJECT_TESTS
using System;
using NUnit.Framework;

namespace Defective.JSON.Tests {
	class JSONObjectEnumeratorTests {
		[Test]
		public void TestListEnumerator() {
			var jsonObject = new JSONObject();
			for (var i = 0; i < 3; i++) {
				jsonObject.Add(42);
			}

			foreach (var element in jsonObject) {
				Assert.That(element.type, Is.EqualTo(JSONObject.Type.Number));
				Assert.That(element.longValue, Is.EqualTo(42));
			}
		}

		[Test]
		public void TestObjectEnumerator() {
			var jsonObject = new JSONObject();
			for (var i = 0; i < 3; i++) {
				jsonObject.AddField(string.Format("Field{0}", i), 42);
			}

			foreach (var element in jsonObject) {
				Assert.That(element.type, Is.EqualTo(JSONObject.Type.Number));
				Assert.That(element.longValue, Is.EqualTo(42));
			}
		}

		[Test]
		public void TestNonContainerEnumeratorThrows() {
			var jsonObject = new JSONObject(42);
			Assert.That(() => EnumerateJsonObject(jsonObject), Throws.TypeOf<InvalidOperationException>());
		}

		static void EnumerateJsonObject(JSONObject jsonObject) {
			foreach (var unused in jsonObject) { }
		}

#if JSONOBJECT_POOLING
		[OneTimeTearDown]
		public void OneTimeTearDown() {
			JSONObject.ClearPool();
		}
#endif
	}
}
#endif
