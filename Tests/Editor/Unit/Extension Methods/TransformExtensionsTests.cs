using NUnit.Framework;
using UnityEngine;
using UnityEditor.Experimental.EditorVR.Extensions;

namespace UnityEditor.Experimental.EditorVR.Tests.Extensions
{
    [TestFixture]
    public class TransformExtensionsTests
    {
        GameObject go;

        [OneTimeSetUp]
        public void Setup()
        {
            go = new GameObject();
        }

        [Test]
        public void TransformBounds_TranslatesLocalBoundsToWorld()
        {
            var size = new Vector3(2, 2, 2);
            var offset = new Vector3(1, 2, 3);
            go.transform.position += offset;
            var localBounds = new Bounds(go.transform.position, size);
            var worldBounds = go.transform.TransformBounds(localBounds);

            Assert.AreEqual(localBounds.center, worldBounds.center - offset);
            Assert.AreEqual(localBounds.extents, worldBounds.extents);
        }
    }
}
