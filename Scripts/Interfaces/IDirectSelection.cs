using System;
using System.Collections.Generic;
using UnityEngine.InputNew;

namespace UnityEngine.VR.Modules
{
	/// <summary>
	/// Gives decorated class access to direct selections
	/// </summary>
	public interface IDirectSelection
	{
		/// <summary>
		/// ConnectInterfaces provides a delegate which can be called to get a dictionary of the current direct selection
		/// Key is the rayOrigin used to select the object
		/// Value is a data class containing the selected object and metadata
		/// </summary>
		Func<Dictionary<Transform, DirectSelection>> getDirectSelection { set; }

		/// <summary>
		/// Drop a currently held object
		/// </summary>
		/// <param name="obj">The object we wish to drop</param>
		void DropHeldObject(Transform obj);

		/// <summary>
		/// Drop a currently held object, getting its current offset
		/// </summary>
		/// <param name="obj">The object we wish to drop</param>
		/// <param name="positionOffset">The position offset between the rayOrigin and the object</param>
		/// <param name="rotationOffset">The rotation offset between the rayOrigin and the object</param>
		void DropHeldObject(Transform obj, out Vector3 positionOffset, out Quaternion rotationOffset);

		/// <summary>
		/// Get the object held by a given rayOrign
		/// </summary>
		/// <param name="rayOrigin">The rayOrigin to query</param>
		/// <returns></returns>
		Transform GetHeldObject(Transform rayOrigin);

		/// <summary>
		/// Transfer a held object between rayOrigins (i.e. dragging into the MiniWorld)
		/// </summary>
		/// <param name="rayOrigin">rayOrigin of current held object</param>
		/// <param name="input">DirectSelect ActionMapInput for the holding ray</param>
		/// <param name="destRayOrigin">Destination rayOrigin</param>
		/// <param name="deltaOffset">Change in position offset (added to GrabData.positionOffset)</param>
		void TransferHeldObject(Transform rayOrigin, ActionMapInput input, Transform destRayOrigin, Vector3 deltaOffset);

		void AddHeldObject(Node node, Transform rayOrigin, Transform grabbedObject, ActionMapInput input);
	}
}