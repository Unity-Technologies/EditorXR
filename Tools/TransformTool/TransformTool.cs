using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputNew;
using UnityEngine.VR;
using UnityEngine.VR.Helpers;
using UnityEngine.VR.Modules;
using UnityEngine.VR.Tools;
using UnityEngine.VR.Utilities;

public class TransformTool : MonoBehaviour, ITool, ICustomActionMap, ITransformTool, ISelectionChanged, IDirectSelection
{
	const float kBaseManipulatorSize = 0.3f;
	const float kLazyFollowTranslate = 8f;
	const float kLazyFollowRotate = 12f;
	const float kViewerPivotTransitionTime = 0.75f;

	class GrabData
	{
		public Transform grabbedObject;
		public Transform rayOrigin;
		public Vector3 positionOffset;
		public Quaternion rotationOffset;
		public Vector3 initialScale;
		public DirectSelectInput input;

		public GrabData(Transform rayOrigin, Transform grabbedObject, DirectSelectInput input)
		{
			this.rayOrigin = rayOrigin;
			this.grabbedObject = grabbedObject;
			this.input = input;
			Reset();
		}

		public void Reset()
		{
			U.Math.GetTransformOffset(rayOrigin, grabbedObject, out positionOffset, out rotationOffset);
			initialScale = grabbedObject.transform.localScale;
		}

		public void PositionObject()
		{
			U.Math.SetTransformOffset(rayOrigin, grabbedObject, positionOffset, rotationOffset);
		}

		public void ScaleObject(float scaleFactor)
		{
			grabbedObject.position = rayOrigin.position + positionOffset * scaleFactor;
			grabbedObject.localScale = initialScale * scaleFactor;
		}
	}

	[SerializeField]
	GameObject m_StandardManipulatorPrefab;

	[SerializeField]
	GameObject m_ScaleManipulatorPrefab;

	[SerializeField]
	ActionMap m_TransformActionMap;

	readonly List<IManipulator> m_AllManipulators = new List<IManipulator>();
	IManipulator m_CurrentManipulator;
	int m_CurrentManipulatorIndex;

	Transform[] m_SelectionTransforms;
	Bounds m_SelectionBounds;
	Vector3 m_TargetPosition;
	Quaternion m_TargetRotation;
	Vector3 m_TargetScale;
	Quaternion m_PositionOffsetRotation;
	Quaternion m_StartRotation;

	readonly Dictionary<Transform, Vector3> m_PositionOffsets = new Dictionary<Transform, Vector3>();
	readonly Dictionary<Transform, Quaternion> m_RotationOffsets = new Dictionary<Transform, Quaternion>();
	readonly Dictionary<Transform, Vector3> m_ScaleOffsets = new Dictionary<Transform, Vector3>();

	PivotRotation m_PivotRotation = PivotRotation.Local;
	PivotMode m_PivotMode = PivotMode.Pivot;

	public bool directManipulationEnabled { get { return m_DirectManipulationEnabled; } set { m_DirectManipulationEnabled = value; } }
	bool m_DirectManipulationEnabled = true;

	readonly Dictionary<Node, GrabData> m_GrabData = new Dictionary<Node, GrabData>();
	bool m_DirectSelected;
	float m_ScaleStartDistance;
	Node m_ScaleFirstNode;
	float m_ScaleFactor;
	bool m_WasScaling;

	TransformInput m_TransformInput;

	public ActionMap actionMap { get { return m_TransformActionMap; } }

	public ActionMapInput actionMapInput { get { return m_TransformInput; } set { m_TransformInput = (TransformInput)value; } }

	public Func<Dictionary<Transform, DirectSelection>> getDirectSelection { private get; set; }

	void Awake()
	{
		// Add standard and scale manipulator prefabs to a list (because you cannot add asset references directly to a serialized list)
		if (m_StandardManipulatorPrefab != null)
			m_AllManipulators.Add(CreateManipulator(m_StandardManipulatorPrefab));

		if (m_ScaleManipulatorPrefab != null)
			m_AllManipulators.Add(CreateManipulator(m_ScaleManipulatorPrefab));

		m_CurrentManipulatorIndex = 0;
		m_CurrentManipulator = m_AllManipulators[m_CurrentManipulatorIndex];
	}

	public void OnSelectionChanged()
	{
		m_SelectionTransforms = Selection.GetTransforms(SelectionMode.Editable);

		// Reset direct selection state in case of a ray selection
		m_DirectSelected = false;

		if (m_SelectionTransforms.Length == 0)
			m_CurrentManipulator.active = false;
		else
			UpdateCurrentManipulator();
	}

