using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	public delegate void TransformWithSnappingDelegate(Transform rayOrigin, GameObject[] objects, ref Vector3 position, ref Quaternion rotation, Vector3 delta, bool constrained);
	public delegate bool DirectTransformWithSnappingDelegate(Transform rayOrigin, GameObject[] objects, ref Vector3 position, ref Quaternion rotation, Vector3 targetPosition, Quaternion targetRotation);

	public interface IUsesSnapping
	{
		TransformWithSnappingDelegate transformWithSnapping { set; }
		DirectTransformWithSnappingDelegate directTransformWithSnapping { set; }
		Action<Transform> clearSnappingState { set; }
	}
}
