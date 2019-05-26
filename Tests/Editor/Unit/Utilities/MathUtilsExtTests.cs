using NUnit.Framework;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Tests.Utilities
{
    [TestFixture]
    public class MathUtilsExtTests
    {
        [OneTimeSetUp]
        public void Setup() { }

        [Test]
        public void SmoothDamp_DividesSmoothTimeBy3_Float()
        {
            var velocity = 1f;
            var damped = MathUtilsExt.SmoothDamp(2f, 8f, ref velocity, .3f, 10f, .1f);
            velocity = 1f;
            var dampedExpected = Mathf.SmoothDamp(2f, 8f, ref velocity, .1f, 10f, .1f);

            Assert.AreEqual(dampedExpected, damped);
        }

        [Test]
        public void SmoothDamp_DividesSmoothTimeBy3_Vector3()
        {
            var velocity = new Vector3(2, 1, 0);
            var current = new Vector3(0, 0, 0);
            var target = new Vector3(2, 4, 8);
            var damped = MathUtilsExt.SmoothDamp(current, target, ref velocity, .3f, 10f, .1f);
            velocity = new Vector3(2, 1, 0);
            var dampedExpected = Vector3.SmoothDamp(current, target, ref velocity, .1f, 10f, .1f);

            Assert.AreEqual(dampedExpected, damped);
        }

        [TearDown]
        public void Cleanup() { }
    }
}
