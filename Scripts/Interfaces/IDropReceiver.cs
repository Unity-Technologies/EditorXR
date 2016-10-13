using System;

namespace UnityEngine.VR.Modules
{
	/// <summary>
	/// Implementors can recieve IDroppables by calling setDropReceiver with the hovering rayOrigin, itself, and an optional backing object
	/// CanDrop is called to check if the object can be dropped, and ReceiveDrop is called to do the actual handoff
	/// </summary>
	public interface IDropReceiver
	{
		/// <summary>
		/// Sets the given IDropReceiver as active for the given target (GameObject) and rayOrigin (Transform)
		/// </summary>
		Action<Transform, IDropReceiver, GameObject> setCurrentDropReceiver { set; }

		/// <summary>
		/// Gets the current drop object attached to the given rayOrigin (Transform)
		/// </summary>
		Func<Transform, object> getCurrentDropObject { set; }

		/// <summary>
		/// Called when an object is hovering over the receiver
		/// </summary>
		/// <param name="target">The GameObject with which the pointer is intersecting</param>
		/// <param name="droppedObject">The object we are dropping</param>
		/// <returns>Whether the drop can be accepted</returns>
		bool CanDrop(GameObject target, object droppedObject);

		/// <summary>
		/// Called when an object is dropped on the receiver
		/// </summary>
		/// <param name="target">The GameObject with which the pointer is intersecting</param>
		/// <param name="droppedObject">The object we are dropping</param>
		/// <returns>Whether the drop was accepted</returns>
		bool ReceiveDrop(GameObject target, object droppedObject);
	}
}