	void Update()
	{
		var directSelection = getDirectSelection();
		var hasLeft = m_GrabData.ContainsKey(Node.LeftHand);
		var hasRight = m_GrabData.ContainsKey(Node.RightHand);
		var hasObject = directSelection.Count > 0 || hasLeft || hasRight;

		if (m_DirectManipulationEnabled && !m_CurrentManipulator.dragging)
		{
			// Disable manipulator on direct hover or drag
			if (m_CurrentManipulator.active && hasObject)
				m_CurrentManipulator.active = false;

			foreach (var kvp in directSelection)
			{
				var selection = kvp.Value;

				// Only MiniWorldRays can grab the player head
				if (selection.gameObject.tag == EditorVR.kVRPlayerTag && !selection.isMiniWorldRay)
					continue;

				var directSelectInput = (DirectSelectInput)selection.input;
				if (directSelectInput.select.wasJustPressed)
				{
					// Detach the player head model so that 
					if (selection.gameObject.tag == EditorVR.kVRPlayerTag)
						selection.gameObject.transform.parent = null;

					var grabbedObject = selection.gameObject.transform;
					var rayOrigin = kvp.Key;

					// Check if the other hand is already grabbing for two-handed scale
					foreach (var grabData in m_GrabData)
					{
						var otherNode = grabData.Key;
						if (otherNode != selection.node)
						{
							m_ScaleStartDistance = (rayOrigin.position - grabData.Value.rayOrigin.position).magnitude;
							m_ScaleFirstNode = otherNode;
							grabData.Value.positionOffset = grabbedObject.position - grabData.Value.rayOrigin.position;
							break;
						}
					}

					m_GrabData[selection.node] = new GrabData(rayOrigin, grabbedObject, directSelectInput);

					Selection.activeGameObject = grabbedObject.gameObject;

					// Wait a frame since OnSelectionChanged is called after setting m_DirectSelected to true
					EditorApplication.delayCall += () =>
					{
						// A direct selection has been made. Hide the manipulator until the selection changes
						m_DirectSelected = true;
					};
				}
			}

			GrabData leftData;
			hasLeft = m_GrabData.TryGetValue(Node.LeftHand, out leftData);

			GrabData rightData;
			hasRight = m_GrabData.TryGetValue(Node.RightHand, out rightData);

			var leftHeld = leftData != null && leftData.input.select.isHeld;
			var rightHeld = rightData != null && rightData.input.select.isHeld;
			if (hasLeft && hasRight && leftHeld && rightHeld && leftData.grabbedObject == rightData.grabbedObject) // Two-handed scaling
			{
				// Offsets will change while scaling. Whichever hand keeps holding the trigger after scaling is done will need to reset itself
				m_WasScaling = true;

				m_ScaleFactor = (leftData.rayOrigin.position - rightData.rayOrigin.position).magnitude / m_ScaleStartDistance;
				if (m_ScaleFactor > 0 && m_ScaleFactor < Mathf.Infinity)
				{
					if (m_ScaleFirstNode == Node.LeftHand)
						leftData.ScaleObject(m_ScaleFactor);
					else
						rightData.ScaleObject(m_ScaleFactor);
				}
			}
			else
			{
				if (m_WasScaling)
				{
					// Reset initial conditions
					if (hasLeft)
						leftData.Reset();
					if (hasRight)
						rightData.Reset();

					m_WasScaling = false;
				}

				if (hasLeft && leftHeld)
					leftData.PositionObject();

				if (hasRight && rightHeld)
					rightData.PositionObject();
			}

			if (hasLeft && leftData.input.select.wasJustReleased)
				DropObject(Node.LeftHand);

			if (hasRight && rightData.input.select.wasJustReleased)
				DropObject(Node.RightHand);
		}

		// Manipulator is disabled while direct manipulation is happening
		if (hasObject || m_DirectSelected)
			return;

		if (m_SelectionTransforms != null && m_SelectionTransforms.Length > 0)
		{
			if (m_TransformInput.pivotMode.wasJustPressed) // Switching center vs pivot
				SwitchPivotMode();

			if (m_TransformInput.pivotRotation.wasJustPressed) // Switching global vs local
				SwitchPivotRotation();

			if (m_TransformInput.manipulatorType.wasJustPressed)
				SwitchManipulator();

			if (!m_CurrentManipulator.dragging)
			{
				UpdateManipulatorSize();
				UpdateCurrentManipulator();
			}

			var deltaTime = Time.unscaledDeltaTime;
			var manipulatorTransform = m_CurrentManipulator.transform;
			manipulatorTransform.position = Vector3.Lerp(manipulatorTransform.position, m_TargetPosition, kLazyFollowTranslate * deltaTime);

			if (m_PivotRotation == PivotRotation.Local) // Manipulator does not rotate when in global mode
				manipulatorTransform.rotation = Quaternion.Slerp(manipulatorTransform.rotation, m_TargetRotation, kLazyFollowRotate * deltaTime);

			foreach (var t in m_SelectionTransforms)
			{
				t.rotation = Quaternion.Slerp(t.rotation, m_TargetRotation * m_RotationOffsets[t], kLazyFollowRotate * deltaTime);

				if (m_PivotMode == PivotMode.Center) // Rotate the position offset from the manipulator when rotating around center
				{
					m_PositionOffsetRotation = Quaternion.Slerp(m_PositionOffsetRotation, m_TargetRotation * Quaternion.Inverse(m_StartRotation), kLazyFollowRotate * deltaTime);
					t.position = manipulatorTransform.position + m_PositionOffsetRotation * m_PositionOffsets[t];
				}
				else
					t.position = manipulatorTransform.position + m_PositionOffsets[t];

				t.localScale = Vector3.Lerp(t.localScale, Vector3.Scale(m_TargetScale, m_ScaleOffsets[t]), kLazyFollowTranslate * deltaTime);
			}
		}
	}

