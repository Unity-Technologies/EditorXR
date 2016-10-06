using UnityEngine;

namespace UnityEngine.VR.Extensions
{
	public static class MonoBehaviourExtensions
	{
		public static void StopCoroutine(this MonoBehaviour mb, ref Coroutine coroutine)
		{
			if (coroutine != null)
			{
				mb.StopCoroutine(coroutine);
				coroutine = null;
			}
		}
	}
}