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

//#define USEFLOAT	//Use floats for numbers instead of doubles (enable if you don't need support for doubles and want to cut down on significant digits in output)

#if UNITY_5_6_OR_NEWER && JSONOBJECT_TESTS
using NUnit.Framework;

namespace Defective.JSON.Tests {
	class JSONObjectTests {
		const string TestJsonString = "{\"TestObject\":{\"SomeText\":\"Blah\",\"SomeObject\":{\"SomeNumber\":42,\"SomeFloat\":13.37,\"SomeBool\":true,\"SomeNull\":null},\"SomeEmptyObject\":{},\"SomeEmptyArray\":[],\"EmbeddedObject\":\"{\\\"field\\\":\\\"Value with \\\\\\\"escaped quotes\\\\\\\"\\\"}\"}}";
		const string TestPrettyJsonString = "{\n\t\"TestObject\":{\n\t\t\"SomeText\":\"Blah\",\n\t\t\"SomeObject\":{\n\t\t\t\"SomeNumber\":42,\n\t\t\t\"SomeFloat\":13.37,\n\t\t\t\"SomeBool\":true,\n\t\t\t\"SomeNull\":null\n\t\t},\n\t\t\"SomeEmptyObject\":{},\n\t\t\"SomeEmptyArray\":[],\n\t\t\"EmbeddedObject\":\"{\\\"field\\\":\\\"Value with \\\\\\\"escaped quotes\\\\\\\"\\\"}\"\n\t}\n}";
		const string TestFieldName = "TestField";
		const string TestJsonFormat = "{{\"" + TestFieldName + "\":{0}}}";
		const string TestJsonFormatFloat = "{{\"" + TestFieldName + "\":{0:R}}}";

		[Test]
		public void InputMatchesOutput() {
			var jsonObject = new JSONObject(TestJsonString);
			Assert.That(jsonObject.ToString(), Is.EqualTo(TestJsonString));
		}

		[Test]
		public void PrettyInputMatchesPrettyOutput() {
			var jsonObject = new JSONObject(TestPrettyJsonString);
			Assert.That(jsonObject.ToString(true), Is.EqualTo(TestPrettyJsonString));
		}

		[TestCase(long.MaxValue)]
		[TestCase(long.MinValue)]
		[TestCase(0)]
		[TestCase(42)]
		public void ParseLong(long value) {
			var jsonObject = new JSONObject(string.Format(TestJsonFormat, value));
			Assert.That(jsonObject[TestFieldName].n, Is.EqualTo(value));
		}

		[TestCase(float.NegativeInfinity)]
		[TestCase(float.PositiveInfinity)]
		[TestCase(float.NaN)]
		[TestCase(float.MaxValue)]
		[TestCase(float.MinValue)]
		[TestCase(0)]
		[TestCase(42)]
		public void ParseFloat(float value) {
			var jsonObject = new JSONObject(string.Format(TestJsonFormatFloat, value));
			Assert.That(jsonObject[TestFieldName].f, Is.EqualTo(value));
		}

		[TestCase(double.NegativeInfinity)]
		[TestCase(double.PositiveInfinity)]
		[TestCase(double.NaN)]
		[TestCase(double.MaxValue)]
		[TestCase(double.MinValue)]
		[TestCase(float.MaxValue)]
		[TestCase(float.MinValue)]
		[TestCase(0)]
		[TestCase(42)]
		public void ParseDouble(double value) {
			var jsonObject = new JSONObject(string.Format(TestJsonFormatFloat, value));

#if USEFLOAT
			var expected = (float) value;
#else
			var expected = value;
#endif

			Assert.That(jsonObject[TestFieldName].n, Is.EqualTo(expected));
		}

		[TestCase(long.MaxValue)]
		[TestCase(long.MinValue)]
		[TestCase(0)]
		[TestCase(42)]
		public void EncodeLong(long value) {
			var jsonObject = new JSONObject();
			jsonObject.AddField(TestFieldName, value);
			Assert.That(jsonObject.ToString(), Is.EqualTo(string.Format(TestJsonFormat, value)));
		}

		[TestCase(float.NegativeInfinity)]
		[TestCase(float.PositiveInfinity)]
		[TestCase(float.NaN)]
		[TestCase(float.MaxValue)]
		[TestCase(float.MinValue)]
		[TestCase(0)]
		[TestCase(42)]
		public void EncodeFloat(float value) {
			var jsonObject = new JSONObject();
			jsonObject.AddField(TestFieldName, value);

#if USEFLOAT
			var expected = value;
#else
			var expected = (double) value;
#endif

			Assert.That(jsonObject.ToString(), Is.EqualTo(string.Format(TestJsonFormatFloat, expected)));
		}

		[TestCase(double.NegativeInfinity)]
		[TestCase(double.PositiveInfinity)]
		[TestCase(double.NaN)]
		[TestCase(double.MaxValue)]
		[TestCase(double.MinValue)]
		[TestCase(float.MaxValue)]
		[TestCase(float.MinValue)]
		[TestCase(0)]
		[TestCase(42)]
		public void EncodeDouble(double value) {
			var jsonObject = new JSONObject();
			jsonObject.AddField(TestFieldName, value);

#if USEFLOAT
			var expected = (float) value;
#else
			var expected = value;
#endif

			Assert.That(jsonObject.ToString(), Is.EqualTo(string.Format(TestJsonFormatFloat, expected)));
		}

		[TestCase(long.MaxValue)]
		[TestCase(long.MinValue)]
		[TestCase(0)]
		[TestCase(42)]
		public void EncodeAndParseLong(long value) {
			var jsonText = string.Format(TestJsonFormat, value);
			var jsonObject = new JSONObject(jsonText);
			Assert.That(jsonObject.ToString(), Is.EqualTo(jsonText));
		}

		[TestCase(float.NegativeInfinity)]
		[TestCase(float.PositiveInfinity)]
		[TestCase(float.NaN)]
		[TestCase(float.MaxValue)]
		[TestCase(float.MinValue)]
		[TestCase(0)]
		[TestCase(42)]
		public void EncodeAndParseFloat(float value) {
			var jsonText = string.Format(TestJsonFormatFloat, value);
			var jsonObject = new JSONObject(jsonText);
			Assert.That(jsonObject.ToString(), Is.EqualTo(jsonText));
		}

		[TestCase(double.NegativeInfinity)]
		[TestCase(double.PositiveInfinity)]
		[TestCase(double.NaN)]
		[TestCase(double.MaxValue)]
		[TestCase(double.MinValue)]
		[TestCase(float.MaxValue)]
		[TestCase(float.MinValue)]
		[TestCase(0)]
		[TestCase(42)]
		public void EncodeAndParseDouble(double value) {
			var jsonText = string.Format(TestJsonFormatFloat, value);
			var jsonObject = new JSONObject(jsonText);

#if USEFLOAT
			var expected = string.Format(TestJsonFormatFloat, (float) value);
#else
			var expected = jsonText;
#endif

			Assert.That(jsonObject.ToString(), Is.EqualTo(expected));
		}
	}
}
#endif
