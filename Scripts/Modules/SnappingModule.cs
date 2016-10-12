using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR.Utilities;
using UnityEngine.VR.Tools;

[MainMenuItem("Snapping", "Settings", "Select snapping modes")]
public class SnappingModule : MonoBehaviour, IModule
{

	public GameObject moduleMenuPrefab
	{
		get { return m_ModuleMenuPrefab; }
	}
	[SerializeField]
	private GameObject m_ModuleMenuPrefab;

	private Dictionary<Transform, ObjectSnapData> m_SnapDataTable = new Dictionary<Transform, ObjectSnapData>();

	private const int kFramesToKeep = 90;

	private class ObjectSnapData
	{
		internal MeshFilter meshFilter;
		internal Vector3 startPosition;

		internal List<Vector3> movementDirections;
		internal List<float> movementTimestamps;

		internal Vector3 throwDirection;
		internal float startVelocity;
		internal float currentVelocity;

		internal bool hasCollision;
		internal RaycastHit targetPoint;
		internal Vector3 closestVertex;
		internal float startDistance;
	}

	public void OnSnapStarted(Transform target, Vector3 deltaMovement, Transform[] raycastIgnore)
	{
		var meshFilter = target.GetComponent<MeshFilter>();
		ObjectSnapData snapData = new ObjectSnapData();

		snapData.meshFilter = meshFilter;
		snapData.startPosition = target.position;
		snapData.movementDirections = new List<Vector3>();
		snapData.movementTimestamps = new List<float>();
		m_SnapDataTable[target] = snapData;
	}

	public void OnSnapEnded(Transform target, Vector3 deltaMovement, Transform[] raycastIgnore)
	{
		var meshFilter = m_SnapDataTable[target].meshFilter;
		HandleThrowEnd(target, meshFilter, raycastIgnore);
	}

	public void OnSnapHeld(Transform target, Vector3 deltaMovement, Transform[] raycastIgnore)
	{
		MeshFilter meshFilter = null;
		if (m_SnapDataTable.ContainsKey(target))
			meshFilter = m_SnapDataTable[target].meshFilter;
		else
		{
			meshFilter = target.GetComponent<MeshFilter>();
			ObjectSnapData snapData = new ObjectSnapData();

			snapData.meshFilter = meshFilter;
			snapData.startPosition = target.position;
			m_SnapDataTable[target] = snapData;
		}

		UpdateMovementBuffers(target, deltaMovement);

		HandleGroundSnap(target, meshFilter, deltaMovement);
		HandleSurfaceSnap(target, meshFilter, raycastIgnore);
	}

	public void OnSnapUpdate(Transform target)
	{
		UpdateThrow(target);
	}

	void UpdateMovementBuffers(Transform target, Vector3 deltaMovement)
	{
		if (m_SnapDataTable[target].movementDirections == null)
			m_SnapDataTable[target].movementDirections = new List<Vector3>();
		if (m_SnapDataTable[target].movementTimestamps == null)
			m_SnapDataTable[target].movementTimestamps = new List<float>();

		m_SnapDataTable[target].movementDirections.Add(deltaMovement);
		m_SnapDataTable[target].movementTimestamps.Add(Time.realtimeSinceStartup);

		while (m_SnapDataTable[target].movementDirections.Count > kFramesToKeep)
			m_SnapDataTable[target].movementDirections.RemoveAt(0);
		while (m_SnapDataTable[target].movementTimestamps.Count > kFramesToKeep)
			m_SnapDataTable[target].movementTimestamps.RemoveAt(0);
	}

	private void HandleGroundSnap(Transform target, MeshFilter meshFilter, Vector3 deltaMovement)
	{
		if (U.Snapping.HasFlag(U.Snapping.SnappingModes.SnapToGround))
		{
			var closestVertex = U.Snapping.GetClosestVertex(meshFilter, Vector3.zero, Vector3.up);
			U.Snapping.SnapToGroundPlane(target, deltaMovement, closestVertex);
		}
	}

