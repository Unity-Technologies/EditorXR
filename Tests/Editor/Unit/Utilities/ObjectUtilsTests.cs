using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.TestTools;
using UnityEditor.Experimental.EditorVR.Utilities;

namespace UnityEditor.Experimental.EditorVR.Tests.Utilities
{
    [TestFixture]
    public class ObjectUtilsTests
    {
		GameObject go, parent, other;
		GameObject currentObject;
		List<GameObject> toCleanupAfterEach = new List<GameObject>();

		float delta = 0.00000001f;

		[SetUp]
		public void BeforeEach()
		{
			go = new GameObject("object utils test");
			parent = new GameObject("parent");
			parent.transform.position += new Vector3(2, 4, 8);
			other = new GameObject("other");
			other.transform.position += new Vector3(-5, 1, 2);

			toCleanupAfterEach.AddRange(new GameObject[] { go, parent, other });
		}

		[Test]
		public void Instantiate_OneArg_ClonesActiveAtOrigin()
		{
			currentObject = ObjectUtils.Instantiate(go);
			Assert.IsTrue(currentObject.activeSelf);
			Assert.AreEqual(new Vector3(0, 0, 0), currentObject.transform.position);
		}

		[Test]
		public void Instantiate_InactiveClone()
		{
			currentObject = ObjectUtils.Instantiate(go, null, true, true, false);
			Assert.IsFalse(currentObject.activeSelf);
		}

		[Test]
		public void Instantiate_WithParent_WorldPositionStays()
		{
			currentObject = ObjectUtils.Instantiate(go, parent.transform);
			Assert.AreEqual(parent.transform, currentObject.transform.parent);
			Assert.AreNotEqual(parent.transform.position, currentObject.transform.position);
		}

		[Test]
		public void Instantiate_WithParent_WorldPositionMoves()
		{
			currentObject = ObjectUtils.Instantiate(go, parent.transform, false);
			Assert.AreEqual(parent.transform, currentObject.transform.parent);
			Assert.AreEqual(parent.transform.position, currentObject.transform.position);
			Assert.AreEqual(parent.transform.rotation, currentObject.transform.rotation);
		}

		[TestCase(true)]
		[TestCase(false)]
		public void Instantiate_SetRunInEditMode(bool expected)
		{
			Assert.IsFalse(Application.isPlaying);
			currentObject = ObjectUtils.Instantiate(go, null, true, expected);
			AssertRunInEditModeSet(currentObject, expected);
		}

		[UnityTest]
		public IEnumerator Destroy_OneArg_DestroysImmediately_InEditMode()
		{
			Assert.IsFalse(Application.isPlaying);
			ObjectUtils.Destroy(other);
			yield return null;              // skip frame to allow destruction to run
			Assert.IsTrue(other == null);
		}

		// here, we could test the other types of calls to Destroy / Instantiate, but that
		// would require refactor / making some things "internal" instead of private,
		// as well as figuring out how we want to do mocking / stubs - hard to test coroutines

		[Test]
		public void CreateGameObjectWithComponent_OneArg_TypeAsGeneric()
		{
			var renderer = ObjectUtils.CreateGameObjectWithComponent<MeshRenderer>();
			currentObject = renderer.gameObject;
			// the object name assigned is based on the component's type name
			var foundObject = GameObject.Find(typeof(MeshRenderer).Name);
			Assert.IsInstanceOf<MeshRenderer>(renderer);
			Assert.IsInstanceOf<MeshRenderer>(foundObject.GetComponent<MeshRenderer>());
		}

		[Test]
		public void CreateGameObjectWithComponent_SetsParent_WorldPositionStays()
		{
			var comp = ObjectUtils.CreateGameObjectWithComponent<MeshRenderer>(parent.transform);
			currentObject = comp.gameObject;
			Assert.AreEqual(parent.transform, comp.transform.parent);
			Assert.AreNotEqual(parent.transform.position, comp.transform.position);
		}

		[Test]
		public void CreateGameObjectWithComponent_SetsParent_WorldPositionMoves()
		{
			var comp = ObjectUtils.CreateGameObjectWithComponent<MeshRenderer>(parent.transform, false);
			currentObject = comp.gameObject;
			Assert.AreEqual(parent.transform, comp.transform.parent);
			Assert.AreEqual(parent.transform.position, comp.transform.position);
		}

		[Test]
		public void AddComponent_AddsToObject_TypeAsGeneric()
		{
			var instance = ObjectUtils.AddComponent<MeshRenderer>(other);
			var onObject = other.GetComponent<MeshRenderer>();
			Assert.IsInstanceOf<MeshRenderer>(instance);
			Assert.AreEqual(instance, onObject);
			AssertRunInEditModeSet(other, true);
		}

		[Test]
		public void AddComponent_AddsToObject_TypeAsArg()
		{
			var instance = ObjectUtils.AddComponent(typeof(MeshRenderer), other);
			var onObject = other.GetComponent<MeshRenderer>();
			Assert.IsInstanceOf<Component>(instance);
			Assert.AreEqual((MeshRenderer)instance, onObject);
			AssertRunInEditModeSet(other, true);
		}

		[Test]
		public void GetBounds_WithoutExtents()
		{
			var localBounds = new Bounds(other.transform.position, new Vector3(0, 0, 0));
			var bounds = ObjectUtils.GetBounds(other.transform);

			Assert.AreEqual(localBounds, bounds);
		}

		[Test]
		public void GetBounds_Array()
		{
			var boundsA = new GameObject();
			boundsA.transform.position += new Vector3(-5, -2, 8);
			var boundsB = new GameObject();
			boundsB.transform.position += new Vector3(2, 6, 4);
			var transforms = new Transform[] { boundsA.transform, boundsB.transform };

			// if you want to work with more than one object in a test, add them to cleanup list manually
			toCleanupAfterEach.AddRange(new GameObject[] { boundsA, boundsB });

			var bounds = ObjectUtils.GetBounds(transforms);
			Bounds expected = new Bounds(new Vector3(-1.5f, 2f, 6f), new Vector3(7f, 8f, 4f));

			Assert.That(bounds, Is.EqualTo(expected).Within(delta));
		}

		[TearDown]
		public void CleanupAfterEach()
		{
			if (currentObject != null)
				toCleanupAfterEach.Add(currentObject);

			foreach (var o in toCleanupAfterEach)
			{
				ObjectUtils.Destroy(o);
			}
		}

		// this doesn't actually do it recursively yet
		private void AssertRunInEditModeSet(GameObject go, bool expected)
		{
			var MBs = go.GetComponents<MonoBehaviour>();
			foreach (var mb in MBs)
			{
				Assert.AreEqual(expected, mb.runInEditMode);
			}
		}
    }
}
