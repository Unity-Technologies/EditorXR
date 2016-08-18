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

		public HandleDragEventData(Vector3 deltaPos, Quaternion deltaRot)
		{
			deltaPosition = deltaPos;
			deltaRotation = deltaRot;
		}

		public HandleDragEventData(Vector3 deltaPos)
		{
			deltaPosition = deltaPos;
			deltaRotation = Quaternion.identity;
		}

		public HandleDragEventData(Quaternion deltaRot)
		{
			deltaPosition = Vector3.zero;
			deltaRotation = deltaRot;
		}
	}
}