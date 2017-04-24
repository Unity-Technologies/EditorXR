#if UNITY_EDITOR
using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	interface IGetPointerLength
	{
	}

	static class IGetPointerLengthMethods
	{
		internal static Func<Transform, float> getPointerLength { get; set; }

		public static float GetPointerLength(this IGetPointerLength obj, Transform rayOrigin)
		{
			return getPointerLength(rayOrigin);
		}
	}
}
#endif
