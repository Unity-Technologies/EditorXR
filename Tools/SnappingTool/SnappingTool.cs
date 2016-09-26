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

	public SnappingModes snappingMode { private get; set; }
	private SnappingModes m_SnappingMode;
	
	private Vector3 m_LastRayPosition;
	private float m_InitialDistance;
	private float m_LastDistance;
	
	private Coroutine m_ThrowCoroutine;

	private const int kFramesToKeep = 90;
	private List<Vector3> m_MovementBufferDirections = new List<Vector3>(kFramesToKeep);
	private List<float> m_MovementBufferTimestamps = new List<float>(kFramesToKeep);

	void Update()
	{
		if (rayOrigin == null)
			return;

		CheckForCanvas();

		GameObject active = UnityEditor.Selection.activeGameObject;
		if (active == null)
		{
			if (m_ActionMapInput.trigger.wasJustPressed)
			{
				active = getFirstGameObject(rayOrigin);
				UnityEditor.Selection.activeGameObject = active;
			}
		}

		if (active)
		{
			if (m_ActionMapInput.trigger.wasJustPressed)
				HandleTriggerPress(active);
			else if (m_ActionMapInput.trigger.isHeld)
				HandleTriggerHeld(active);
			else if (m_ActionMapInput.trigger.wasJustReleased)
				HandleTriggerRelease(active);
		}
	}

	void CheckForCanvas()
	{
		if (m_ToolCanvas == null)
		{
			var go = instantiateUI(ToolCanvasPrefab.gameObject);
			m_ToolCanvas = go.GetComponent<Canvas>();
			m_ToolCanvas.GetComponent<SnappingToolUI>().snappingTool = this;
			m_ToolCanvas.transform.SetParent(rayOrigin, false);
		}
	}

	void HandleTriggerPress(GameObject active)
	{
		m_LastRayPosition = active.transform.position;
		m_InitialDistance = Vector3.Distance(rayOrigin.position, m_LastRayPosition);
		m_LastDistance = m_InitialDistance;

		if (m_ThrowCoroutine != null)
		{
			StopCoroutine(m_ThrowCoroutine);
			m_ThrowCoroutine = null;
		}
	}

	void HandleTriggerHeld(GameObject active)
	{
		while (m_MovementBufferDirections.Count > kFramesToKeep)
			m_MovementBufferDirections.RemoveAt(0);
		while (m_MovementBufferTimestamps.Count > kFramesToKeep)
			m_MovementBufferTimestamps.RemoveAt(0);

		var activeTransform = active.transform;

		Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
		switch (snappingMode)
		{
			case SnappingModes.None:
			case SnappingModes.SnapToGround:
			case SnappingModes.Throw:
				Vector3 rayPoint = ray.GetPoint(m_InitialDistance);
				Vector3 deltaMovement = rayPoint - m_LastRayPosition;

				activeTransform.position = rayPoint;
				m_MovementBufferDirections.Add(deltaMovement);
				m_MovementBufferTimestamps.Add(Time.realtimeSinceStartup);

				if (snappingMode == SnappingModes.SnapToGround)
				{
					Vector3 smoothDelta = Vector3.zero;
					int bufferCount = m_MovementBufferDirections.Count;

					for (int i = bufferCount - 1; i >= 0; i--)
					{
						smoothDelta += m_MovementBufferDirections[i];
					}
					smoothDelta /= bufferCount;

					U.Snapping.SnapToGroundPlane(activeTransform, smoothDelta);
				}

				m_LastRayPosition = rayPoint;
				break;
			case SnappingModes.SnapToSurface:
			case SnappingModes.SnapToSurfaceNormal:
				if (U.Snapping.SnapToSurface(activeTransform, ray, alignRotation: snappingMode == SnappingModes.SnapToSurfaceNormal))
					m_LastDistance = Vector3.Distance(ray.origin, activeTransform.position);
				else
					activeTransform.position = ray.GetPoint(m_LastDistance);
				break;
		}
	}

	void HandleTriggerRelease(GameObject active)
	{
		if (snappingMode == SnappingModes.Throw)
		{
			int count = m_MovementBufferDirections.Count;

			Vector3 last = m_MovementBufferDirections[count - 1];
			Vector3 total = last;
			
			float firstTime = m_MovementBufferTimestamps[count - 1];
			float time = 0;

			for (int i = count - 1; i > 0; i--)
			{
				if (Vector3.Angle(last, m_MovementBufferDirections[i]) < 30)
				{
					total += m_MovementBufferDirections[i];
					time = firstTime - m_MovementBufferTimestamps[i];
				}
				else
					break;
			}

			float totalMagnitude = total.magnitude;
			if (totalMagnitude > 1)
				m_ThrowCoroutine = StartCoroutine(ThrowObject(active, total / totalMagnitude, totalMagnitude, time));
		}
	}

	IEnumerator ThrowObject(GameObject objectToThrow, Vector3 throwDirection, float distance, float throwTime)
	{
		var target = objectToThrow.transform;
		float prevTime = Time.realtimeSinceStartup;
		
		float velocity = distance / throwTime;
		float startVelocity = velocity;

		while (velocity > 0)
		{
			float deltaTime = Time.realtimeSinceStartup - prevTime;
			float deltaVelocity = velocity * deltaTime;

			if (!float.IsNaN(deltaVelocity))
				target.position += throwDirection * deltaVelocity;

			velocity -= startVelocity * deltaTime;
			prevTime = Time.realtimeSinceStartup;

			Ray ray = new Ray(target.position, throwDirection);
			RaycastHit hit;
			if (U.Snapping.GetRaySnapHit(ray, deltaVelocity, out hit, target))
			{
				target.position = hit.point;
				yield break;
			}

			yield return null;
		}
	}

	void OnDestroy()
	{
		if (m_ToolCanvas)
			U.Object.Destroy(m_ToolCanvas.gameObject);
	}

}
