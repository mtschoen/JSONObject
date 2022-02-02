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
// ReSharper disable InlineOutVariableDeclaration
// ReSharper disable UseObjectOrCollectionInitializer

#if UNITY_5_6_OR_NEWER && JSONOBJECT_TESTS
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using TestStrings = Defective.JSON.Tests.JSONObjectTestStrings;

namespace Defective.JSON.Tests {
	class JSONObjectTests {
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

		static void ValidateJsonObject(JSONObject jsonObject, string jsonString, bool pretty = false) {
			CheckJsonLists(jsonObject);
			Assert.That(jsonObject.ToString(pretty), Is.EqualTo(jsonString));
		}

		static void ValidateJsonString(string jsonString, bool pretty = false) {
			var jsonObject = JSONObject.Create(jsonString);
			ValidateJsonObject(jsonObject, jsonString, pretty);
		}

		[TestCase(TestStrings.SomeObject)]
		[TestCase(TestStrings.NestedArray)]
		[TestCase(TestStrings.JsonString)]
		public void InputMatchesOutput(string jsonString) {
			ValidateJsonString(jsonString);
		}

		[Test]
		public void InputWithExtraWhitespace() {
			var expected = TestStrings.JsonExtraWhitespace.Replace(" ", string.Empty);
			ValidateJsonObject(JSONObject.Create(TestStrings.JsonExtraWhitespace), expected);
		}

		[Test]
		public void PrettyInputMatchesPrettyOutput() {
			ValidateJsonString(TestStrings.PrettyJsonString, true);
		}

		[Test]
		public void SubStringInputMatchesOutput() {
			const int start = 14;
			var end = TestStrings.JsonString.Length - 1;
			var substring = TestStrings.JsonString.Substring(start, end - start);
			ValidateJsonObject(JSONObject.Create(TestStrings.JsonString, start, end), substring);
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
			var jsonObject = JSONObject.Create(TestStrings.JsonString, maxDepth: maxDepth, storeExcessLevels: storeExcessLevels);
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

			if (storeExcessLevels || maxDepth > 4)
				ValidateJsonObject(jsonObject, TestStrings.JsonString);
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
		[TestCase(float.Epsilon)]
		[TestCase(-float.Epsilon)]
		[TestCase(0)]
		[TestCase(0.01f)]
		[TestCase(13.37f)]
		[TestCase(42)]
		public void ParseFloat(float value) {
			var jsonObject = new JSONObject(string.Format(TestStrings.JsonFormatFloat, value.ToString("R")));
			Assert.That(jsonObject[TestStrings.FieldName].floatValue, Is.EqualTo(value));
		}

		[TestCase(double.NegativeInfinity)]
		[TestCase(double.PositiveInfinity)]
		[TestCase(double.NaN)]
		[TestCase(double.MaxValue)]
		[TestCase(double.MinValue)]
		[TestCase(double.Epsilon)]
		[TestCase(-double.Epsilon)]
		[TestCase(float.MaxValue)]
		[TestCase(float.MinValue)]
		[TestCase(float.Epsilon)]
		[TestCase(-float.Epsilon)]
		[TestCase(0)]
		[TestCase(0.01d)]
		[TestCase(13.37d)]
		[TestCase(42)]
		public void ParseDouble(double value) {
			var jsonObject = new JSONObject(string.Format(TestStrings.JsonFormatFloat, value.ToString("R")));

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
		[TestCase(float.Epsilon)]
		[TestCase(-float.Epsilon)]
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
		[TestCase(double.Epsilon)]
		[TestCase(-double.Epsilon)]
		[TestCase(float.MaxValue)]
		[TestCase(float.MinValue)]
		[TestCase(float.Epsilon)]
		[TestCase(-float.Epsilon)]
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
			ValidateJsonString(string.Format(TestStrings.JsonFormat, value));
		}

		[TestCase(float.NegativeInfinity)]
		[TestCase(float.PositiveInfinity)]
		[TestCase(float.NaN)]
		[TestCase(float.MaxValue)]
		[TestCase(float.MinValue)]
		[TestCase(float.Epsilon)]
		[TestCase(-float.Epsilon)]
		[TestCase(0)]
		[TestCase(0.01f)]
		[TestCase(13.37f)]
		[TestCase(42)]
		public void EncodeAndParseFloat(float value) {
			ValidateJsonString(string.Format(TestStrings.JsonFormatFloat, value.ToString("R")));
		}

		[TestCase(double.NegativeInfinity)]
		[TestCase(double.PositiveInfinity)]
		[TestCase(double.NaN)]
		[TestCase(double.MaxValue)]
		[TestCase(double.MinValue)]
		[TestCase(double.Epsilon)]
		[TestCase(-double.Epsilon)]
		[TestCase(float.MaxValue)]
		[TestCase(float.MinValue)]
		[TestCase(float.Epsilon)]
		[TestCase(-float.Epsilon)]
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
					stringBuilder.AppendFormat("\"field{0}\":{1},", j, j);
				}

				stringBuilder.Length--;
				stringBuilder.Append("},");
			}

			stringBuilder.Length--;
			stringBuilder.Append("]");
			var jsonText = stringBuilder.ToString();

