using System.Collections;
using UnityEngine;

namespace Unity.EditorXR.Extensions
{
    static class MonoBehaviourExtensions
    {
        public static void StopCoroutine(this MonoBehaviour mb, ref Coroutine coroutine)
        {
            if (coroutine != null)
            {
                mb.StopCoroutine(coroutine);
                coroutine = null;
            }
        }

        public static void RestartCoroutine(this MonoBehaviour mb, ref Coroutine coroutine, IEnumerator routine)
        {
            mb.StopCoroutine(ref coroutine);
            coroutine = mb.StartCoroutine(routine);
        }
    }
}
