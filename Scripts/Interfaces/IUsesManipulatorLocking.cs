using System;

namespace UnityEngine.Experimental.EditorVR
{
	/// <summary>
	/// Gives decorated class the ability to lock and unlock the visibility state of all manipulators
	/// </summary>
	public interface IUsesManipulatorLocking
	{
		/// <summary>
		/// Lock the visibility state of all manipulators
		/// object = The object performing the lock is passed in and must be used for unlocking
		/// returns whether the lock succeeded
		/// </summary>
		Func<object, bool> lockManipulatorsVisibility { set; }

		/// <summary>
		/// Unlock visibility state of all manipulators
		/// object = The object performing the unlock must be passed in and match the one that locked it or null to override
		/// returns whether the unlock succeeded
		/// </summary>
		Func<object, bool> unlockManipulatorsVisibility { set; }
	}
}