using System;
using UnityEngine;

/// <summary>
/// Gives access to the selection module
/// </summary>
public interface ISelectObject
{
	/// <summary>
	/// Given a hovered object, get the object which will be selected by SelectObject
	/// GameObject: The hovered object
	/// returns null if the selection will fail, the prefab root if there is one, or the original hover object
	/// </summary>
	Func<GameObject, GameObject> getSelectObject { set; }

	/// <summary>
	/// Select the given object using the given rayOrigin
	/// GameObject: the hovered object
	/// Transform: the rayOrigin used for selection
	/// returns whether the selection succeeded
	/// </summary>
	Action<GameObject, Transform, bool> selectObject { set; }
}
