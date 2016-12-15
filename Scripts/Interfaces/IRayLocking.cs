using System;

namespace UnityEngine.Experimental.EditorVR.Tools
{
    /// <summary>
    /// Gives decorated class the ability to lock and unlock the default ray
    /// </summary>
    public interface IRayLocking
	{
		/// <summary>
		/// Lock the default ray's show/hide state.
		/// Transform = Ray origin
		/// object = The object performing the lock is passed in and must be used for unlocking
		/// </summary>
		Func<Transform, object, bool> lockRay { set; }

		/// <summary>
		/// Unlock the default ray's show/hide state.
		/// Transform = Ray origin
		/// object = The object performing the unlock must be passed in and match the one that locked it or null to override
		/// </summary>
		Func<Transform, object, bool> unlockRay { set; }
	}
}