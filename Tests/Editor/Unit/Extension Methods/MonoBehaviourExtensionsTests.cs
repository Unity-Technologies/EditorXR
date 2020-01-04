using System.Collections;
using NUnit.Framework;
using UnityEngine;
using Unity.Labs.EditorXR.Extensions;
using Unity.Labs.EditorXR.Proxies;

namespace Unity.Labs.EditorXR.Tests.Extensions
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
