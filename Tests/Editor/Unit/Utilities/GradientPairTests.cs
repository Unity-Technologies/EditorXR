using UnityEngine;
using NUnit.Framework;
using UnityEditor.Experimental.EditorVR.Helpers;

namespace UnityEditor.Experimental.EditorVR.Tests.Utilities
{
    [TestFixture]
    public class GradientPairTests
    {
        Color colorA, colorB, colorC, colorD, defaultColor;

        [OneTimeSetUp]
        public void Setup()
        {
            defaultColor = new Color(0f, 0f, 0f, 0f);
            colorA = new Color(0.2f, 0.4f, 0.8f);
            colorB = new Color(0.4f, 0.8f, 0.2f);
            colorC = new Color(0.5f, 0.6f, 0.7f);
            colorD = new Color(0.1f, 0.2f, 0.3f);
        }

        [Test]
        public void Construct_Parameterless_HasDefaultColors()
        {
            var pair = new GradientPair();
            Assert.AreEqual(defaultColor, pair.a);
            Assert.AreEqual(defaultColor, pair.b);
        }

        [Test]
        public void Construct_FromTwoColors()
        {
            var pair = new GradientPair(colorA, colorB);
            Assert.AreEqual(colorA, pair.a);
            Assert.AreEqual(colorB, pair.b);
        }

        [Test]
        public void Lerp_Interpolates_StartAndEndColors()
        {
            var x = new GradientPair(colorA, colorB);
            var y = new GradientPair(colorC, colorD);
            var lerped = GradientPair.Lerp(x, y, .1f);
            Assert.AreEqual(Color.Lerp(x.a, y.a, .1f),  lerped.a);
            Assert.AreEqual(Color.Lerp(x.b, y.b, .1f),  lerped.b);
        }

        [TearDown]
        public void Cleanup() { }
    }
}

