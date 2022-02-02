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
using System.Text;
using NUnit.Framework;
using TestStrings = Defective.JSON.Tests.JSONObjectTestStrings;

namespace Defective.JSON.Tests {
	class JSONObjectAsyncTests {
		static void CheckJsonLists(JSONObject jsonObject) {
			if (jsonObject.type != JSONObject.Type.Object)
				return;

			var list = jsonObject.list;
			if (list == null)
				return;

			Assert.That(list.Count, Is.EqualTo(jsonObject.keys.Count));
			foreach (var child in jsonObject.list) {
				CheckJsonLists(child);
			}
		}

		static void ValidateJsonObject(JSONObject jsonObject, string expected, bool pretty = false) {
			Assert.IsNotNull(jsonObject);
			CheckJsonLists(jsonObject);
			using (var printer = jsonObject.PrintAsync(pretty).GetEnumerator()) {
				while (printer.MoveNext()) { }
				Assert.That(printer.Current, Is.EqualTo(expected));
			}
		}

		static void ValidateJsonString(string input, string expected, bool pretty = false) {
			using (var parser = JSONObject.CreateAsync(input).GetEnumerator()) {
				var offset = 0;
				var inputLength = input.Length;
				while (parser.MoveNext()) {
					var newOffset = parser.Current.offset;
					Assert.That(newOffset, Is.GreaterThan(offset));
					offset = newOffset;

					// Progress can be tracked using offset / total length
					//Debug.Log(string.Format("Progress: {0:f2}%", (float)newOffset * 100 / inputLength));
				}

				var result = parser.Current;
				Assert.That(result.offset, Is.EqualTo(inputLength));
				ValidateJsonObject(result.result, expected, pretty);
			}
		}

		[TestCase(TestStrings.SomeObject)]
		[TestCase(TestStrings.NestedArray)]
		[TestCase(TestStrings.JsonString)]
		public void InputMatchesOutput(string jsonString) {
			ValidateJsonString(jsonString, jsonString);
		}

		[Test]
		public void InputWithExtraWhitespace() {
			var expected = TestStrings.JsonExtraWhitespace.Replace(" ", string.Empty);
			ValidateJsonString(TestStrings.JsonExtraWhitespace, expected);
		}

		[Test]
		public void SubStringInputMatchesOutput() {
			var start = 14;
			var end = TestStrings.JsonString.Length - 1;
			var substring = TestStrings.JsonString.Substring(start, end - start);
			using (var parser = JSONObject.CreateAsync(TestStrings.JsonString, start, end).GetEnumerator()) {
				var offset = 0;
				while (parser.MoveNext()) {
					var newOffset = parser.Current.offset;
					Assert.That(newOffset, Is.GreaterThan(offset));
					offset = newOffset;
				}

				var result = parser.Current;
				Assert.That(result.offset, Is.EqualTo(end + 1));
				ValidateJsonObject(result.result, substring);
			}
		}

		[TestCase(0, true)]
		[TestCase(1, true)]
		[TestCase(2, true)]
		[TestCase(3, true)]
		[TestCase(4, true)]
		[TestCase(5, true)]
		[TestCase(0, false)]
		[TestCase(1, false)]
		[TestCase(2, false)]
		[TestCase(3, false)]
		[TestCase(4, false)]
		[TestCase(5, false)]
		public void MaxDepthWithExcessLevels(int maxDepth, bool storeExcessLevels) {
			using (var parser = JSONObject.CreateAsync(TestStrings.JsonString, maxDepth: maxDepth, storeExcessLevels: storeExcessLevels).GetEnumerator()) {
				while (parser.MoveNext()) { }

				var jsonObject = parser.Current.result;
				var expectedType = storeExcessLevels ? JSONObject.Type.Baked : JSONObject.Type.Null;
				switch (maxDepth) {
					case 0:
						Assert.That(jsonObject.type, Is.EqualTo(expectedType));
						break;
					case 1:
						var testObject = jsonObject["TestObject"];
						Assert.That(testObject.type, Is.EqualTo(expectedType));
						if (storeExcessLevels)
							Assert.That(testObject.stringValue, Is.EqualTo(TestStrings.JsonString.Substring(14, TestStrings.JsonString.Length - 15)));

						break;
					case 2:
						testObject = jsonObject["TestObject"];
						var someObject = testObject["SomeObject"];
						Assert.That(someObject.type, Is.EqualTo(expectedType));
						if (storeExcessLevels)
							Assert.That(someObject.stringValue, Is.EqualTo(TestStrings.SomeObject));

						var nestedArray = testObject["NestedArray"];
						Assert.That(nestedArray.type, Is.EqualTo(expectedType));
						if (storeExcessLevels)
							Assert.That(nestedArray.stringValue, Is.EqualTo(TestStrings.NestedArray));

						break;

					case 3:
						testObject = jsonObject["TestObject"];
						someObject = testObject["SomeObject"];
						Assert.That(someObject.type, Is.EqualTo(JSONObject.Type.Object));
						nestedArray = testObject["NestedArray"];
						Assert.That(nestedArray.type, Is.EqualTo(JSONObject.Type.Array));
						break;
				}

				if (storeExcessLevels)
					ValidateJsonObject(jsonObject, TestStrings.JsonString);
			}
		}

