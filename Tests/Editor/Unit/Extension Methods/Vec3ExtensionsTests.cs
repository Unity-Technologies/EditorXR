using NUnit.Framework;
using UnityEngine;
using UnityEditor.Experimental.EditorVR.Extensions;

// MinComponent() has no uses in the project currently, so no test for it
namespace UnityEditor.Experimental.EditorVR.Tests.Extensions
{
    public class Vec3ExtensionsTests
    {
        float delta = 0.00000001f;

        [Test]
        public void MaxComponent_ReturnsMaxAxisValue()
        {
            var maxX = new Vector3(2f, 1f, 0f);
            Assert.AreEqual(maxX.MaxComponent(), maxX.x);
            var maxY = new Vector3(0f, 2f, 1f);
            Assert.AreEqual(maxY.MaxComponent(), maxY.y);
            var maxZ = new Vector3(0f, 1f, 2f);
            Assert.AreEqual(maxZ.MaxComponent(), maxZ.z);
        }

        [Test]
        public void AveragedComponents()
        {
            var vec3 = new Vector3(4f, 2f, 6f);
            Assert.That(vec3.AveragedComponents(), Is.EqualTo(4f).Within(delta));
            vec3 = new Vector3(-4f, 0f, 1f);
            Assert.That(vec3.AveragedComponents(), Is.EqualTo(-1f).Within(delta));
        }

        [Test]
        public void Inverse_PositiveValues()
        {
            var vec3 = new Vector3(2f, 4f, 10f);
            var expected = new Vector3(.5f, .25f, .1f);
            Assert.That(vec3.Inverse(), Is.EqualTo(expected).Within(delta));
        }

        [Test]
        public void Inverse_NegativeValues()
        {
            var vec3 = new Vector3(-10f, -4f, -2f);
            var expected = new Vector3(-.1f, -.25f, -.5f);
            Assert.That(vec3.Inverse(), Is.EqualTo(expected).Within(delta));
        }
    }
}
