using UnityEngine;

namespace UnityEngine.VR.Handles
{
	/// <summary>
	/// Event data for BaseHandle.DragEventCallback
	/// </summary>
	public struct HandleDragEventData
	{
		public Vector3 deltaPosition;
		public Quaternion deltaRotation;
		public Transform rayOrigin;

		public HandleDragEventData(Vector3 deltaPos, Quaternion deltaRot, Transform rayOrigin = null)
		{
			this.rayOrigin = rayOrigin;
			deltaPosition = deltaPos;
			deltaRotation = deltaRot;
		}

		public HandleDragEventData(Vector3 deltaPos)
		{
			deltaPosition = deltaPos;
			deltaRotation = Quaternion.identity;
			rayOrigin = null;
		}

		public HandleDragEventData(Quaternion deltaRot)
		{
			deltaPosition = Vector3.zero;
			deltaRotation = deltaRot;
			rayOrigin = null;
		}
		public HandleDragEventData(Transform rayOrigin)
		{
			deltaPosition = Vector3.zero;
			deltaRotation = Quaternion.identity;
			this.rayOrigin = rayOrigin;
		}
	}
}