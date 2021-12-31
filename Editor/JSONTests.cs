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

#if UNITY_5_6_OR_NEWER && JSONOBJECT_TESTS
using NUnit.Framework;
using UnityEngine;

namespace Defective.JSON.Tests {
	public class JsonObjectMergeTests {
		const string TestJsonString = @"{""TestObject"":{""SomeText"":""Blah"",""SomeObject"":{""SomeNumber"":42,""SomeFloat"":13.37,""SomeBool"":true,""SomeNull"":null},""SomeEmptyObject"":{},""SomeEmptyArray"":[],""EmbeddedObject"":""{\""field\"":\""Value with \\\""escaped quotes\\\""\""}""}}";

		[Test]
		public void InputMatchesOutput() {
			var jsonObject = new JSONObject(TestJsonString);
			Assert.That(jsonObject.ToString(), Is.EqualTo(TestJsonString));

		}
	}
}
#endif
