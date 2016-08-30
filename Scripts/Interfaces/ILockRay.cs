using System;

namespace UnityEngine.VR.Tools
{
	public interface ILockRay
	{
		/// <summary>
		/// Lock the default ray's show/hide state.
		/// The object performing the lock is passed in and must be used for unlocking
		/// </summary>
		Func<object, bool> lockRay { set; }

		/// <summary>
		/// Unlock the default ray's show/hide state.
		/// The object performing the unlock must be passed in and match the one that locked it or null to override
		/// </summary>
		Func<object, bool> unlockRay { set; }
	}
}