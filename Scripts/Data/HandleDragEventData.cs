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

		public HandleDragEventData(Vector3 deltaPos, Quaternion deltaRot, Transform rayOrigin)
		{
			this.rayOrigin = rayOrigin;
			deltaPosition = deltaPos;
			deltaRotation = deltaRot;
		}

		public HandleDragEventData(Vector3 deltaPos, Transform rayOrigin)
		{
			this.rayOrigin = rayOrigin;
			deltaPosition = deltaPos;
			deltaRotation = Quaternion.identity;
		}

		public HandleDragEventData(Quaternion deltaRot, Transform rayOrigin)
		{
			this.rayOrigin = rayOrigin;
			deltaPosition = Vector3.zero;
			deltaRotation = deltaRot;
		}
		public HandleDragEventData(Transform rayOrigin)
		{
			this.rayOrigin = rayOrigin;
			deltaPosition = Vector3.zero;
			deltaRotation = Quaternion.identity;
		}
	}
}