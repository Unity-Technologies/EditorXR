using UnityEngine;
using UnityEngine.UI;
using NUnit.Framework;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEditor.Experimental.EditorVR.Tests.TestHelpers;

namespace UnityEditor.Experimental.EditorVR.Tests.Utilities
{
    public class MaterialUtilsTests
    {
        GameObject go;
        Renderer renderer;
        Graphic graphic;
        Material clone;

        [OneTimeSetUp]
        public void Setup()
        {
            go = new GameObject("renderer object");
            Shader shader = Shader.Find("Standard");

            renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = new Material(shader);
            graphic = go.AddComponent<TestImage>();
            graphic.material = renderer.sharedMaterial;
        }

        [Test]
        public void GetMaterialClone_ClonesRendererSharedMaterial()
        {
            clone = MaterialUtils.GetMaterialClone(renderer);
            Assert.AreEqual(renderer.sharedMaterial, clone);
            ObjectUtils.Destroy(clone);
        }

        [Test]
        public void GetMaterialClone_ClonesGraphicMaterial()
        {
            clone = MaterialUtils.GetMaterialClone(graphic);
            Assert.AreEqual(graphic.material, clone);
            ObjectUtils.Destroy(clone);
        }

        // normally you can directly assert equality on Colors, but 
        // creating them based on the float coming from this results in mismatches due to rounding
        private void AssertColorsEqual(Color expected, Color actual)
        {
            float tolerance = 0.334f;
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(tolerance));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(tolerance));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(tolerance));
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(tolerance));
        }

        [TestCase("#000000", 0f, 0f, 0f, 1f)]                      // rgb: 0, 0, 0
        [TestCase("#002244", 0f, 0.133f, 0.267f, 1f)]              // rgb: 136, 221, 102
        [TestCase("#4488BBBB", 0.267f, 0.533f, 0.733f, 0.733f)]    // rgb: 68, 136, 187
        [TestCase("#FFFFFF", 1f, 1f, 1f, 1f)]                      // rgb: 255,255,255 
        public void HextoColor_DoesValidConversion(string hex, float r, float g, float b, float a)
        {
            AssertColorsEqual(new Color(r, g, b, a), MaterialUtils.HexToColor(hex));
        }

        [TearDown]
        public void Cleanup() {}
    }

}
