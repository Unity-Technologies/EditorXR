using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputNew;
using UnityEngine.VR.Tools;
using UnityEngine.VR.Utilities;
using SnappingModes = UnityEngine.VR.Utilities.U.Snapping.SnappingModes;

[MainMenuItem("Snapping", "Transform", "Select snapping modes")]
public class SnappingTool : MonoBehaviour, ITool, IRay, IRaycaster, ICustomActionMap, IInstantiateUI
{

	[SerializeField]
	private Canvas ToolCanvasPrefab;
	private Canvas m_ToolCanvas;

	public ActionMap actionMap
	{
		get { return m_ActionMap; }
	}
	[SerializeField]
	private ActionMap m_ActionMap;

	public ActionMapInput actionMapInput
	{
		get { return m_ActionMapInput; }
		set { m_ActionMapInput = (SnappingInput)value; }
	}
	private SnappingInput m_ActionMapInput;

	public Transform rayOrigin { private get; set; }

	public Func<GameObject, GameObject> instantiateUI { private get; set; }

	public Func<Transform, GameObject> getFirstGameObject { private get; set; }

	public SnappingModes snappingMode { get; set; }

	private MeshFilter m_SelectedMeshFilter;

	private GameObject m_LastSelectedObject;
	private Vector3 m_LowestPointOffset;

	private Vector3 m_LastRayPosition;
	private float m_InitialDistance;
	private float m_LastDistance;

	private const int kFramesToKeep = 90;
	private List<Vector3> m_MovementBufferDirections = new List<Vector3>(kFramesToKeep);
	private List<float> m_MovementBufferTimestamps = new List<float>(kFramesToKeep);

	private const float kNormalSnapAngle = 60f;
	
	private Transform m_ThrowTarget;
	private Vector3 m_ThrowDirection;
	private float m_ThrowStartVelocity;
	private float m_ThrowVelocity;
	private RaycastHit m_ThrowTargetPoint;
	private Vector3 m_ClosestPoint;
	private bool m_ThrowHasCollision;
	private float m_ThrowStartDistance;

	private void Update()
	{
		if (rayOrigin == null)
			return;

		CheckForCanvas();

		GameObject active = GetActiveObject();
		if (active)
		{
			CheckSelectedObject(active);
			
			if (m_ActionMapInput.trigger.wasJustPressed)
				HandleTriggerPress(active);
			else if (m_ActionMapInput.trigger.isHeld)
				HandleTriggerHeld(active);
			else if (m_ActionMapInput.trigger.wasJustReleased)
				HandleTriggerRelease(active);
		}

		UpdateThrow();
	}

	private GameObject GetActiveObject()
	{
		GameObject active = UnityEditor.Selection.activeGameObject;

		if (active == null)
		{
			if (m_ActionMapInput.trigger.wasJustPressed)
			{
				active = getFirstGameObject(rayOrigin);
				UnityEditor.Selection.activeGameObject = active;
			}
		}

		return active;
	}

	private void CheckSelectedObject(GameObject active)
	{
		if (active != m_LastSelectedObject)
		{
			m_LastSelectedObject = active;
			m_SelectedMeshFilter = m_LastSelectedObject.GetComponent<MeshFilter>();

			if (m_SelectedMeshFilter != null)
			{
				var mesh = m_SelectedMeshFilter.sharedMesh;
				if (mesh != null)
				{
					m_LowestPointOffset = Vector3.down * mesh.bounds.min.y;
					return;
				}
			}

			m_LowestPointOffset = Vector3.zero;
		}
	}

	private void CheckForCanvas()
	{
		if (m_ToolCanvas == null)
		{
			var canvasObj = instantiateUI(ToolCanvasPrefab.gameObject);
			m_ToolCanvas = canvasObj.GetComponent<Canvas>();
			m_ToolCanvas.GetComponent<SnappingToolUI>().snappingTool = this;
			m_ToolCanvas.transform.SetParent(rayOrigin, false);
		}
	}

