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

	/// <summary>
	/// Adds the given object to the held objects for the given node and rayOrigin
	/// </summary>
	/// <param name="node">The node associtated with the rayOrigin</param>
	/// <param name="rayOrigin">The rayOrigin to attach the object to</param>
	/// <param name="grabbedObject">The object we are adding</param>
	/// <param name="input">The input used to control selection</param>
	void AddHeldObject(Node node, Transform rayOrigin, Transform grabbedObject, ActionMapInput input);

	/// <summary>
	/// Returns true if the object can be grabbed
	/// Params: the selection, the rayOrigin
	/// </summary>
	Func<DirectSelectionData, Transform, bool> canGrabObject { set; }

	/// <summary>
	/// Informs EditorVR 
	/// Params: the implementor, the selection, the rayOrigin, returns whether the grab succeeded
	/// </summary>
	Func<IGrabObject, DirectSelectionData, Transform, bool> grabObject { set; }

	/// <summary>
	/// Informs EditorVR 
	/// Params: the implementor, the selected object, the rayOrigin
	/// </summary>
	Action<IGrabObject, Transform, Transform> dropObject { set; }
}