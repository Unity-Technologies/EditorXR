using System;

namespace UnityEngine.VR.Modules
{
	/// <summary>
	/// Gets a drop receiver from the system
	/// </summary>
	/// <param name="rayOrigin">The rayOrigin we are checking (which hand is hovering)</param>
	/// <param name="target">The GameObject which the DropReceiver is attached to</param>
	/// <returns>The current drop receiver</returns>
	public delegate IDropReceiver GetDropReceiverDelegate(Transform rayOrigin, out GameObject target);

	/// <summary>
	/// Implementors can be dropped on IDropReceivers by calling setCurrentDropObject with the hovering rayOrigin and themselves
	/// Call getCurrentDropReceiver and ReceiveDrop on the returned object to drop
	/// </summary>
	public interface IDroppable
	{
		/// <summary>
		/// Gets the current drop receiver for the given rayOrigin (Transform), along with the target object (out GameObject)
		/// </summary>
		GetDropReceiverDelegate getCurrentDropReceiver { set; }

		/// <summary>
		/// Sets the current drop object for the given rayOrign (Transform)
		/// </summary>
		Action<Transform, object> setCurrentDropObject { set; }
	}
}