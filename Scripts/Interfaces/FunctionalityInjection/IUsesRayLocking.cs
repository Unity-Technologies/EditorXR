#if UNITY_EDITOR
using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Gives decorated class the ability to lock and unlock the default ray
	/// </summary>
	public interface IUsesRayLocking
	{
	}

	public static class IUsesRayLockingMethods
	{
		internal delegate bool RayLockingDelegate(Transform rayOrigin, object obj, int priority = 0);

		internal static RayLockingDelegate lockRay { get; set; }
		internal static Func<Transform, object, bool> unlockRay { get; set; }

		/// <summary>
		/// Lock the default ray's show/hide state.
		/// </summary>
		/// <param name="rayOrigin">The ray to lock</param>
		/// <param name="obj">The object performing the lock is passed in and must be used for unlocking</param>
		/// <param name="priority">The priority used for locking (higher is more important)</param>
		public static void LockRay(this IUsesRayLocking customRay, Transform rayOrigin, object obj, int priority = 0)
		{
			lockRay(rayOrigin, obj);
		}

		/// <summary>
		/// Unlock the default ray's show/hide state.
		/// </summary>
		/// <param name="rayOrigin">The ray to unlock</param>
		/// <param name="obj">The object performing the unlock must be passed in and match the one that locked it or null to override</param>
		public static void UnlockRay(this IUsesRayLocking customRay, Transform rayOrigin, object obj)
		{
			unlockRay(rayOrigin, obj);
		}
	}

}
#endif