		[Test]
		public void PrettyInputMatchesPrettyOutput() {
			ValidateJsonString(TestStrings.PrettyJsonString, TestStrings.PrettyJsonString, true);
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
		[TestCase(0.01f)]
		[TestCase(13.37f)]
		[TestCase(42)]
		public void EncodeFloat(float value) {
			var jsonObject = new JSONObject();
			jsonObject.AddField(TestStrings.FieldName, value);

#if JSONOBJECT_USE_FLOAT
			var expected = value;
#else
			var expected = (double) value;
#endif

			ValidateJsonObject(jsonObject, string.Format(TestStrings.JsonFormatFloat, expected.ToString("R")));
		}

		[TestCase(double.NegativeInfinity)]
		[TestCase(double.PositiveInfinity)]
		[TestCase(double.NaN)]
		[TestCase(double.MaxValue)]
		[TestCase(double.MinValue)]
		[TestCase(float.MaxValue)]
		[TestCase(float.MinValue)]
		[TestCase(0)]
		[TestCase(0.01d)]
		[TestCase(13.37d)]
		[TestCase(42)]
		public void EncodeDouble(double value) {
			var jsonObject = new JSONObject();
			jsonObject.AddField(TestStrings.FieldName, value);

#if JSONOBJECT_USE_FLOAT
			var expected = (float) value;
#else
			var expected = value;
#endif

			ValidateJsonObject(jsonObject, string.Format(TestStrings.JsonFormatFloat, expected.ToString("R")));
		}

		[TestCase(long.MaxValue)]
		[TestCase(long.MinValue)]
		[TestCase(0)]
		[TestCase(42)]
		public void EncodeAndParseLong(long value) {
			var jsonText = string.Format(TestStrings.JsonFormat, value);
			ValidateJsonString(jsonText, jsonText);
		}

		[TestCase(float.NegativeInfinity)]
		[TestCase(float.PositiveInfinity)]
		[TestCase(float.NaN)]
		[TestCase(float.MaxValue)]
		[TestCase(float.MinValue)]
		[TestCase(0)]
		[TestCase(0.01f)]
		[TestCase(13.37f)]
		[TestCase(42)]
		public void EncodeAndParseFloat(float value) {
			var jsonText = string.Format(TestStrings.JsonFormatFloat, value.ToString("R"));
			ValidateJsonString(jsonText, jsonText);
		}

		[TestCase(double.NegativeInfinity)]
		[TestCase(double.PositiveInfinity)]
		[TestCase(double.NaN)]
		[TestCase(double.MaxValue)]
		[TestCase(double.MinValue)]
		[TestCase(float.MaxValue)]
		[TestCase(float.MinValue)]
		[TestCase(0)]
		[TestCase(0.01d)]
		[TestCase(13.37d)]
		[TestCase(42)]
		public void EncodeAndParseDouble(double value) {
			var jsonText = string.Format(TestStrings.JsonFormatFloat, value.ToString("R"));

#if JSONOBJECT_USE_FLOAT
			var expected = string.Format(TestStrings.JsonFormatFloat, ((float) value).ToString("R"));
#else
			var expected = jsonText;
#endif

			ValidateJsonString(jsonText, expected);
		}

		[TestCase("Hello World!")]
		[TestCase("")]
		[TestCase("æ")]
		[TestCase("ø")]
		[TestCase("\\u00e6")]
		[TestCase("\\u00f8")]
		public void EncodeAndParseString(string value) {
			var jsonText = string.Format(TestStrings.JsonFormatString, value);
			ValidateJsonString(jsonText, jsonText);
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

			var inputLength = jsonText.Length;
			for (var i = 0; i < 5; i++) {
				using (var parser = JSONObject.CreateAsync(jsonText).GetEnumerator()) {
					var offset = 0;
					while (parser.MoveNext()) {
						var newOffset = parser.Current.offset;
						Assert.That(newOffset, Is.GreaterThan(offset));
						offset = newOffset;
					}

					var result = parser.Current;
					Assert.That(result.offset, Is.EqualTo(inputLength));
					var jsonObject = result.result;
					Assert.IsNotNull(jsonObject);

					stringBuilder.Length = 0;
					using (var printer = jsonObject.PrintAsync(stringBuilder).GetEnumerator()) {
						while (printer.MoveNext()) { }
						Assert.That(stringBuilder.ToString(), Is.EqualTo(jsonText));
					}
				}
			}
		}

		[Test]
		public void Bake() {
			var jsonObject = new JSONObject(TestStrings.JsonString);
			foreach (var unused in jsonObject["TestObject"]["SomeObject"].BakeAsync()) { }
			var testObject = jsonObject["TestObject"];
			var someObject = testObject["SomeObject"];
			Assert.That(someObject.type, Is.EqualTo(JSONObject.Type.Baked));
			Assert.That(someObject.stringValue, Is.EqualTo(TestStrings.SomeObject));

			var nestedArray = testObject["NestedArray"];
			Assert.That(nestedArray.type, Is.EqualTo(JSONObject.Type.Array));
			ValidateJsonObject(jsonObject, TestStrings.JsonString);
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