	IEnumerator UpdateViewerPivot(Transform playerHead)
	{
		var viewerPivot = U.Camera.GetViewerPivot();

		// Smooth motion will cause Workspaces to lag behind camera
		var components = viewerPivot.GetComponentsInChildren<SmoothMotion>();
		foreach (var smoothMotion in components)
		{
			smoothMotion.enabled = false;
		}

		// Hide player head to avoid jarring impact
		var playerHeadRenderers = playerHead.GetComponentsInChildren<Renderer>();
		foreach (var renderer in playerHeadRenderers)
		{
			renderer.enabled = false;
		}

		var mainCamera = U.Camera.GetMainCamera().transform;
		var startPosition = viewerPivot.position;
		var startRotation = viewerPivot.rotation;

		var rotationDiff = U.Math.ConstrainYawRotation(Quaternion.Inverse(mainCamera.rotation) * playerHead.rotation);
		var cameraDiff = viewerPivot.position - mainCamera.position;
		cameraDiff.y = 0;
		var rotationOffset = rotationDiff * cameraDiff - cameraDiff;

		var endPosition = viewerPivot.position + (playerHead.position - mainCamera.position) + rotationOffset;
		var endRotation = viewerPivot.rotation * rotationDiff;
		var startTime = Time.realtimeSinceStartup;
		var diffTime = 0f;

		while (diffTime < kViewerPivotTransitionTime)
		{
			diffTime = Time.realtimeSinceStartup - startTime;
			var t = diffTime / kViewerPivotTransitionTime;
			// Use a Lerp instead of SmoothDamp for constant velocity (avoid motion sickness)
			viewerPivot.position = Vector3.Lerp(startPosition, endPosition, t);
			viewerPivot.rotation = Quaternion.Lerp(startRotation, endRotation, t);
			yield return null;
		}

		viewerPivot.position = endPosition;
		viewerPivot.rotation = endRotation;

		playerHead.parent = mainCamera;
		playerHead.localRotation = Quaternion.identity;
		playerHead.localPosition = Vector3.zero;

		foreach (var smoothMotion in components)
		{
			smoothMotion.enabled = true;
		}

		foreach (var renderer in playerHeadRenderers)
		{
			renderer.enabled = true;
		}
	}

	public void DropHeldObject(Transform obj)
	{
		Vector3 position;
		Quaternion rotation;
		DropHeldObject(obj, out position, out rotation);
	}

	public void DropHeldObject(Transform obj, out Vector3 positionOffset, out Quaternion rotationOffset)
	{
		foreach (var kvp in m_GrabData)
		{
			var grabData = kvp.Value;
			if (grabData.grabbedObject == obj)
			{
				positionOffset = grabData.positionOffset;
				rotationOffset = grabData.rotationOffset;
				DropObject(kvp.Key);
				return;
			}
		}

		positionOffset = Vector3.zero;
		rotationOffset = Quaternion.identity;
	}

	public Transform GetHeldObject(Transform rayOrigin)
	{
		foreach (var grabData in m_GrabData.Values)
		{
			if (grabData.rayOrigin == rayOrigin)
				return grabData.grabbedObject;
		}

		return null;
	}

	public void TransferHeldObject(Transform rayOrigin, ActionMapInput input, Transform destRayOrigin, Vector3 deltaOffset)
	{
		foreach (var grabData in m_GrabData.Values)
		{
			if (grabData.rayOrigin == rayOrigin)
			{
				grabData.rayOrigin = destRayOrigin;
				grabData.positionOffset += deltaOffset;
				grabData.input = (DirectSelectInput)input;
				grabData.PositionObject();
				return;
			}
		}
	}

	public void AddHeldObject(Node node, Transform rayOrigin, Transform grabbedObject, ActionMapInput input)
	{
		m_GrabData[node] = new GrabData(rayOrigin, grabbedObject, (DirectSelectInput)input);
	}

