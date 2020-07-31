using NUnit.Framework;
using UnityEngine;
using Unity.EditorXR.Extensions;

namespace Unity.EditorXR.Tests.Extensions
{
    [TestFixture]
    public class TransformExtensionsTests
    {
        GameObject go;
        float delta = 0.00000001f;

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
            var local = new Bounds(go.transform.position, size);
            var world = go.transform.TransformBounds(local);

            Assert.That(local.center, Is.EqualTo(world.center - offset).Within(delta));
            Assert.That(local.extents, Is.EqualTo(world.extents).Within(delta));
        }
    }
}
