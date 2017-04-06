using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	public interface IGetPointerLength
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
