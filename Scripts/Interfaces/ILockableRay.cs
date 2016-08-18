using System;

namespace UnityEngine.VR.Tools
{
	public interface ILockableRay
	{
		/// <summary>
		/// Handle locking of the default ray's show/hide state.
		/// Require the object performing the lock to be passed in, its reference locally cached,
		/// and compared to for valid unlocking
		/// </summary>
		Action<object> lockRay { set; }

		/// <summary>
		/// Handle unlocking of the default ray's show/hide state.
		/// Require the object performing the unlock to be passed in and compared to the object
		/// reference used to lock the ray. Unlocking is allowed if the local locking object
		/// reference is valid or null
		/// </summary>
		Action<object> unlockRay { set; }
	}
}