	void DropObject(Node inputNode)
	{
		// Droppin the player head updates the viewer pivot
		var grabbedObject = m_GrabData[inputNode].grabbedObject;
		if (grabbedObject.tag == EditorVR.kVRPlayerTag)
			StartCoroutine(UpdateViewerPivot(grabbedObject));

		m_GrabData.Remove(inputNode);
	}


	private void Translate(Vector3 delta)
	{
		m_TargetPosition += delta;
	}

	private void Rotate(Quaternion delta)
	{
		m_TargetRotation = delta * m_TargetRotation;
	}

	private void Scale(Vector3 delta)
	{
		m_TargetScale += delta;
	}

	private void UpdateSelectionBounds()
	{
		Bounds? newBounds = null;
		foreach (var selectedObj in m_SelectionTransforms)
		{
			var renderers = selectedObj.GetComponentsInChildren<Renderer>();
			foreach (var r in renderers)
			{
				if (Mathf.Approximately(r.bounds.extents.sqrMagnitude, 0f)) // Necessary because Particle Systems have renderer components with center and extents (0,0,0)
					continue;

				if (newBounds.HasValue)
					// Only use encapsulate after the first renderer, otherwise bounds will always encapsulate point (0,0,0)
					newBounds.Value.Encapsulate(r.bounds);
				else
					newBounds = r.bounds;
			}
		}

		// If we haven't encountered any Renderers, return bounds of (0,0,0) at the center of the selected objects
		if (newBounds == null)
		{
			var bounds = new Bounds();
			foreach (var selectedObj in m_SelectionTransforms)
				bounds.center += selectedObj.transform.position / m_SelectionTransforms.Length;
			newBounds = bounds;
		}

		m_SelectionBounds = newBounds.Value;
	}

	private void UpdateManipulatorSize()
	{
		var camera = U.Camera.GetMainCamera();
		var manipulatorTransform = m_CurrentManipulator.transform;
		var distance = Vector3.Distance(camera.transform.position, manipulatorTransform.position);
		manipulatorTransform.localScale = Vector3.one * distance * kBaseManipulatorSize;
	}

	private IManipulator CreateManipulator(GameObject prefab)
	{
		var go = U.Object.Instantiate(prefab, transform, active: false);
		var manipulator = go.GetComponent<IManipulator>();
		manipulator.translate = Translate;
		manipulator.rotate = Rotate;
		manipulator.scale = Scale;
		return manipulator;
	}

	private void UpdateCurrentManipulator()
	{
		if (m_SelectionTransforms.Length <= 0)
			return;

		UpdateSelectionBounds();
		m_CurrentManipulator.active = true;
		var manipulatorTransform = m_CurrentManipulator.transform;
		manipulatorTransform.position = m_PivotMode == PivotMode.Pivot ? m_SelectionTransforms[0].position : m_SelectionBounds.center;
		manipulatorTransform.rotation = m_PivotRotation == PivotRotation.Global ? Quaternion.identity : m_SelectionTransforms[0].rotation;
		m_TargetPosition = manipulatorTransform.position;
		m_TargetRotation = manipulatorTransform.rotation;
		m_StartRotation = m_TargetRotation;
		m_PositionOffsetRotation = Quaternion.identity;
		m_TargetScale = Vector3.one;

		// Save the initial position, rotation, and scale realtive to the manipulator
		m_PositionOffsets.Clear();
		m_RotationOffsets.Clear();
		m_ScaleOffsets.Clear();
		foreach (var t in m_SelectionTransforms)
		{
			m_PositionOffsets.Add(t, t.position - manipulatorTransform.position);
			m_ScaleOffsets.Add(t, t.localScale);
			m_RotationOffsets.Add(t, Quaternion.Inverse(manipulatorTransform.rotation) * t.rotation);
		}
	}

	private void SwitchPivotMode()
	{
		m_PivotMode = m_PivotMode == PivotMode.Pivot ? PivotMode.Center : PivotMode.Pivot;
		UpdateCurrentManipulator();
	}

	private void SwitchPivotRotation()
	{
		m_PivotRotation = m_PivotRotation == PivotRotation.Global ? PivotRotation.Local : PivotRotation.Global;
		UpdateCurrentManipulator();
	}

	private void SwitchManipulator()
	{
		foreach (var manipulator in m_AllManipulators)
			manipulator.active = false;

		// Go to the next manipulator type in the list
		m_CurrentManipulatorIndex = (m_CurrentManipulatorIndex + 1) % m_AllManipulators.Count;
		m_CurrentManipulator = m_AllManipulators[m_CurrentManipulatorIndex];
		UpdateCurrentManipulator();
	}
}