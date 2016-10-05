using System;

namespace UnityEngine.VR.Modules
{
	public interface IDropReceiver
	{
		/// <summary>
		/// Sets the given IDropReciever as active for the given target (GameObject) and rayOrigin (Transform)
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
		bool TestDrop(GameObject target, object droppedObject);

		/// <summary>
		/// Called when an object is dropped on the receiver
		/// </summary>
		/// <param name="target">The GameObject with which the pointer is intersecting</param>
		/// <param name="droppedObject">The object we are dropping</param>
		/// <returns>Whether the drop was accepted</returns>
		bool ReceiveDrop(GameObject target, object droppedObject);
	}
}