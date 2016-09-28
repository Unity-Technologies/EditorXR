using System;

namespace UnityEngine.VR.Modules
{
	public interface IDropReciever
	{
		Action<Transform, IDropReciever> setCurrentDropReciever { set; }

		/// <summary>
		/// Called when an object is dropped on the reciever
		/// </summary>
		/// <param name="droppedObject">The object we are dropping</param>
		/// <returns>Whether the drop was accepted</returns>
		bool OnDrop(object droppedObject);
	}
}