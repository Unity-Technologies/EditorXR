using System;
using UnityEngine;
using UnityEngine.InputNew;
using UnityEngine.Experimental.EditorVR.Tools;
using UnityEngine.Experimental.EditorVR.Modules;

/// <summary>
/// Provides methods and delegates used to directly select and grab scene objects
/// </summary>
public interface IGrabObject
{
	/// <summary>
	/// Drop a currently held object
	/// </summary>
	/// <param name="rayOrigin">The object we wish to drop</param>
	void DropHeldObjects(Transform rayOrigin);

	/// <summary>
	/// Drop a currently held object, getting its current offset
	/// </summary>
	/// <param name="rayOrigin">The rayOrigin that was holding the objects</param>
	/// <param name="positionOffset">The position offset between the rayOrigin and the object</param>
	/// <param name="rotationOffset">The rotation offset between the rayOrigin and the object</param>
	void DropHeldObjects(Transform rayOrigin, out Vector3[] positionOffset, out Quaternion[] rotationOffset);

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
	/// <param name="input">DirectSelect ActionMapInput for the holding ray</param>
	/// <param name="destRayOrigin">Destination rayOrigin</param>
	/// <param name="deltaOffset">Change in position offset (added to GrabData.positionOffset)</param>
	void TransferHeldObjects(Transform rayOrigin, Transform destRayOrigin, Vector3 deltaOffset);

	/// <summary>
	/// Adds the given objects to the held objects for the given node and rayOrigin
	/// </summary>
	/// <param name="node">The node associtated with the rayOrigin</param>
	/// <param name="rayOrigin">The rayOrigin to attach the object to</param>
	/// <param name="input">The input used to control selection</param>
	/// <param name="objects">The objects being grabbed</param>
	void GrabObjects(Node node, Transform rayOrigin, ActionMapInput input, Transform[] objects);

	/// <summary>
	/// Returns true if the object can be grabbed
	/// Params: the selection, the rayOrigin
	/// </summary>
	Func<GameObject, Transform, bool> canGrabObject { set; }

	/// <summary>
	/// Informs EditorVR that a group of objects was grabbed
	/// Params: the grabber, the selected objects, the rayOrigin, returns whether the grab succeeded
	/// </summary>
	Func<IGrabObject, GameObject, Transform, bool> grabObject { set; }

	/// <summary>
	/// Informs EditorVR that a group of objects was dropped
	/// Params: the grabber, the selected objects, the rayOrigin
	/// </summary>
	Action<IGrabObject, Transform[], Transform> dropObjects { set; }
}