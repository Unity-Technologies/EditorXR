using System;

namespace UnityEngine.VR.Modules
{
	public interface IDropReciever
	{
		Action<Transform, IDropReciever, GameObject> setCurrentDropReciever { set; }
		Func<Transform, object> getCurrentDropObject { set; }

		/// <summary>
		/// Called when an object is hovering over the reciever
		/// </summary>
		/// <param name="droppedObject">The object we are dropping</param>
		/// <returns>Whether the drop can be accepted</returns>
		bool TestDrop(GameObject target, object droppedObject);
		/// <summary>
		/// Called when an object is dropped on the reciever
		/// </summary>
		/// <param name="droppedObject">The object we are dropping</param>
		/// <returns>Whether the drop was accepted</returns>
		bool RecieveDrop(GameObject target, object droppedObject);
	}
}