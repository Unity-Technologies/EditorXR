using UnityEngine;
using UnityEditor.Experimental.EditorVR;

namespace UnityEditor.Experimental.EditorVR.Extensions
{
	internal static class IGrabObjectsExtension
	{
		public static void DropHeldObjects(this IGrabObjects grabObjects, Transform rayOrigin)
		{
			Vector3[] positionOffset;
			Quaternion[] rotationOffset;
			grabObjects.DropHeldObjects(rayOrigin, out positionOffset, out rotationOffset);
		}
	}
}
