using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	public delegate void TransformWithSnappingDelegate(Transform rayOrigin, GameObject[] objects, ref Vector3 position, ref Quaternion rotation, Vector3 delta, bool constrained);

	public interface IUsesSnapping
	{
		TransformWithSnappingDelegate transformWithSnapping { set; }
		Action<Transform> clearSnappingState { set; }
	}
}
