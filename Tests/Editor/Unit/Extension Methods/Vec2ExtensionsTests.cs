using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Unity.Labs.EditorXR.Extensions;

// MinComponent, MaxComponent, & Inverse are unused presently
namespace Unity.Labs.EditorXR.Tests.Extensions
{
    [InitializeOnLoad]
    public class Vec2ExtensionsTests
    {
        [Test]
        public void Abs_NegativeValues_AreInverted()
        {
            Assert.AreEqual(new Vector2(2f, 1f), new Vector2(-2f, -1f).Abs());
        }

        [Test]
        public void Abs_PositiveValues_AreUnchanged()
        {
            Assert.AreEqual(new Vector2(2f, 1f), new Vector2(2f, 1f).Abs());
        }
    }
}
