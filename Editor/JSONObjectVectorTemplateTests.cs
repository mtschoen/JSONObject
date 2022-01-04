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
using UnityEngine;

namespace Defective.JSON.Tests {
	class JSONObjectVectorTemplateTests {
		[TestCase(0, 0)]
		[TestCase(1, 1)]
		[TestCase(-1, -1)]
		[TestCase(float.MinValue, float.MaxValue)]
		[TestCase(float.Epsilon, float.NaN)]
		public void Vector2Template(float x, float y) {
			var value = new Vector2(x, y);
			var jsonObject = value.ToJson();
			var result = jsonObject.ToVector2();
			Assert.That(result.x, Is.EqualTo(x));
			Assert.That(result.y, Is.EqualTo(y));
		}

		[TestCase(0, 0)]
		[TestCase(1, 1)]
		[TestCase(-1, -1)]
		[TestCase(float.MinValue, float.MaxValue)]
		[TestCase(float.Epsilon, float.NaN)]
		public void Vector3Template(float x, float y) {
			var value = new Vector3(x, y);
			var jsonObject = value.ToJson();
			var result = jsonObject.ToVector3();
			Assert.That(result.x, Is.EqualTo(x));
			Assert.That(result.y, Is.EqualTo(y));
		}

		[TestCase(0, 0)]
		[TestCase(1, 1)]
		[TestCase(-1, -1)]
		[TestCase(float.MinValue, float.MaxValue)]
		[TestCase(float.Epsilon, float.NaN)]
		public void Vector4Template(float x, float y) {
			var value = new Vector4(x, y);
			var jsonObject = value.ToJson();
			var result = jsonObject.ToVector4();
			Assert.That(result.x, Is.EqualTo(x));
			Assert.That(result.y, Is.EqualTo(y));
		}

		[TestCase(0, 0, 0, 0)]
		[TestCase(1, 1, 1, 1)]
		[TestCase(-1, -1, -1, -1)]
		[TestCase(float.MinValue, float.MaxValue, float.MinValue, float.MaxValue)]
		[TestCase(float.Epsilon, float.NaN, float.Epsilon, float.NaN)]
		// ReSharper disable once InconsistentNaming
		public void Matrix4x4Template(float fov, float aspect, float zNear, float zFar) {
			var value = Matrix4x4.Perspective(fov, aspect, zNear, zFar);
			var jsonObject = value.ToJson();
			var result = jsonObject.ToMatrix4x4();
			for (var i = 0; i < 16; i++) {
				Assert.That(result[i], Is.EqualTo(value[i]));
			}
		}

		[TestCase(0, 0, 0, 0)]
		[TestCase(90, 1, 0, 0)]
		[TestCase(-90, -1, -1, -1)]
		[TestCase(float.MinValue, float.MaxValue, float.MinValue, float.MaxValue)]
		[TestCase(float.Epsilon, float.NaN, float.Epsilon, float.NaN)]
		public void QuaternionTemplate(float angle, float x, float y, float z) {
			var value = Quaternion.AngleAxis(angle, new Vector3(x, y, z));
			var jsonObject = value.ToJson();
			Assert.That(jsonObject.ToQuaternion(), Is.EqualTo(value));
		}

		[TestCase(0, 0, 0, 0)]
		[TestCase(1, 1, 1, 1)]
		[TestCase(-1, -1, -1, -1)]
		[TestCase(float.MinValue, float.MaxValue, float.MinValue, float.MaxValue)]
		[TestCase(float.Epsilon, float.NaN, float.Epsilon, float.NaN)]
		public void ColorTemplate(float r, float g, float b, float a) {
			var value = new Color(r, g, b, a);
			var jsonObject = value.ToJson();
			Assert.That(jsonObject.ToColor(), Is.EqualTo(value));
		}

		[TestCase(0)]
		[TestCase(1)]
		[TestCase(-1)]
		[TestCase(int.MinValue)]
		[TestCase(int.MaxValue)]
		public void LayerMaskTemplate(int mask) {
			var value = (LayerMask) mask;
			var jsonObject = value.ToJson();
			Assert.That(jsonObject.ToLayerMask(), Is.EqualTo(value));
		}

		[TestCase(0, 0, 0, 0)]
		[TestCase(1, 1, 1, 1)]
		[TestCase(-1, -1, -1, -1)]
		[TestCase(float.MinValue, float.MaxValue, float.MinValue, float.MaxValue)]
		[TestCase(float.Epsilon, float.NaN, float.Epsilon, float.NaN)]
		public void RectTemplate(float x, float y, float width, float height) {
			var value = new Rect(x, y, width, height);
			var jsonObject = value.ToJson();
			Assert.That(jsonObject.ToRect(), Is.EqualTo(value));
		}

		[TestCase(0, 0, 0, 0)]
		[TestCase(1, 1, 1, 1)]
		[TestCase(-1, -1, -1, -1)]
		[TestCase(int.MinValue, int.MaxValue, int.MinValue, int.MaxValue)]
		public void RectOffsetTemplate(int left, int right, int top, int bottom) {
			var value = new RectOffset(left, right, top , bottom);
			var jsonObject = value.ToJson();

			// RectOffset is a class, so Is.EqualTo will be false even if the values are the same
			var result = jsonObject.ToRectOffset();
			Assert.That(result.left, Is.EqualTo(value.left));
			Assert.That(result.right, Is.EqualTo(value.right));
			Assert.That(result.top, Is.EqualTo(value.top));
			Assert.That(result.bottom, Is.EqualTo(value.bottom));
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