			// Repeat multiple times to stress test pooling
			for (var i = 0; i < 5; i++) {
				var jsonObject = JSONObject.Create(jsonText);
				stringBuilder.Length = 0;
				jsonObject.Print(stringBuilder);
				Assert.That(stringBuilder.ToString(), Is.EqualTo(jsonText));
			}
		}

		[Test]
		public void Add() {
			var jsonObject = JSONObject.Create();
			jsonObject.Add(true);
			jsonObject.Add(42);
			jsonObject.Add(13.37f);
			jsonObject.Add(42L);
			jsonObject.Add(13.37d);
			jsonObject.Add("test");
			jsonObject.Add(JSONObject.CreateStringObject("test"));
			jsonObject.Add(new JSONObject(42));
			jsonObject.Add(new JSONObject(42L));
			jsonObject.Add(self => {
				self.Add("test");
			});

			Assert.That(jsonObject.type, Is.EqualTo(JSONObject.Type.Array));

			var element = jsonObject[0];
			Assert.That(element.type, Is.EqualTo(JSONObject.Type.Bool));
			Assert.That(element.isBool, Is.True);
			Assert.That(element.boolValue, Is.EqualTo(true));

			element = jsonObject[1];
			Assert.That(element.type, Is.EqualTo(JSONObject.Type.Number));
			Assert.That(element.isNumber, Is.True);
			Assert.That(element.intValue, Is.EqualTo(42));
			Assert.That(element.doubleValue, Is.EqualTo(42));

			element = jsonObject[2];
			Assert.That(element.type, Is.EqualTo(JSONObject.Type.Number));
			Assert.That(element.isNumber, Is.True);
			Assert.That(element.floatValue, Is.EqualTo(13.37f));

			element = jsonObject[3];
			Assert.That(element.type, Is.EqualTo(JSONObject.Type.Number));
			Assert.That(element.isNumber, Is.True);
			Assert.That(element.longValue, Is.EqualTo(42L));
			Assert.That(element.doubleValue, Is.EqualTo(42));

			element = jsonObject[4];
			Assert.That(element.type, Is.EqualTo(JSONObject.Type.Number));
			Assert.That(element.isNumber, Is.True);

#if JSONOBJECT_USE_FLOAT
			Assert.That(element.doubleValue, Is.EqualTo(13.37f));
#else
			Assert.That(element.doubleValue, Is.EqualTo(13.37d));
#endif

			element = jsonObject[5];
			Assert.That(element.type, Is.EqualTo(JSONObject.Type.String));
			Assert.That(element.isString, Is.True);
			Assert.That(element.stringValue, Is.EqualTo("test"));

			element = jsonObject[6];
			Assert.That(element.type, Is.EqualTo(JSONObject.Type.String));
			Assert.That(element.isString, Is.True);
			Assert.That(element.stringValue, Is.EqualTo("test"));

			element = jsonObject[7];
			Assert.That(element.type, Is.EqualTo(JSONObject.Type.Number));
			Assert.That(element.isNumber, Is.True);
			Assert.That(element.intValue, Is.EqualTo(42));

			element = jsonObject[8];
			Assert.That(element.type, Is.EqualTo(JSONObject.Type.Number));
			Assert.That(element.isNumber, Is.True);
			Assert.That(element.longValue, Is.EqualTo(42L));

			element = jsonObject[9];
			Assert.That(element.type, Is.EqualTo(JSONObject.Type.Array));
			Assert.That(element.isArray, Is.True);
			Assert.That(element.count, Is.EqualTo(1));
		}

		[Test]
		public void AddField() {
			var jsonObject = new JSONObject();
			jsonObject.AddField("a", true);
			jsonObject.AddField("b", 42);
			jsonObject.AddField("c", 13.37f);
			jsonObject.AddField("d", 42L);
			jsonObject.AddField("e", 13.37d);
			jsonObject.AddField("f", "test");
			jsonObject.AddField("g", JSONObject.CreateStringObject("test"));
			jsonObject.AddField("h", new JSONObject(13.37f));
			jsonObject.AddField("i", new JSONObject(13.37d));
			jsonObject.AddField("j", self => {
				self.AddField("a", JSONObject.nullObject);
			});

			Assert.That(jsonObject.type, Is.EqualTo(JSONObject.Type.Object));

			var element = jsonObject["a"];
			Assert.That(element.type, Is.EqualTo(JSONObject.Type.Bool));
			Assert.That(element.isBool, Is.True);
			Assert.That(element.boolValue, Is.EqualTo(true));

			element = jsonObject["b"];
			Assert.That(element.type, Is.EqualTo(JSONObject.Type.Number));
			Assert.That(element.isNumber, Is.True);
			Assert.That(element.intValue, Is.EqualTo(42));
			Assert.That(element.doubleValue, Is.EqualTo(42));

			element = jsonObject["c"];
			Assert.That(element.type, Is.EqualTo(JSONObject.Type.Number));
			Assert.That(element.isNumber, Is.True);
			Assert.That(element.floatValue, Is.EqualTo(13.37f));

			element = jsonObject["d"];
			Assert.That(element.type, Is.EqualTo(JSONObject.Type.Number));
			Assert.That(element.isNumber, Is.True);
			Assert.That(element.longValue, Is.EqualTo(42));
			Assert.That(element.doubleValue, Is.EqualTo(42));

			element = jsonObject["e"];
			Assert.That(element.type, Is.EqualTo(JSONObject.Type.Number));
			Assert.That(element.isNumber, Is.True);

#if JSONOBJECT_USE_FLOAT
			Assert.That(element.doubleValue, Is.EqualTo(13.37f));
#else
			Assert.That(element.doubleValue, Is.EqualTo(13.37d));
#endif

			element = jsonObject["f"];
			Assert.That(element.type, Is.EqualTo(JSONObject.Type.String));
			Assert.That(element.isString, Is.True);
			Assert.That(element.stringValue, Is.EqualTo("test"));

			element = jsonObject["g"];
			Assert.That(element.type, Is.EqualTo(JSONObject.Type.String));
			Assert.That(element.isString, Is.True);
			Assert.That(element.stringValue, Is.EqualTo("test"));

			element = jsonObject["h"];
			Assert.That(element.type, Is.EqualTo(JSONObject.Type.Number));
			Assert.That(element.isNumber, Is.True);
			Assert.That(element.floatValue, Is.EqualTo(13.37f));

			element = jsonObject["i"];
			Assert.That(element.type, Is.EqualTo(JSONObject.Type.Number));
			Assert.That(element.isNumber, Is.True);

#if JSONOBJECT_USE_FLOAT
			Assert.That(element.doubleValue, Is.EqualTo(13.37f));
#else
			Assert.That(element.doubleValue, Is.EqualTo(13.37d));
#endif

			element = jsonObject["j"];
			Assert.That(element.type, Is.EqualTo(JSONObject.Type.Object));
			Assert.That(element.isObject, Is.True);
			Assert.That(element.count, Is.EqualTo(1));
		}

		[Test]
		public void SetField() {
			var jsonObject = new JSONObject();
			jsonObject.SetField("a", true);
			jsonObject.SetField("b", 42);
			jsonObject.SetField("c", 13.37f);
			jsonObject.SetField("d", 42L);
			jsonObject.SetField("e", 13.37d);
			jsonObject.SetField("f", "test");
			jsonObject.SetField("g", JSONObject.CreateStringObject("test"));
			jsonObject.SetField("h", new JSONObject(13.37f));
			jsonObject.SetField("i", new JSONObject(13.37d));

			Assert.That(jsonObject.type, Is.EqualTo(JSONObject.Type.Object));

			var element = jsonObject["a"];
			Assert.That(element.type, Is.EqualTo(JSONObject.Type.Bool));
			Assert.That(element.isBool, Is.True);
			Assert.That(element.boolValue, Is.EqualTo(true));

			element = jsonObject["b"];
			Assert.That(element.type, Is.EqualTo(JSONObject.Type.Number));
			Assert.That(element.isNumber, Is.True);
			Assert.That(element.intValue, Is.EqualTo(42));
			Assert.That(element.doubleValue, Is.EqualTo(42));

			element = jsonObject["c"];
			Assert.That(element.type, Is.EqualTo(JSONObject.Type.Number));
			Assert.That(element.isNumber, Is.True);
			Assert.That(element.floatValue, Is.EqualTo(13.37f));

			element = jsonObject["d"];
			Assert.That(element.type, Is.EqualTo(JSONObject.Type.Number));
			Assert.That(element.isNumber, Is.True);
			Assert.That(element.longValue, Is.EqualTo(42));
			Assert.That(element.doubleValue, Is.EqualTo(42));

			element = jsonObject["e"];
			Assert.That(element.type, Is.EqualTo(JSONObject.Type.Number));
			Assert.That(element.isNumber, Is.True);

#if JSONOBJECT_USE_FLOAT
			Assert.That(element.doubleValue, Is.EqualTo(13.37f));
#else
			Assert.That(element.doubleValue, Is.EqualTo(13.37d));
#endif

			element = jsonObject["f"];
			Assert.That(element.type, Is.EqualTo(JSONObject.Type.String));
			Assert.That(element.isString, Is.True);
			Assert.That(element.stringValue, Is.EqualTo("test"));

			element = jsonObject["g"];
			Assert.That(element.type, Is.EqualTo(JSONObject.Type.String));
			Assert.That(element.isString, Is.True);
			Assert.That(element.stringValue, Is.EqualTo("test"));

			element = jsonObject["h"];
			Assert.That(element.type, Is.EqualTo(JSONObject.Type.Number));
			Assert.That(element.isNumber, Is.True);
			Assert.That(element.floatValue, Is.EqualTo(13.37f));

			element = jsonObject["i"];
			Assert.That(element.type, Is.EqualTo(JSONObject.Type.Number));
			Assert.That(element.isNumber, Is.True);

#if JSONOBJECT_USE_FLOAT
			Assert.That(element.doubleValue, Is.EqualTo(13.37f));
#else
			Assert.That(element.doubleValue, Is.EqualTo(13.37d));
#endif

			jsonObject.SetField("a", "newValue");
			element = jsonObject["a"];
			Assert.That(element.type, Is.EqualTo(JSONObject.Type.String));
			Assert.That(element.isString, Is.True);
			Assert.That(element.stringValue, Is.EqualTo("newValue"));
		}

		[Test]
		public void GetField() {
			var jsonObject = new JSONObject();

			/*
			 * Bool
			 */
			bool boolValue;
			var success = jsonObject.GetField(out boolValue, "a", true);
			Assert.That(success, Is.False);
			Assert.That(boolValue, Is.EqualTo(true));

			var notFound = false;
			success = jsonObject.GetField(ref boolValue, "a", name => { notFound = true; });
			Assert.That(success, Is.False);
			Assert.That(boolValue, Is.EqualTo(true));
			Assert.That(notFound, Is.True);

			jsonObject.SetField("a", true);
			success = jsonObject.GetField(out boolValue, "a", true);
			Assert.That(success, Is.True);
			Assert.That(boolValue, Is.EqualTo(true));

			/*
			 * Int
			 */
			int intValue;
			success = jsonObject.GetField(out intValue, "b", 13);
			Assert.That(success, Is.False);
			Assert.That(intValue, Is.EqualTo(13));

			notFound = false;
			success = jsonObject.GetField(ref intValue, "b", name => { notFound = true; });
			Assert.That(success, Is.False);
			Assert.That(intValue, Is.EqualTo(13));
			Assert.That(notFound, Is.True);

			jsonObject.SetField("b", 42);
			success = jsonObject.GetField(out intValue, "b", 13);
			Assert.That(success, Is.True);
			Assert.That(intValue, Is.EqualTo(42));

			/*
			 * UInt
			 */
			uint uIntValue;
			success = jsonObject.GetField(out uIntValue, "c", 13);
			Assert.That(success, Is.False);
			Assert.That(uIntValue, Is.EqualTo(13));

			notFound = false;
			success = jsonObject.GetField(ref uIntValue, "c", name => { notFound = true; });
			Assert.That(success, Is.False);
			Assert.That(uIntValue, Is.EqualTo(13));
			Assert.That(notFound, Is.True);

			jsonObject.SetField("c", 42);
			success = jsonObject.GetField(out uIntValue, "c", 13);
			Assert.That(success, Is.True);
			Assert.That(uIntValue, Is.EqualTo(42));

			/*
			 * Long
			 */
			long longValue;
			success = jsonObject.GetField(out longValue, "d", 13L);
			Assert.That(success, Is.False);
			Assert.That(longValue, Is.EqualTo(13L));

			notFound = false;
			success = jsonObject.GetField(ref longValue, "d", name => {
				notFound = true;
			});
			Assert.That(success, Is.False);
			Assert.That(longValue, Is.EqualTo(13L));
			Assert.That(notFound, Is.True);

			jsonObject.SetField("d", 42L);
			success = jsonObject.GetField(out longValue, "d", 13);
			Assert.That(success, Is.True);
			Assert.That(longValue, Is.EqualTo(42L));


			/*
			 * Float
			 */
			float floatValue;
			success = jsonObject.GetField(out floatValue, "e", 0.01f);
			Assert.That(success, Is.False);
			Assert.That(floatValue, Is.EqualTo(0.01f));

			notFound = false;
			success = jsonObject.GetField(ref floatValue, "e", name => {
				notFound = true;
			});
			Assert.That(success, Is.False);
			Assert.That(floatValue, Is.EqualTo(0.01f));
			Assert.That(notFound, Is.True);

			jsonObject.SetField("e", 13.37f);
			success = jsonObject.GetField(out floatValue, "e", 0.01f);
			Assert.That(success, Is.True);
			Assert.That(floatValue, Is.EqualTo(13.37f));

			/*
			 * Double
			 */
			double doubleValue;
			success = jsonObject.GetField(out doubleValue, "f", 0.01d);
			Assert.That(success, Is.False);
			Assert.That(doubleValue, Is.EqualTo(0.01d));

			notFound = false;
			success = jsonObject.GetField(ref doubleValue, "f", name => {
				notFound = true;
			});
			Assert.That(success, Is.False);
			Assert.That(doubleValue, Is.EqualTo(0.01d));
			Assert.That(notFound, Is.True);

			jsonObject.SetField("f", 13.37d);
			success = jsonObject.GetField(out doubleValue, "f", 0.01d);
			Assert.That(success, Is.True);

#if JSONOBJECT_USE_FLOAT
			Assert.That(doubleValue, Is.EqualTo(13.37f));
#else
			Assert.That(doubleValue, Is.EqualTo(13.37d));
#endif

			/*
			 * String
			 */
			string stringValue;
			success = jsonObject.GetField(out stringValue, "g", "fail");
			Assert.That(success, Is.False);
			Assert.That(stringValue, Is.EqualTo("fail"));

			notFound = false;
			success = jsonObject.GetField(ref stringValue, "g", name => {
				notFound = true;
			});
			Assert.That(success, Is.False);
			Assert.That(stringValue, Is.EqualTo("fail"));
			Assert.That(notFound, Is.True);

			jsonObject.SetField("g", "success");
			success = jsonObject.GetField(out stringValue, "g", "fail");
			Assert.That(success, Is.True);
			Assert.That(stringValue, Is.EqualTo("success"));

			/*
			 * JSONObject
			 */
			var result = jsonObject.GetField("h");
			Assert.That(result, Is.Null);

			notFound = false;
			var found = false;
			jsonObject.GetField("h", value => {
				found = true;
				result = value;
			}, name => {
				notFound = true;
			});

			Assert.That(found, Is.False);
			Assert.That(result, Is.Null);
			Assert.That(notFound, Is.True);

			notFound = false;
			jsonObject.SetField("h", JSONObject.StringObject("test"));
			jsonObject.GetField("h", value => {
				found = true;
				result = value;
			}, name => {
				notFound = true;
			});

			Assert.That(notFound, Is.False);
			Assert.That(found, Is.True);
			Assert.That(result.stringValue, Is.EqualTo("test"));
		}

		[Test]
		public void RemoveField() {
			var jsonObject = new JSONObject();
			jsonObject["test"] = new JSONObject(42);

			Assert.That(jsonObject.type, Is.EqualTo(JSONObject.Type.Object));
			Assert.That(jsonObject.count, Is.EqualTo(1));
			Assert.That(jsonObject.HasField("test"), Is.True);
			Assert.That(jsonObject["test"].intValue, Is.EqualTo(42));

			jsonObject.RemoveField("test");
			Assert.That(jsonObject.type, Is.EqualTo(JSONObject.Type.Object));
			Assert.That(jsonObject.count, Is.EqualTo(0));
			Assert.That(jsonObject.HasField("test"), Is.False);
			Assert.That(jsonObject["test"], Is.Null);
		}

		[Test]
		public void HasField() {
			var jsonObject = new JSONObject();
			jsonObject["test"] = new JSONObject(42);

			Assert.That(jsonObject.type, Is.EqualTo(JSONObject.Type.Object));
			Assert.That(jsonObject.count, Is.EqualTo(1));
			Assert.That(jsonObject.HasField("test"), Is.True);
			Assert.That(jsonObject["test"].intValue, Is.EqualTo(42));
		}

		[Test]
		public void HasFields() {
			var jsonObject = new JSONObject();
			jsonObject["a"] = new JSONObject(42);
			jsonObject["b"] = new JSONObject(13.37f);
			jsonObject["c"] = JSONObject.StringObject("test");

			Assert.That(jsonObject.type, Is.EqualTo(JSONObject.Type.Object));
			Assert.That(jsonObject.count, Is.EqualTo(3));
			Assert.That(jsonObject.HasFields(new[] { "a", "b" }), Is.True);
			Assert.That(jsonObject.HasFields(new [] {"a", "b", "c"}), Is.True);
			Assert.That(jsonObject.HasFields(new[] { "a", "b", "c", "d" }), Is.False);
		}

		[Test]
		public void Copy() {
			var jsonObject = new JSONObject(TestStrings.JsonString);
			var copy = jsonObject.Copy();
			ValidateJsonObject(copy, TestStrings.JsonString);
		}

		[Test]
		public void Merge() {
			var left = new JSONObject();
			left["a"] = new JSONObject(42);
			left["b"] = new JSONObject(13.37f);

			Assert.That(left.type, Is.EqualTo(JSONObject.Type.Object));
			Assert.That(left.count, Is.EqualTo(2));
			Assert.That(left.HasFields(new[] { "a", "b" }), Is.True);
			Assert.That(left.HasFields(new[] { "a", "b", "c" }), Is.False);
			Assert.That(left["a"].intValue, Is.EqualTo(42));
			Assert.That(left["b"].floatValue, Is.EqualTo(13.37f));

			var right = new JSONObject();
			right["b"] = new JSONObject(0.01f);
			right["c"] = JSONObject.StringObject("test");

			Assert.That(right.type, Is.EqualTo(JSONObject.Type.Object));
			Assert.That(right.count, Is.EqualTo(2));
			Assert.That(right.HasFields(new[] { "b", "c" }), Is.True);
			Assert.That(right.HasFields(new[] { "a", "b", "c" }), Is.False);

			left.Merge(right);

			Assert.That(left.type, Is.EqualTo(JSONObject.Type.Object));
			Assert.That(left.count, Is.EqualTo(3));
			Assert.That(left.HasFields(new[] { "a", "b" }), Is.True);
			Assert.That(left.HasFields(new[] { "a", "b", "c" }), Is.True);
			Assert.That(left.HasFields(new[] { "a", "b", "c", "d" }), Is.False);
			Assert.That(left["a"].intValue, Is.EqualTo(42));
			Assert.That(left["b"].floatValue, Is.EqualTo(0.01f));
			Assert.That(left["c"].stringValue, Is.EqualTo("test"));

			Assert.That(right.type, Is.EqualTo(JSONObject.Type.Object));
			Assert.That(right.count, Is.EqualTo(2));
			Assert.That(right.HasFields(new[] { "b", "c" }), Is.True);
			Assert.That(right.HasFields(new[] { "a", "b", "c" }), Is.False);
		}

		[Test]
		public void Absorb() {
			var left = new JSONObject();
			left["a"] = new JSONObject(42);
			left["b"] = new JSONObject(13.37f);

			Assert.That(left.type, Is.EqualTo(JSONObject.Type.Object));
			Assert.That(left.count, Is.EqualTo(2));
			Assert.That(left.HasFields(new[] { "a", "b" }), Is.True);
			Assert.That(left.HasFields(new[] { "a", "b", "c" }), Is.False);

			var right = new JSONObject();
			right["c"] = JSONObject.StringObject("test");

			Assert.That(right.type, Is.EqualTo(JSONObject.Type.Object));
			Assert.That(right.count, Is.EqualTo(1));
			Assert.That(right.HasFields(new[] { "a", "b" }), Is.False);
			Assert.That(right.HasFields(new[] { "c" }), Is.True);

			left.Absorb(right);

			Assert.That(left.type, Is.EqualTo(JSONObject.Type.Object));
			Assert.That(left.count, Is.EqualTo(3));
			Assert.That(left.HasFields(new[] { "a", "b" }), Is.True);
			Assert.That(left.HasFields(new[] { "a", "b", "c" }), Is.True);
			Assert.That(left.HasFields(new[] { "a", "b", "c", "d" }), Is.False);
			Assert.That(left["a"].intValue, Is.EqualTo(42));
			Assert.That(left["b"].floatValue, Is.EqualTo(13.37f));
			Assert.That(left["c"].stringValue, Is.EqualTo("test"));

			Assert.That(right.type, Is.EqualTo(JSONObject.Type.Object));
			Assert.That(right.count, Is.EqualTo(1));
			Assert.That(right.HasFields(new[] { "a", "b" }), Is.False);
			Assert.That(right.HasFields(new[] { "c" }), Is.True);
		}

		[Test]
		public void Clear() {
			var jsonObject = new JSONObject();
			jsonObject["a"] = new JSONObject(42);
			jsonObject["b"] = new JSONObject(13.37f);

			Assert.That(jsonObject.type, Is.EqualTo(JSONObject.Type.Object));
			Assert.That(jsonObject.count, Is.EqualTo(2));
			Assert.That(jsonObject.HasFields(new[] { "a", "b" }), Is.True);
			Assert.That(jsonObject.HasFields(new[] { "a", "b", "c" }), Is.False);

			jsonObject.Clear();
			Assert.That(jsonObject.type, Is.EqualTo(JSONObject.Type.Null));
			Assert.That(jsonObject.count, Is.EqualTo(0));
			Assert.That(jsonObject.HasFields(new[] { "a", "b" }), Is.False);

			jsonObject = JSONObject.CreateStringObject("test");

			Assert.That(jsonObject.type, Is.EqualTo(JSONObject.Type.String));
			Assert.That(jsonObject.stringValue, Is.EqualTo("test"));

			jsonObject.Clear();
			Assert.That(jsonObject.type, Is.EqualTo(JSONObject.Type.Null));
			Assert.That(jsonObject.stringValue, Is.Null);

			jsonObject = new JSONObject(42);

			Assert.That(jsonObject.type, Is.EqualTo(JSONObject.Type.Number));
			Assert.That(jsonObject.isInteger, Is.True);
			Assert.That(jsonObject.intValue, Is.EqualTo(42));

			jsonObject.Clear();
			Assert.That(jsonObject.type, Is.EqualTo(JSONObject.Type.Null));
			Assert.That(jsonObject.isInteger, Is.False);
			Assert.That(jsonObject.intValue, Is.EqualTo(default(int)));

			jsonObject = new JSONObject(13.37f);

			Assert.That(jsonObject.type, Is.EqualTo(JSONObject.Type.Number));
			Assert.That(jsonObject.isInteger, Is.False);
			Assert.That(jsonObject.floatValue, Is.EqualTo(13.37f));

			jsonObject.Clear();
			Assert.That(jsonObject.type, Is.EqualTo(JSONObject.Type.Null));
			Assert.That(jsonObject.floatValue, Is.EqualTo(default(float)));

			jsonObject = new JSONObject(true);

			Assert.That(jsonObject.type, Is.EqualTo(JSONObject.Type.Bool));
			Assert.That(jsonObject.boolValue, Is.EqualTo(true));

			jsonObject.Clear();
			Assert.That(jsonObject.type, Is.EqualTo(JSONObject.Type.Null));
			Assert.That(jsonObject.boolValue, Is.EqualTo(default(bool)));
		}

		[Test]
		public void Bake() {
			var jsonObject = new JSONObject(TestStrings.JsonString);
			jsonObject["TestObject"]["SomeObject"].Bake();
			var testObject = jsonObject["TestObject"];
			var someObject = testObject["SomeObject"];
			Assert.That(someObject.type, Is.EqualTo(JSONObject.Type.Baked));
			Assert.That(someObject.stringValue, Is.EqualTo(TestStrings.SomeObject));

			var nestedArray = testObject["NestedArray"];
			Assert.That(nestedArray.type, Is.EqualTo(JSONObject.Type.Array));
			ValidateJsonObject(jsonObject, TestStrings.JsonString);
		}

		[Test]
		public void Constructors() {
			var jsonObject = new JSONObject(TestStrings.JsonString);
			Assert.That(jsonObject.type, Is.EqualTo(JSONObject.Type.Object));
			Assert.That(jsonObject.isObject, Is.True);
			Assert.That(jsonObject.count, Is.EqualTo(1));

			jsonObject = new JSONObject(true);
			Assert.That(jsonObject.type, Is.EqualTo(JSONObject.Type.Bool));
			Assert.That(jsonObject.isBool, Is.True);
			Assert.That(jsonObject.boolValue, Is.EqualTo(true));

			jsonObject = new JSONObject(42);
			Assert.That(jsonObject.type, Is.EqualTo(JSONObject.Type.Number));
			Assert.That(jsonObject.isNumber, Is.True);
			Assert.That(jsonObject.isInteger, Is.True);
			Assert.That(jsonObject.intValue, Is.EqualTo(42));
			Assert.That(jsonObject.doubleValue, Is.EqualTo(42));

			jsonObject = new JSONObject(13.37f);
			Assert.That(jsonObject.type, Is.EqualTo(JSONObject.Type.Number));
			Assert.That(jsonObject.isNumber, Is.True);
			Assert.That(jsonObject.isInteger, Is.False);
			Assert.That(jsonObject.floatValue, Is.EqualTo(13.37f));

			jsonObject = new JSONObject(42L);
			Assert.That(jsonObject.type, Is.EqualTo(JSONObject.Type.Number));
			Assert.That(jsonObject.isNumber, Is.True);
			Assert.That(jsonObject.isInteger, Is.True);
			Assert.That(jsonObject.longValue, Is.EqualTo(42L));
			Assert.That(jsonObject.doubleValue, Is.EqualTo(42));

			jsonObject = new JSONObject(13.37d);
			Assert.That(jsonObject.type, Is.EqualTo(JSONObject.Type.Number));
			Assert.That(jsonObject.isNumber, Is.True);
			Assert.That(jsonObject.isInteger, Is.False);

#if JSONOBJECT_USE_FLOAT
			Assert.That(jsonObject.doubleValue, Is.EqualTo(13.37f));
#else
			Assert.That(jsonObject.doubleValue, Is.EqualTo(13.37d));
#endif

			jsonObject = new JSONObject(JSONObject.Type.Object);
			Assert.That(jsonObject.type, Is.EqualTo(JSONObject.Type.Object));
			Assert.That(jsonObject.isObject, Is.True);
			Assert.That(jsonObject.count, Is.EqualTo(0));

			jsonObject = new JSONObject(new [] { new JSONObject(42) });
			Assert.That(jsonObject.type, Is.EqualTo(JSONObject.Type.Array));
			Assert.That(jsonObject.isArray, Is.True);
			Assert.That(jsonObject.count, Is.EqualTo(1));
			Assert.That(jsonObject[0].intValue, Is.EqualTo(42));

			jsonObject = new JSONObject(new List<JSONObject> { new JSONObject(42) });
			Assert.That(jsonObject.type, Is.EqualTo(JSONObject.Type.Array));
			Assert.That(jsonObject.isArray, Is.True);
			Assert.That(jsonObject.count, Is.EqualTo(1));
			Assert.That(jsonObject[0].intValue, Is.EqualTo(42));

			jsonObject = new JSONObject(new Dictionary<string, string> { { "field", "value" } });
			Assert.That(jsonObject.type, Is.EqualTo(JSONObject.Type.Object));
			Assert.That(jsonObject.isObject, Is.True);
			Assert.That(jsonObject.count, Is.EqualTo(1));
			Assert.That(jsonObject["field"].stringValue, Is.EqualTo("value"));

			jsonObject = new JSONObject(new Dictionary<string, JSONObject> { { "field", new JSONObject(42) } });
			Assert.That(jsonObject.type, Is.EqualTo(JSONObject.Type.Object));
			Assert.That(jsonObject.isObject, Is.True);
			Assert.That(jsonObject.count, Is.EqualTo(1));
			Assert.That(jsonObject["field"].intValue, Is.EqualTo(42));

			jsonObject = new JSONObject(self => {
				self.Add("test");
			});
			Assert.That(jsonObject.type, Is.EqualTo(JSONObject.Type.Array));
			Assert.That(jsonObject.isArray, Is.True);
			Assert.That(jsonObject.count, Is.EqualTo(1));
		}

		[Test]
		public void CreateMethods() {
			var jsonObject = JSONObject.Create(TestStrings.JsonString);
			Assert.That(jsonObject.type, Is.EqualTo(JSONObject.Type.Object));
			Assert.That(jsonObject.isObject, Is.True);
			Assert.That(jsonObject.count, Is.EqualTo(1));

			jsonObject = JSONObject.Create(true);
			Assert.That(jsonObject.type, Is.EqualTo(JSONObject.Type.Bool));
			Assert.That(jsonObject.isBool, Is.True);
			Assert.That(jsonObject.isInteger, Is.False);
			Assert.That(jsonObject.boolValue, Is.EqualTo(true));

			jsonObject = JSONObject.Create(42);
			Assert.That(jsonObject.type, Is.EqualTo(JSONObject.Type.Number));
			Assert.That(jsonObject.isNumber, Is.True);
			Assert.That(jsonObject.isInteger, Is.True);
			Assert.That(jsonObject.intValue, Is.EqualTo(42));
			Assert.That(jsonObject.doubleValue, Is.EqualTo(42));

			jsonObject = JSONObject.Create(13.37f);
			Assert.That(jsonObject.type, Is.EqualTo(JSONObject.Type.Number));
			Assert.That(jsonObject.isNumber, Is.True);
			Assert.That(jsonObject.isInteger, Is.False);
			Assert.That(jsonObject.floatValue, Is.EqualTo(13.37f));

			jsonObject = JSONObject.Create(42L);
			Assert.That(jsonObject.type, Is.EqualTo(JSONObject.Type.Number));
			Assert.That(jsonObject.isNumber, Is.True);
			Assert.That(jsonObject.isInteger, Is.True);
			Assert.That(jsonObject.longValue, Is.EqualTo(42L));
			Assert.That(jsonObject.doubleValue, Is.EqualTo(42));

			jsonObject = JSONObject.Create(13.37d);
			Assert.That(jsonObject.type, Is.EqualTo(JSONObject.Type.Number));
			Assert.That(jsonObject.isNumber, Is.True);
			Assert.That(jsonObject.isInteger, Is.False);

#if JSONOBJECT_USE_FLOAT
			Assert.That(jsonObject.doubleValue, Is.EqualTo(13.37f));
#else
			Assert.That(jsonObject.doubleValue, Is.EqualTo(13.37d));
#endif

			jsonObject = JSONObject.StringObject("test");
			Assert.That(jsonObject.type, Is.EqualTo(JSONObject.Type.String));
			Assert.That(jsonObject.isString, Is.True);
			Assert.That(jsonObject.stringValue, Is.EqualTo("test"));

			jsonObject = JSONObject.CreateStringObject("test");
			Assert.That(jsonObject.type, Is.EqualTo(JSONObject.Type.String));
			Assert.That(jsonObject.isString, Is.True);
			Assert.That(jsonObject.stringValue, Is.EqualTo("test"));

			jsonObject = JSONObject.CreateBakedObject("test");
			Assert.That(jsonObject.type, Is.EqualTo(JSONObject.Type.Baked));
			Assert.That(jsonObject.isBaked, Is.True);
			Assert.That(jsonObject.stringValue, Is.EqualTo("test"));

			jsonObject = JSONObject.Create(JSONObject.Type.Object);
			Assert.That(jsonObject.type, Is.EqualTo(JSONObject.Type.Object));
			Assert.That(jsonObject.isObject, Is.True);
			Assert.That(jsonObject.count, Is.EqualTo(0));

			jsonObject = JSONObject.Create(new[] { new JSONObject(42) });
			Assert.That(jsonObject.type, Is.EqualTo(JSONObject.Type.Array));
			Assert.That(jsonObject.isArray, Is.True);
			Assert.That(jsonObject.count, Is.EqualTo(1));
			Assert.That(jsonObject[0].intValue, Is.EqualTo(42));

			jsonObject = JSONObject.Create(new List<JSONObject> { new JSONObject(42) });
			Assert.That(jsonObject.type, Is.EqualTo(JSONObject.Type.Array));
			Assert.That(jsonObject.isArray, Is.True);
			Assert.That(jsonObject.count, Is.EqualTo(1));
			Assert.That(jsonObject[0].intValue, Is.EqualTo(42));

			jsonObject = JSONObject.Create(new Dictionary<string, string> { { "field", "value" } });
			Assert.That(jsonObject.type, Is.EqualTo(JSONObject.Type.Object));
			Assert.That(jsonObject.isObject, Is.True);
			Assert.That(jsonObject.count, Is.EqualTo(1));
			Assert.That(jsonObject["field"].stringValue, Is.EqualTo("value"));

			jsonObject = JSONObject.Create(new Dictionary<string, JSONObject> { { "field", new JSONObject(42) } });
			Assert.That(jsonObject.type, Is.EqualTo(JSONObject.Type.Object));
			Assert.That(jsonObject.isObject, Is.True);
			Assert.That(jsonObject.count, Is.EqualTo(1));
			Assert.That(jsonObject["field"].intValue, Is.EqualTo(42));

			jsonObject = JSONObject.Create(self => {
				self.Add("test");
			});
			Assert.That(jsonObject.type, Is.EqualTo(JSONObject.Type.Array));
			Assert.That(jsonObject.isArray, Is.True);
			Assert.That(jsonObject.count, Is.EqualTo(1));
		}

		[Test]
		public void ToDictionary() {
			var sourceDictionary = new Dictionary<string, string> { { "a", "A" }, { "b", "B" }, { "c", "C" } };
			var jsonObject = JSONObject.Create(sourceDictionary);
			var dictionary = jsonObject.ToDictionary();

			Assert.That(dictionary.Count, Is.EqualTo(3));
			Assert.That(dictionary.ContainsKey("a"), Is.True);
			Assert.That(dictionary.ContainsKey("b"), Is.True);
			Assert.That(dictionary.ContainsKey("c"), Is.True);
			Assert.That(dictionary["a"], Is.EqualTo("A"));
			Assert.That(dictionary["b"], Is.EqualTo("B"));
			Assert.That(dictionary["c"], Is.EqualTo("C"));
		}

		[Test]
		public void StaticHelpers() {
			var jsonObject =JSONObject.emptyArray;
			Assert.That(jsonObject.type, Is.EqualTo(JSONObject.Type.Array));
			Assert.That(jsonObject.count, Is.EqualTo(0));
			Assert.That(jsonObject.isArray, Is.True);

			jsonObject = JSONObject.emptyObject;
			Assert.That(jsonObject.type, Is.EqualTo(JSONObject.Type.Object));
			Assert.That(jsonObject.count, Is.EqualTo(0));
			Assert.That(jsonObject.isObject, Is.True);

			jsonObject = JSONObject.nullObject;
			Assert.That(jsonObject.type, Is.EqualTo(JSONObject.Type.Null));
			Assert.That(jsonObject.count, Is.EqualTo(0));
			Assert.That(jsonObject.isNull, Is.True);
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
