#if UNITY_5_6_OR_NEWER
using UnityEngine;
using UnityEngine.UI;
using NUnit.Framework;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEditor.Experimental.EditorVR.Tests.TestHelpers;

namespace UnityEditor.Experimental.EditorVR.Tests.Utilities
{
    public class MaterialUtilsTests
    {
        GameObject m_GO;
        Renderer m_Renderer;
        Graphic m_Graphic;
        Material m_Clone;

        [OneTimeSetUp]
        public void Setup()
        {
            m_GO = new GameObject("renderer object");
            var shader = Shader.Find("Standard");

            m_Renderer = m_GO.AddComponent<MeshRenderer>();
            m_Renderer.sharedMaterial = new Material(shader);
            m_Graphic = m_GO.AddComponent<TestImage>();
            m_Graphic.material = m_Renderer.sharedMaterial;
        }

        [Test]
        public void GetMaterialClone_ClonesRendererSharedMaterial()
        {
            m_Clone = MaterialUtils.GetMaterialClone(m_Renderer);
            Assert.AreEqual(m_Renderer.sharedMaterial, m_Clone);
            ObjectUtils.Destroy(m_Clone);
        }

        [Test]
        public void GetMaterialClone_ClonesGraphicMaterial()
        {
            m_Clone = MaterialUtils.GetMaterialClone(m_Graphic);
            Assert.AreEqual(m_Graphic.material, m_Clone);
            ObjectUtils.Destroy(m_Clone);
        }

        // normally you can directly assert equality on Colors, but 
        // creating them based on the float coming from this results in mismatches due to rounding
        static void AssertColorsEqual(Color expected, Color actual)
        {
            const float tolerance = 0.334f;
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(tolerance));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(tolerance));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(tolerance));
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(tolerance));
        }

        [TestCase("#000000", 0f, 0f, 0f, 1f)] // rgb: 0, 0, 0
        [TestCase("#002244", 0f, 0.133f, 0.267f, 1f)] // rgb: 136, 221, 102
        [TestCase("#4488BBBB", 0.267f, 0.533f, 0.733f, 0.733f)] // rgb: 68, 136, 187
        [TestCase("#FFFFFF", 1f, 1f, 1f, 1f)] // rgb: 255,255,255 
        public void HextoColor_DoesValidConversion(string hex, float r, float g, float b, float a)
        {
            AssertColorsEqual(new Color(r, g, b, a), MaterialUtils.HexToColor(hex));
        }

        [TearDown]
        public void Cleanup() { }
    }
}
#endif
