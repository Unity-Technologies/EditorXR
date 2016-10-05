using System;

namespace UnityEngine.VR.Modules
{
	/// <summary>
	/// Gets a drop reciever from the system
	/// </summary>
	/// <param name="rayOrigin">The rayOrigin we are checking (which hand is hovering)</param>
	/// <param name="target">The GameObject which the DropReciever is attached to</param>
	/// <returns>The current drop reciever</returns>
	public delegate IDropReceiver GetDropReceiverDelegate(Transform rayOrigin, out GameObject target);

	public interface IDroppable
	{
		/// <summary>
		/// Gets the current drop reciever for the given rayOrigin (Transform), along with the target object (out GameObject)
		/// </summary>
		GetDropReceiverDelegate getCurrentDropReceiver { set; }

		/// <summary>
		/// Sets the current drop object for the given rayOrign (Transform)
		/// </summary>
		Action<Transform, object> setCurrentDropObject { set; }
	}
}