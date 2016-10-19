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
		internal MeshRenderer meshRenderer;
		internal Vector3 startPosition;
		internal Vector3 startCenter;

		internal List<Vector3> movementDirections;
		internal List<float> movementTimestamps;

		internal Vector3 throwDirection;
		internal float startVelocity;
		internal float currentVelocity;

		internal bool hasCollision;
		internal RaycastHit targetPoint;
		internal Vector3 closestVertex;
		internal float startDistance;
		internal Ray throwRay;

		internal Vector3? collisionPoint;
	}

	public void OnSnapStarted(Transform target, Vector3 deltaMovement, Transform[] raycastIgnore)
	{
		var meshFilter = target.GetComponent<MeshFilter>();
		var meshRenderer = target.GetComponent<MeshRenderer>();
		ObjectSnapData snapData = new ObjectSnapData();

		snapData.meshFilter = meshFilter;
		snapData.meshRenderer = meshRenderer;
		snapData.startPosition = target.position;
		snapData.startCenter = meshRenderer.bounds.center;
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

	private void UpdateMovementBuffers(Transform target, Vector3 deltaMovement)
	{
		var snapData = m_SnapDataTable[target];

		CheckNullList(snapData.movementDirections);
		CheckNullList(snapData.movementTimestamps);

		snapData.movementDirections.Add(deltaMovement);
		snapData.movementTimestamps.Add(Time.realtimeSinceStartup);

		CheckListLength(snapData.movementDirections);
		CheckListLength(snapData.movementTimestamps);
	}

	private void CheckNullList<T>(List<T> listToCheck)
	{
		if (listToCheck == null)
			listToCheck = new List<T>();
	}

	private void CheckListLength<T>(List<T> listToCheck)
	{
		while (listToCheck.Count > kFramesToKeep)
			listToCheck.RemoveAt(0);
	}

	private void HandleGroundSnap(Transform target, MeshFilter meshFilter, Vector3 deltaMovement)
	{
		if (!U.Snapping.HasFlag(U.Snapping.SnappingModes.SnapToGround))
			return;

		var closestVertex = U.Snapping.GetClosestVertex(meshFilter, Vector3.zero, Vector3.up);
		U.Snapping.SnapToGroundPlane(target, deltaMovement, closestVertex);
	}

	private Vector3 GetWorldExtents(Mesh mesh, Transform target)
	{
		Vector3 extents = mesh.bounds.extents;

		for (int i = 0; i < 3; i++)
			extents[i] *= target.lossyScale[i];

		return extents;
	}

	private void HandleSurfaceSnap(Transform target, MeshFilter meshFilter, Transform[] raycastIgnore)
	{
		if (!U.Snapping.HasFlag(U.Snapping.SnappingModes.SnapToSurfaceNormal))
			return;

		var snapData = m_SnapDataTable[target];

		Bounds meshBounds = snapData.meshRenderer.bounds;
		Vector3 extents = GetWorldExtents(meshFilter.sharedMesh, target);

		Vector3 origin = snapData.startCenter;
		Vector3 currentOffset = meshBounds.center - origin;

		RaycastHit hit;
		Ray ray = new Ray(origin, currentOffset);

		if (U.Snapping.GetBoxSnapHit(
			target,
			ray,
			extents,
			currentOffset.magnitude,
			out hit,
			raycastIgnore))
		{
			if (snapData.collisionPoint.HasValue)
				target.position = snapData.collisionPoint.Value;
			else
			{
				ray.origin = snapData.startPosition;
				target.position = ray.GetPoint(hit.distance);
			}
		}
		else
		{
			snapData.startCenter = meshBounds.center;
			snapData.startPosition = target.position;
			snapData.collisionPoint = null;
		}
	}

	private void HandleThrowEnd(Transform target, MeshFilter meshFilter, Transform[] raycastIgnore)
	{
		if (!U.Snapping.HasFlag(U.Snapping.SnappingModes.Throw))
			return;
		
		if (!m_SnapDataTable.ContainsKey(target))
			return;

		var movementBuffer = m_SnapDataTable[target].movementDirections;
		var timestampBuffer = m_SnapDataTable[target].movementTimestamps;

		int count = movementBuffer.Count;
		if (count <= 1)
			return;

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
		SetupThrow(target, meshFilter, raycastIgnore, total / totalMagnitude, totalMagnitude, time);
	}

	private void SetupThrow(Transform target, MeshFilter meshFilter, Transform[] raycastIgnore, Vector3 throwDirection, float distance, float throwTime)
	{
		float velocity = distance / throwTime;
		if (velocity < 1)
			return;
		var snapData = m_SnapDataTable[target];

		snapData.throwDirection = throwDirection;
		snapData.startVelocity = velocity;
		snapData.currentVelocity = velocity;
		
		Bounds meshBounds = snapData.meshRenderer.bounds;
		Vector3 extents = GetWorldExtents(meshFilter.sharedMesh, target);

		Vector3 origin = meshBounds.center;
		RaycastHit hit;

		Ray ray = new Ray(origin, throwDirection);
		snapData.throwRay = ray;

		snapData.hasCollision = U.Snapping.GetBoxSnapHit(
			target,
			ray,
			extents,
			distance * 100f,
			out hit,
			raycastIgnore);

		snapData.targetPoint = hit;
		if (snapData.hasCollision)
		{
			snapData.startPosition = target.position;
			snapData.startDistance = hit.distance;
		}
		else
			snapData.closestVertex = U.Snapping.GetClosestVertex(meshFilter, Vector3.zero, Vector3.up);
	}

	private void UpdateThrow(Transform target)
	{
		if (!U.Snapping.HasFlag(U.Snapping.SnappingModes.Throw))
			return;

		if (!m_SnapDataTable.ContainsKey(target))
			return;

		var snapData = m_SnapDataTable[target];
		if (snapData.currentVelocity <= 0)
			return;

		float deltaTime = Time.unscaledDeltaTime;
		float deltaVelocity = snapData.currentVelocity * deltaTime;
		Vector3 deltaMovement = snapData.throwDirection * deltaVelocity;
		bool validMovement = true;
		for (int i = 0; i < 3; i++)
		{
			if (float.IsInfinity(deltaMovement[i]) || float.IsNaN(deltaMovement[i]))
			{
				validMovement = false;
				break;
			}
		}

		if (validMovement)
			target.position += deltaMovement;

		snapData.currentVelocity -= snapData.startVelocity * deltaTime;

		if (snapData.hasCollision)
		{
			Ray throwRay = snapData.throwRay;
			throwRay.origin = snapData.startPosition;

			Vector3 targetPoint = throwRay.GetPoint(snapData.startDistance);
			Vector3 targetPosition = target.position;

			bool isClose = Vector3.Distance(targetPosition, targetPoint) < deltaVelocity;
			bool overshot = Vector3.Distance(targetPosition, snapData.startPosition) > snapData.startDistance;

			if (isClose || overshot)
			{
				target.position = targetPoint;
				snapData.currentVelocity = -1;
			}
		}
		else
		{
			if (U.Snapping.SnapToGroundPlane(target, snapData.throwDirection, snapData.closestVertex))
				snapData.currentVelocity = -1;
		}

		m_SnapDataTable[target] = snapData;
	}

}
