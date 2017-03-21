using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	public delegate void TranslateWithSnappingDelegate(Transform rayOrigin, GameObject[] objects, ref Vector3 position, ref Quaternion rotation, Vector3 delta, bool constrained);

	public interface IUsesSnapping
	{
		TranslateWithSnappingDelegate translateWithSnapping { set; }
		Action<Transform> clearSnappingState { set; }
	}
}