	private void HandleTriggerPress(GameObject active)
	{
		m_LastRayPosition = active.transform.position;
		m_InitialDistance = Vector3.Distance(rayOrigin.position, m_LastRayPosition);
		m_LastDistance = m_InitialDistance;

		m_ThrowVelocity = 0;
	}

	private void HandleTriggerHeld(GameObject active)
	{
		while (m_MovementBufferDirections.Count > kFramesToKeep)
			m_MovementBufferDirections.RemoveAt(0);
		while (m_MovementBufferTimestamps.Count > kFramesToKeep)
			m_MovementBufferTimestamps.RemoveAt(0);

		var activeTransform = active.transform;

		Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);

		if (snappingMode == SnappingModes.None || HasFlag(SnappingModes.SnapToGround) || HasFlag(SnappingModes.Throw))
			HandleFreeSnapping(activeTransform, ray);
		if (HasFlag(SnappingModes.SnapToSurfaceNormal))
			HandleSurfaceSnapping(activeTransform, ray);
	}

	private void HandleFreeSnapping(Transform activeTransform, Ray ray)
	{
		Vector3 rayPoint = ray.GetPoint(m_InitialDistance);
		Vector3 deltaMovement = rayPoint - m_LastRayPosition;

		activeTransform.position = rayPoint;
		m_MovementBufferDirections.Add(deltaMovement);
		m_MovementBufferTimestamps.Add(Time.realtimeSinceStartup);

		if (HasFlag(SnappingModes.SnapToGround))
		{
			Vector3 smoothDelta = Vector3.zero;
			int bufferCount = m_MovementBufferDirections.Count;

			for (int i = bufferCount - 1; i >= 0; i--)
				smoothDelta += m_MovementBufferDirections[i];
			smoothDelta /= bufferCount;

			if (U.Snapping.SnapToGroundPlane(activeTransform, smoothDelta))
			{
				if (activeTransform.rotation == Quaternion.identity)
					activeTransform.position += m_LowestPointOffset;
				else
				{
					var lowest = GetClosestVertex(activeTransform, Vector3.zero, Vector3.up, true);
					activeTransform.position -= lowest;
				}
			}
		}

		m_LastRayPosition = rayPoint;
	}

	private void HandleSurfaceSnapping(Transform activeTransform, Ray ray)
	{
		RaycastHit hit;

		if (U.Snapping.SnapToSurface(activeTransform, ray, out hit))
		{
			m_LastDistance = Vector3.Distance(ray.origin, activeTransform.position);
			bool normalSnap = Vector3.Angle(hit.normal, activeTransform.up) < kNormalSnapAngle;

			if (normalSnap)
			{
				Quaternion rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
				activeTransform.rotation = rotation;
				activeTransform.position += hit.normal * m_LowestPointOffset.y;
			}
			else
			{
				Vector3 closestVertex = GetClosestVertex(activeTransform, hit.point, hit.normal);
				activeTransform.position -= closestVertex;
			}
		}
		else if (snappingMode == SnappingModes.SnapToSurfaceNormal)
			activeTransform.position = ray.GetPoint(m_LastDistance);
	}

	private void HandleTriggerRelease(GameObject active)
	{
		if (HasFlag(SnappingModes.Throw))
		{
			int count = m_MovementBufferDirections.Count;

			if (count > 1)
			{
				Vector3 lastDirection = m_MovementBufferDirections[count - 1];
				Vector3 total = lastDirection;

				float firstTime = m_MovementBufferTimestamps[count - 1];
				float time = 0;

				for (int i = count - 2; i > 0; i--)
				{
					if (Vector3.Angle(lastDirection, m_MovementBufferDirections[i]) < 30)
					{
						total += m_MovementBufferDirections[i];
						time = firstTime - m_MovementBufferTimestamps[i];
					}
					else
						break;
				}

				float totalMagnitude = total.magnitude;
				if (totalMagnitude > 1 && !Mathf.Approximately(time, 0))
					SetupThrow(active, total / totalMagnitude, totalMagnitude, time);
			}
		}
	}

	private void SetupThrow(GameObject objectToThrow, Vector3 throwDirection, float distance, float throwTime)
	{
		m_ThrowTarget = objectToThrow.transform;
		m_ThrowStartVelocity = distance / throwTime;
		m_ThrowVelocity = m_ThrowStartVelocity;
		m_ThrowDirection = throwDirection;

		Ray ray = new Ray(m_ThrowTarget.position, throwDirection);
		m_ThrowHasCollision = U.Snapping.GetRaySnapHit(ray, distance * 100f, out m_ThrowTargetPoint, m_ThrowTarget);
		if (m_ThrowHasCollision)
		{
			m_ClosestPoint = GetClosestVertex(m_ThrowTarget, m_ThrowTargetPoint.point, m_ThrowTargetPoint.normal, true);
			m_ThrowStartDistance = Vector3.Distance(m_ThrowTarget.position, m_ThrowTargetPoint.point);
		}
	}

	private void UpdateThrow()
	{
		if (m_ThrowVelocity > 0)
		{
			float deltaTime = Time.unscaledDeltaTime;
			float deltaVelocity = m_ThrowVelocity * deltaTime;

			if (!float.IsNaN(deltaVelocity))
				m_ThrowTarget.position += m_ThrowDirection * deltaVelocity;

			m_ThrowVelocity -= m_ThrowStartVelocity * deltaTime;

			if (m_ThrowHasCollision)
			{
				float currentDistance = Vector3.Distance(m_ThrowTarget.position, m_ThrowTargetPoint.point);

				bool isClose = currentDistance < deltaVelocity;
				bool overshot = currentDistance > m_ThrowStartDistance;

				if (isClose || overshot)
				{
					Vector3 targetPosition = m_ThrowTargetPoint.point + m_ClosestPoint;
					m_ThrowTarget.position = targetPosition;
					m_ThrowVelocity = -1;
				}
			}
		}
	}

	private Vector3 GetClosestVertex(Transform active, Vector3 surfacePoint, Vector3 surfaceNormal, bool singleAxisAwayFromSurface = false)
	{
		Vector3 closest = Vector3.zero;

		if (m_SelectedMeshFilter)
		{
			var mesh = m_SelectedMeshFilter.sharedMesh;
			if (mesh)
			{
				var vertexCount = mesh.vertexCount;
				var vertices = mesh.vertices;

				float lowestDistance = float.PositiveInfinity;

				Quaternion rotation = Quaternion.FromToRotation(Vector3.up, surfaceNormal);
				Matrix4x4 surfaceToWorld = Matrix4x4.TRS(surfacePoint, rotation, Vector3.one);
				Matrix4x4 worldToSurface = surfaceToWorld.inverse;
				Matrix4x4 objectToWorld = active.localToWorldMatrix;

				for (int i = 0; i < vertexCount; i++)
				{
					var vertex = vertices[i];
					var transformVector = objectToWorld.MultiplyVector(vertex);
					var surfaceVertex = worldToSurface.MultiplyVector(transformVector);

					if (surfaceVertex.y < lowestDistance)
					{
						if (singleAxisAwayFromSurface)
						{
							Vector3 onlyY = new Vector3(0, surfaceVertex.y, 0);
							closest = surfaceToWorld.inverse.MultiplyVector(onlyY);
						}
						else
							closest = transformVector;

						lowestDistance = surfaceVertex.y;
					}
				}
			}
		}

		return closest;
	}

	private bool HasFlag(SnappingModes flag)
	{
		return (snappingMode & flag) != 0;
	}

	private void OnDestroy()
	{
		if (m_ToolCanvas)
			U.Object.Destroy(m_ToolCanvas.gameObject);
	}

}
