#if UNITY_EDITOR
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Extensions
{
	static class IGrabObjectsExtension
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