	private void HandleSurfaceSnap(Transform target, MeshFilter meshFilter, Transform[] raycastIgnore)
	{
		if (U.Snapping.HasFlag(U.Snapping.SnappingModes.SnapToSurfaceNormal))
		{
			Vector3 transformPosition = target.position;
			Vector3 origin = m_SnapDataTable[target].startPosition;
			Vector3 currentOffset = transformPosition - origin;
			RaycastHit hit;

			Ray ray = new Ray(origin, currentOffset);
			if (U.Snapping.GetRaySnapHit(
				ray,
				1000f,
				out hit,
				raycastIgnore))
			{
				var closestVertex = U.Snapping.GetClosestVertex(meshFilter, hit.point, hit.normal);

				float baseDistance = Vector3.Distance(origin, hit.point);
				float currentDistance = Vector3.Distance(origin, transformPosition + closestVertex);

				if (currentDistance > baseDistance)
					target.position = hit.point - closestVertex;
			}
		}
	}

	private void HandleThrowEnd(Transform target, MeshFilter meshFilter, Transform[] raycastIgnore)
	{
		if (U.Snapping.HasFlag(U.Snapping.SnappingModes.Throw))
		{
			if (m_SnapDataTable.ContainsKey(target))
			{
				var movementBuffer = m_SnapDataTable[target].movementDirections;
				var timestampBuffer = m_SnapDataTable[target].movementTimestamps;

				int count = movementBuffer.Count;

				if (count > 1)
				{
					Vector3 lastDirection = movementBuffer[count - 1];
					Vector3 total = lastDirection;

					float firstTime = timestampBuffer[count - 1];
					float time = 0;

					for (int i = count - 2; i > 0; i--)
					{
						if (Vector3.Angle(lastDirection, movementBuffer[i]) < 30)
						{
							total += movementBuffer[i];
							time = firstTime - timestampBuffer[i];
						}
						else
							break;
					}

					float totalMagnitude = total.magnitude;
					if (totalMagnitude > 1 && time > 0.1f)
						SetupThrow(target, meshFilter, raycastIgnore, total / totalMagnitude, totalMagnitude, time);
				}
			}
		}
	}

	private void SetupThrow(Transform target, MeshFilter meshFilter, Transform[] raycastIgnore, Vector3 throwDirection, float distance, float throwTime)
	{
		var snapData = m_SnapDataTable[target];

		snapData.throwDirection = throwDirection;
		float velocity = distance / throwTime;
		snapData.startVelocity = velocity;
		snapData.currentVelocity = velocity;

		Vector3 transformPosition = target.position;
		Ray ray = new Ray(transformPosition, throwDirection);
		RaycastHit hit;

		snapData.hasCollision = U.Snapping.GetRaySnapHit(ray, distance * 100f, out hit, raycastIgnore);

		snapData.targetPoint = hit;
		if (snapData.hasCollision)
		{
			snapData.closestVertex = U.Snapping.GetClosestVertex(meshFilter, hit.point, hit.normal, true);
			snapData.startDistance = Vector3.Distance(transformPosition, hit.point);
		}

		m_SnapDataTable[target] = snapData;
	}

	private void UpdateThrow(Transform target)
	{
		if (U.Snapping.HasFlag(U.Snapping.SnappingModes.Throw))
		{
			if (m_SnapDataTable.ContainsKey(target))
			{
				var snapData = m_SnapDataTable[target];
				if (snapData.currentVelocity > 0)
				{
					float deltaTime = Time.unscaledDeltaTime;
					float deltaVelocity = snapData.currentVelocity * deltaTime;

					if (!float.IsNaN(deltaVelocity))
						target.position += snapData.throwDirection * deltaVelocity;

					snapData.currentVelocity -= snapData.startVelocity * deltaTime;

					if (snapData.hasCollision)
					{
						float currentDistance = Vector3.Distance(target.position + snapData.closestVertex, snapData.targetPoint.point);

						bool isClose = currentDistance < deltaVelocity;
						bool overshot = currentDistance > snapData.startDistance;

						if (isClose || overshot)
						{
							Vector3 targetPosition = snapData.targetPoint.point - snapData.closestVertex;
							target.position = targetPosition;
							snapData.currentVelocity = -1;
						}
					}
					m_SnapDataTable[target] = snapData;
				}
			}
		}
	}

}
