#if UNITY_5_6_OR_NEWER
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Proxies;

namespace UnityEditor.Experimental.EditorVR.Tests.Extensions
{
    public class MonoBehaviourExtensionsTests
    {
        MonoBehaviour mb;
        int coIndex = 0;

        IEnumerator routine()
        {
            yield return coIndex++;
        }

        [OneTimeSetUp]
        public void Setup()
        {
            mb = new GameObject().AddComponent<DefaultProxyRay>();
        }

        [Test]
        public void StopCoroutineOverload_NullifiesCoroutine()
        {
            Coroutine coroutine = mb.StartCoroutine(routine());
            Assert.IsNotNull(coroutine);
            mb.StopCoroutine(ref coroutine);
            Assert.IsNull(coroutine);
        }
    }
}
#endif
