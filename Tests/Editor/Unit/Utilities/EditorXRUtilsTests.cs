using NUnit.Framework;
using System.Collections.Generic;
using Unity.Labs.EditorXR.Utilities;
using Unity.Labs.Utils;
using UnityEngine;

namespace Unity.Labs.EditorXR.Tests.Utilities
{
    [TestFixture]
    public class EditorXRUtilsTests
    {
        GameObject m_GO, m_Parent, m_Other;
        List<GameObject> m_ToCleanupAfterEach = new List<GameObject>();

        [SetUp]
        public void BeforeEach()
        {
            m_GO = new GameObject("object utils test");
            m_Parent = new GameObject("parent");
            m_Parent.transform.position += new Vector3(2, 4, 8);
            m_Other = new GameObject("other");
            m_Other.transform.position += new Vector3(-5, 1, 2);

            m_ToCleanupAfterEach.AddRange(new[] { m_GO, m_Parent, m_Other });
        }

        [Test]
        public void Instantiate_OneArg_ClonesActiveAtOrigin()
        {
            var clone = EditorXRUtils.Instantiate(m_GO);
            Assert.IsTrue(clone.activeSelf);
            Assert.AreEqual(new Vector3(0, 0, 0), clone.transform.position);
            m_ToCleanupAfterEach.Add(clone);
        }

        [Test]
        public void Instantiate_InactiveClone()
        {
            var clone = EditorXRUtils.Instantiate(m_GO, null, true, true, false);
            Assert.IsFalse(clone.activeSelf);
            m_ToCleanupAfterEach.Add(clone);
        }

        [Test]
        public void Instantiate_WithParent_WorldPositionStays()
        {
            var clone = EditorXRUtils.Instantiate(m_GO, m_Parent.transform);
            Assert.AreEqual(m_Parent.transform, clone.transform.parent);
            Assert.AreNotEqual(m_Parent.transform.position, clone.transform.position);
            m_ToCleanupAfterEach.Add(clone);
        }

        [Test]
        public void Instantiate_WithParent_WorldPositionMoves()
        {
            var clone = EditorXRUtils.Instantiate(m_GO, m_Parent.transform, false);
            Assert.AreEqual(m_Parent.transform, clone.transform.parent);
            Assert.AreEqual(m_Parent.transform.position, clone.transform.position);
            Assert.AreEqual(m_Parent.transform.rotation, clone.transform.rotation);
            m_ToCleanupAfterEach.Add(clone);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Instantiate_SetRunInEditMode(bool expected)
        {
            Assert.IsFalse(Application.isPlaying);
            var clone = EditorXRUtils.Instantiate(m_GO, null, true, expected);
            AssertRunInEditModeSet(clone, expected);
            m_ToCleanupAfterEach.Add(clone);
        }

        // here, we could test the other types of calls to Destroy / Instantiate, but that
        // would require refactor / making some things "internal" instead of private,
        // as well as figuring out how we want to do mocking / stubs - hard to test coroutines

        [Test]
        public void CreateGameObjectWithComponent_OneArg_TypeAsGeneric()
        {
            var renderer = EditorXRUtils.CreateGameObjectWithComponent<MeshRenderer>();
            m_ToCleanupAfterEach.Add(renderer.gameObject);

            // the object name assigned is based on the component's type name
            var foundObject = GameObject.Find(typeof(MeshRenderer).Name);
            Assert.IsInstanceOf<MeshRenderer>(renderer);
            Assert.IsInstanceOf<MeshRenderer>(foundObject.GetComponent<MeshRenderer>());
        }

        [Test]
        public void CreateGameObjectWithComponent_SetsParent_WorldPositionStays()
        {
            var comp = EditorXRUtils.CreateGameObjectWithComponent<MeshRenderer>(m_Parent.transform);
            m_ToCleanupAfterEach.Add(comp.gameObject);
            Assert.AreEqual(m_Parent.transform, comp.transform.parent);
            Assert.AreNotEqual(m_Parent.transform.position, comp.transform.position);
        }

        [Test]
        public void CreateGameObjectWithComponent_SetsParent_WorldPositionMoves()
        {
            var comp = EditorXRUtils.CreateGameObjectWithComponent<MeshRenderer>(m_Parent.transform, false);
            m_ToCleanupAfterEach.Add(comp.gameObject);
            Assert.AreEqual(m_Parent.transform, comp.transform.parent);
            Assert.AreEqual(m_Parent.transform.position, comp.transform.position);
        }

        [Test]
        public void AddComponent_AddsToObject_TypeAsGeneric()
        {
            var instance = EditorXRUtils.AddComponent<MeshRenderer>(m_Other);
            var onObject = m_Other.GetComponent<MeshRenderer>();
            Assert.IsInstanceOf<MeshRenderer>(instance);
            Assert.AreEqual(instance, onObject);
            AssertRunInEditModeSet(m_Other, true);
        }

        [Test]
        public void AddComponent_AddsToObject_TypeAsArg()
        {
            var instance = EditorXRUtils.AddComponent(typeof(MeshRenderer), m_Other);
            var onObject = m_Other.GetComponent<MeshRenderer>();
            Assert.IsInstanceOf<Component>(instance);
            Assert.AreEqual((MeshRenderer)instance, onObject);
            AssertRunInEditModeSet(m_Other, true);
        }

        [TearDown]
        public void CleanupAfterEach()
        {
            foreach (var o in m_ToCleanupAfterEach)
            {
                UnityObjectUtils.Destroy(o);
            }
        }

        // this doesn't actually do it recursively yet
        static void AssertRunInEditModeSet(GameObject go, bool expected)
        {
            var MBs = go.GetComponents<MonoBehaviour>();
            foreach (var mb in MBs)
            {
                Assert.AreEqual(expected, mb.runInEditMode);
            }
        }
    }
}
