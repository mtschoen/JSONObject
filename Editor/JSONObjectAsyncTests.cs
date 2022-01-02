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

#if UNITY_5_6_OR_NEWER && JSONOBJECT_TESTS
using NUnit.Framework;
using TestStrings = Defective.JSON.Tests.JSONObjectTestStrings;

namespace Defective.JSON.Tests {
	class JSONObjectAsyncTests {
		static void ValidateJsonString(JSONObject jsonObject, string expected, bool pretty = false) {
			using (var enumerator = jsonObject.PrintAsync(pretty).GetEnumerator()) {
				while (enumerator.MoveNext()) { }
				Assert.That(enumerator.Current, Is.EqualTo(expected));
			}
		}

		[Test]
		public void InputMatchesOutput() {
			ValidateJsonString(new JSONObject(TestStrings.JsonString), TestStrings.JsonString);
		}

		[Test]
		public void PrettyInputMatchesPrettyOutput() {
			ValidateJsonString(new JSONObject(TestStrings.PrettyJsonString), TestStrings.PrettyJsonString, true);
		}

		[TestCase(long.MaxValue)]
		[TestCase(long.MinValue)]
		[TestCase(0)]
		[TestCase(42)]
		public void EncodeLong(long value) {
			var jsonObject = new JSONObject();
			jsonObject.AddField(TestStrings.FieldName, value);
			ValidateJsonString(jsonObject, string.Format(TestStrings.JsonFormat, value));
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
			jsonObject.AddField(TestStrings.FieldName, value);

#if JSONOBJECT_USE_FLOAT
			var expected = value;
#else
			var expected = (double) value;
#endif

			ValidateJsonString(jsonObject, string.Format(TestStrings.JsonFormatFloat, expected));
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
			jsonObject.AddField(TestStrings.FieldName, value);

#if JSONOBJECT_USE_FLOAT
			var expected = (float) value;
#else
			var expected = value;
#endif

			ValidateJsonString(jsonObject, string.Format(TestStrings.JsonFormatFloat, expected));
		}

		[TestCase(long.MaxValue)]
		[TestCase(long.MinValue)]
		[TestCase(0)]
		[TestCase(42)]
		public void EncodeAndParseLong(long value) {
			var jsonText = string.Format(TestStrings.JsonFormat, value);
			ValidateJsonString(new JSONObject(jsonText), jsonText);
		}

		[TestCase(float.NegativeInfinity)]
		[TestCase(float.PositiveInfinity)]
		[TestCase(float.NaN)]
		[TestCase(float.MaxValue)]
		[TestCase(float.MinValue)]
		[TestCase(0)]
		[TestCase(42)]
		public void EncodeAndParseFloat(float value) {
			var jsonText = string.Format(TestStrings.JsonFormatFloat, value);
			ValidateJsonString(new JSONObject(jsonText), jsonText);
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
			var jsonText = string.Format(TestStrings.JsonFormatFloat, value);

#if JSONOBJECT_USE_FLOAT
			var expected = string.Format(TestJsonFormatFloat, (float) value);
#else
			var expected = jsonText;
#endif

			ValidateJsonString(new JSONObject(jsonText), expected);
		}

		[TestCase("Hello World!")]
		[TestCase("")]
		[TestCase("æ")]
		[TestCase("ø")]
		[TestCase("\\u00e6")]
		[TestCase("\\u00f8")]
		public void EncodeAndParseString(string value) {
			var jsonText = string.Format(TestStrings.JsonFormatString, value);
			ValidateJsonString(new JSONObject(jsonText), jsonText);
		}
	}
}
#endif
