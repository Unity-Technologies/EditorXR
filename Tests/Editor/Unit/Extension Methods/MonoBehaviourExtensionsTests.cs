using System.Collections;
using NUnit.Framework;
using UnityEngine;
using Unity.EditorXR.Extensions;
using Unity.EditorXR.Proxies;

namespace Unity.EditorXR.Tests.Extensions
{
    class MonoBehaviourExtensionsTests
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
