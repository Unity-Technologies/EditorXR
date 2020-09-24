using NUnit.Framework;
using Unity.EditorXR.Helpers;
using UnityEngine;

namespace Unity.EditorXR.Tests.Utilities
{
    [TestFixture]
    class GradientPairTests
    {
        Color m_ColorA;
        Color m_ColorB;
        Color m_ColorC;
        Color m_ColorD;
        Color m_DefaultColor;

        [OneTimeSetUp]
        public void Setup()
        {
            m_DefaultColor = new Color(0f, 0f, 0f, 0f);
            m_ColorA = new Color(0.2f, 0.4f, 0.8f);
            m_ColorB = new Color(0.4f, 0.8f, 0.2f);
            m_ColorC = new Color(0.5f, 0.6f, 0.7f);
            m_ColorD = new Color(0.1f, 0.2f, 0.3f);
        }

        [Test]
        public void Construct_Parameterless_HasDefaultColors()
        {
            var pair = new GradientPair();
            Assert.AreEqual(m_DefaultColor, pair.a);
            Assert.AreEqual(m_DefaultColor, pair.b);
        }

        [Test]
        public void Construct_FromTwoColors()
        {
            var pair = new GradientPair(m_ColorA, m_ColorB);
            Assert.AreEqual(m_ColorA, pair.a);
            Assert.AreEqual(m_ColorB, pair.b);
        }

        [Test]
        public void Lerp_Interpolates_StartAndEndColors()
        {
            var x = new GradientPair(m_ColorA, m_ColorB);
            var y = new GradientPair(m_ColorC, m_ColorD);
            var lerped = GradientPair.Lerp(x, y, .1f);
            Assert.AreEqual(Color.Lerp(x.a, y.a, .1f), lerped.a);
            Assert.AreEqual(Color.Lerp(x.b, y.b, .1f), lerped.b);
        }

        [TearDown]
        public void Cleanup() { }
    }
}
