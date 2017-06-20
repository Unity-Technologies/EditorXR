#if UNITY_EDITOR
using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Provides access to checks that can test whether a ray is active
	/// </summary>
	public interface IIsRayActive
	{
	}

	public static class IIsRayActiveMethods
	{
		internal static Func<Transform, bool> isRayActive { get; set; }

		/// <summary>
		/// Returns whether the specified ray is active
		/// </summary>
		/// <param name="rayOrigin">The rayOrigin that is being checked</param>
		public static bool IsRayActive(this IIsRayActive obj, Transform rayOrigin)
		{
			return isRayActive(rayOrigin);
		}
	}
}
#endif