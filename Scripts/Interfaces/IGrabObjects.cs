#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Provides methods and delegates used to directly select and grab scene objects
	/// </summary>
	public interface IGrabObjects : ICanGrabObject
	{
		/// <summary>
		/// Adds the given objects to the held objects for the given node and rayOrigin
		/// </summary>
		/// <param name="node">The node associated with the rayOrigin</param>
		/// <param name="rayOrigin">The rayOrigin to attach the object to</param>
		/// <param name="input">The input used to control selection</param>
		/// <param name="objects">The objects being grabbed</param>
		void GrabObjects(Node node, Transform rayOrigin, ActionMapInput input, Transform[] objects);

		/// <summary>
		/// Get the object held by a given rayOrign
		/// </summary>
		/// <param name="rayOrigin">The rayOrigin to query</param>
		/// <returns></returns>
		Transform[] GetHeldObjects(Transform rayOrigin);

		/// <summary>
		/// Transfer a held object between rayOrigins (i.e. dragging into the MiniWorld)
		/// </summary>
		/// <param name="rayOrigin">rayOrigin of current held object</param>
		/// <param name="destRayOrigin">Destination rayOrigin</param>
		/// <param name="deltaOffset">Change in position offset (added to GrabData.positionOffset)</param>
		void TransferHeldObjects(Transform rayOrigin, Transform destRayOrigin, Vector3 deltaOffset);

		/// <summary>
		/// Drop a currently held object, getting its current offset
		/// </summary>
		/// <param name="rayOrigin">The rayOrigin that was holding the objects</param>
		/// <param name="positionOffset">The position offset between the rayOrigin and the object</param>
		/// <param name="rotationOffset">The rotation offset between the rayOrigin and the object</param>
		void DropHeldObjects(Transform rayOrigin, out Vector3[] positionOffset, out Quaternion[] rotationOffset);

		/// <summary>
		/// Must be called by the implementer when an object has been grabbed
		/// Params: the grabbed object
		/// </summary>
		event Action<GameObject> objectGrabbed;

		/// <summary>
		/// Must be called by the implementer when objects have been dropped
		/// Params: the selected objects, the rayOrigin
		/// </summary>
		event Action<Transform[], Transform> objectsDropped;
	}

	public static class IGrabObjectsMethods
	{
		public static void DropHeldObjects(this IGrabObjects grabObjects, Transform rayOrigin)
		{
			Vector3[] positionOffset;
			Quaternion[] rotationOffset;
			grabObjects.DropHeldObjects(rayOrigin, out positionOffset, out rotationOffset);
		}
	}

}
#endif
