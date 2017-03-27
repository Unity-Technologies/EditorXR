using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	public interface IUsesSnapping
	{
	}

	public static class IUsesSnappingMethods
	{
		internal delegate bool TransformWithSnappingDelegate(Transform rayOrigin, GameObject[] objects, ref Vector3 position, ref Quaternion rotation, Vector3 delta, bool constrained);
		internal delegate bool DirectTransformWithSnappingDelegate(Transform rayOrigin, GameObject[] objects, ref Vector3 position, ref Quaternion rotation, Vector3 targetPosition, Quaternion targetRotation);

		internal static TransformWithSnappingDelegate translateWithSnapping { get; set; }
		internal static DirectTransformWithSnappingDelegate directTransformWithSnapping { get; set; }
		internal static Action<Transform> clearSnappingState { get; set; }

		/// <summary>
		/// Lock the default ray's show/hide state.
		/// </summary>
		/// <param name="rayOrigin">The ray to lock</param>
		/// <param name="obj">The object performing the lock is passed in and must be used for unlocking</param>
		public static bool TranslateWithSnapping(this IUsesSnapping usesSnaping, Transform rayOrigin, GameObject[] objects, ref Vector3 position, ref Quaternion rotation, Vector3 delta, bool constrained)
		{
			return translateWithSnapping(rayOrigin, objects, ref position, ref rotation, delta, constrained);
		}

		/// <summary>
		/// Unlock the default ray's show/hide state.
		/// </summary>
		/// <param name="rayOrigin">The ray to unlock</param>
		/// <param name="obj">The object performing the unlock must be passed in and match the one that locked it or null to override</param>
		public static bool DirectTransformWithSnapping(this IUsesSnapping usesSnaping, Transform rayOrigin, GameObject[] objects, ref Vector3 position, ref Quaternion rotation, Vector3 targetPosition, Quaternion targetRotation)
		{
			return directTransformWithSnapping(rayOrigin, objects, ref position, ref rotation, targetPosition, targetRotation);
		}

		/// <summary>
		/// Unlock the default ray's show/hide state.
		/// </summary>
		/// <param name="rayOrigin">The ray to unlock</param>
		/// <param name="obj">The object performing the unlock must be passed in and match the one that locked it or null to override</param>
		public static void ClearSnappingState(this IUsesSnapping usesSnaping, Transform rayOrigin)
		{
			clearSnappingState(rayOrigin);
		}
	}
}
