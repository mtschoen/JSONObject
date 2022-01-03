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
using System.Text;
using NUnit.Framework;
using UnityEngine;
using TestStrings = Defective.JSON.Tests.JSONObjectTestStrings;

namespace Defective.JSON.Tests {
	class JSONObjectTests {
		static void ValidateJsonObject(JSONObject jsonObject, string jsonString, bool pretty = false) {
			Assert.That(jsonObject.ToString(pretty), Is.EqualTo(jsonString));
		}

		static void ValidateJsonString(string jsonString, bool pretty = false) {
			ValidateJsonObject(JSONObject.Create(jsonString), jsonString, pretty);
		}

		[Test]
		public void InputMatchesOutput() {
			ValidateJsonString(TestStrings.JsonString);
		}

		[Test]
		public void PrettyInputMatchesPrettyOutput() {
			ValidateJsonString(TestStrings.PrettyJsonString, true);
		}

		[TestCase(long.MaxValue)]
		[TestCase(long.MinValue)]
		[TestCase(0)]
		[TestCase(42)]
		public void ParseLong(long value) {
			var jsonObject = new JSONObject(string.Format(TestStrings.JsonFormat, value));
			Assert.That(jsonObject[TestStrings.FieldName].longValue, Is.EqualTo(value));
		}

		[TestCase(float.NegativeInfinity)]
		[TestCase(float.PositiveInfinity)]
		[TestCase(float.NaN)]
		[TestCase(float.MaxValue)]
		[TestCase(float.MinValue)]
		[TestCase(0)]
		[TestCase(42)]
		public void ParseFloat(float value) {
			var jsonObject = new JSONObject(string.Format(TestStrings.JsonFormatFloat, value));
			Assert.That(jsonObject[TestStrings.FieldName].floatValue, Is.EqualTo(value));
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
			var jsonObject = new JSONObject(string.Format(TestStrings.JsonFormatFloat, value));

#if JSONOBJECT_USE_FLOAT
			Assert.That(jsonObject[TestStrings.FieldName].floatValue, Is.EqualTo((float) value));
#else
			Assert.That(jsonObject[TestStrings.FieldName].doubleValue, Is.EqualTo(value));
#endif
		}

		[TestCase(long.MaxValue)]
		[TestCase(long.MinValue)]
		[TestCase(0)]
		[TestCase(42)]
		public void EncodeLong(long value) {
			var jsonObject = new JSONObject();
			jsonObject.AddField(TestStrings.FieldName, value);
			ValidateJsonObject(jsonObject, string.Format(TestStrings.JsonFormat, value));
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

			ValidateJsonObject(jsonObject, string.Format(TestStrings.JsonFormatFloat, expected));
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

			ValidateJsonObject(jsonObject, string.Format(TestStrings.JsonFormatFloat, expected));
		}

		[TestCase(long.MaxValue)]
		[TestCase(long.MinValue)]
		[TestCase(0)]
		[TestCase(42)]
		public void EncodeAndParseLong(long value) {
			ValidateJsonString(string.Format(TestStrings.JsonFormat, value));
		}

		[TestCase(float.NegativeInfinity)]
		[TestCase(float.PositiveInfinity)]
		[TestCase(float.NaN)]
		[TestCase(float.MaxValue)]
		[TestCase(float.MinValue)]
		[TestCase(0)]
		[TestCase(42)]
		public void EncodeAndParseFloat(float value) {
			ValidateJsonString(string.Format(TestStrings.JsonFormatFloat, value));
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
			var expected = string.Format(TestStrings.JsonFormatFloat, (float) value);
#else
			var expected = jsonText;
#endif

			ValidateJsonObject(new JSONObject(jsonText), expected);
		}

		[TestCase("Hello World!")]
		[TestCase("")]
		[TestCase("æ")]
		[TestCase("ø")]
		[TestCase("\\u00e6")]
		[TestCase("\\u00f8")]
		public void EncodeAndParseString(string value) {
			ValidateJsonString(string.Format(TestStrings.JsonFormatString, value));
		}

		[Test]
		public void EncodeAndParseLargeObjects() {
			var stringBuilder = new StringBuilder();
			stringBuilder.Append('[');
			for (var i = 0; i < 500; i++) {
				stringBuilder.Append('{');
				for (var j = 0; j < 100; j++) {
					stringBuilder.Append(string.Format("\"field{0}\":{1},", j, j));
				}

				stringBuilder.Length--;
				stringBuilder.Append("},");
			}

			stringBuilder.Length--;
			stringBuilder.Append("]");
			var jsonText = stringBuilder.ToString();

			// Repeat multiple times to stress test pooling
			for (var i = 0; i < 5; i++) {
				ValidateJsonString(jsonText);
			}
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
