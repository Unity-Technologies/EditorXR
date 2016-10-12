using UnityEngine;
using System.Collections.Generic;
using UnityEngine.VR.Tools;
using UnityEditor;
using UnityEngine.VR.Utilities;
using UnityEngine.InputNew;
using System;
using UnityEngine.VR;

public class TransformTool : MonoBehaviour, ITool, ICustomActionMaps, ITransformTool, ISelectionChanged, IDirectSelection
{
	[SerializeField]
	GameObject m_StandardManipulatorPrefab;

	[SerializeField]
	GameObject m_ScaleManipulatorPrefab;

	[SerializeField]
	ActionMap m_TransformActionMap;

	[SerializeField]
	ActionMap m_DirectSelectActionMap;

	const float kBaseManipulatorSize = 0.3f;
	const float kLazyFollowTranslate = 8f;
	const float kLazyFollowRotate = 12f;

	readonly List<GameObject> m_AllManipulators = new List<GameObject>();
	GameObject m_CurrentManipulator;
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

	Transform m_GrabbedObjectLeft;
	Transform m_RayOriginLeft;
	Transform m_GrabbedObjectRight;
	Transform m_RayOriginRight;
	Vector3 m_PositionOffsetLeft;
	Vector3 m_PositionOffsetRight;
	Quaternion m_RotationOffsetLeft;
	Quaternion m_RotationOffsetRight;
	bool m_DirectSelected;

	TransformInput m_TransformInput;
	DirectSelectInput m_DirectSelectInput;

	public ActionMap[] actionMaps { get { return new ActionMap[] { m_TransformActionMap, m_DirectSelectActionMap }; } }

	public ActionMapInput[] actionMapInputs
	{
		get
		{
			return m_ActionMapInputs;
		}
		set
		{
			m_ActionMapInputs = value;
			foreach (var input in m_ActionMapInputs)
			{
				var transformInput = input as TransformInput;
				if (transformInput != null)
					m_TransformInput = transformInput;

				var directInput = input as DirectSelectInput;
				if (directInput != null)
					m_DirectSelectInput = directInput;
			}
		}
	}
	ActionMapInput[] m_ActionMapInputs;

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
		m_DirectSelected = false;

		if (m_SelectionTransforms.Length == 0)
		{
			m_CurrentManipulator.SetActive(false);
		}
		else
		{
			UpdateCurrentManipulator();
		}
	}

	void Update()
	{
		var directSelection = getDirectSelection();
		var isHovering = directSelection.Count > 0 || m_GrabbedObjectLeft || m_GrabbedObjectRight;
		m_DirectSelectInput.active = isHovering;
		if(m_CurrentManipulator.activeSelf && isHovering)
			m_CurrentManipulator.SetActive(false);

		foreach (var selection in directSelection)
		{
			if (selection.Value.node == Node.LeftHand && m_DirectSelectInput.selectLeft.wasJustPressed)
			{
				EditorVR.s_Dragging = true;
				m_DirectSelected = true;
				var grabbedObject = selection.Value.gameObject;
				m_GrabbedObjectLeft = grabbedObject.transform;
				Selection.activeGameObject = grabbedObject;
				m_RayOriginLeft = selection.Key;
				var inverseRotation = Quaternion.Inverse(m_RayOriginLeft.rotation);
				m_PositionOffsetLeft = inverseRotation * (m_GrabbedObjectLeft.transform.position - m_RayOriginLeft.position);
				m_RotationOffsetLeft = inverseRotation * m_GrabbedObjectLeft.transform.rotation;
			}
			if (selection.Value.node == Node.RightHand && m_DirectSelectInput.selectRight.wasJustPressed)
			{
				EditorVR.s_Dragging = true;
				m_DirectSelected = true;
				var grabbedObject = selection.Value.gameObject;
				m_GrabbedObjectRight = grabbedObject.transform;
				Selection.activeGameObject = grabbedObject;
				m_RayOriginRight = selection.Key;
				var inverseRotation = Quaternion.Inverse(m_RayOriginRight.rotation);
				m_PositionOffsetRight = inverseRotation * (m_GrabbedObjectRight.transform.position - m_RayOriginRight.position);
				m_RotationOffsetRight = inverseRotation * m_GrabbedObjectRight.transform.rotation;
			}
		}

		if (m_GrabbedObjectLeft && m_DirectSelectInput.selectLeft.isHeld)
		{
			m_GrabbedObjectLeft.position = m_RayOriginLeft.position + m_RayOriginLeft.rotation * m_PositionOffsetLeft;
			m_GrabbedObjectLeft.rotation = m_RayOriginLeft.rotation * m_RotationOffsetLeft;
		}

		if (m_GrabbedObjectRight && m_DirectSelectInput.selectRight.isHeld)
		{
			m_GrabbedObjectRight.position = m_RayOriginRight.position + m_RayOriginRight.rotation * m_PositionOffsetRight;
			m_GrabbedObjectRight.rotation = m_RayOriginRight.rotation * m_RotationOffsetRight;
		}

		if (m_DirectSelectInput.selectLeft.wasJustReleased)
			m_GrabbedObjectLeft = null;

		if (m_DirectSelectInput.selectRight.wasJustReleased)
			m_GrabbedObjectRight = null;

		if (isHovering || m_DirectSelected)
			return;

		EditorVR.s_Dragging = false;

		if (m_SelectionTransforms != null && m_SelectionTransforms.Length > 0)
		{
			if (m_TransformInput.pivotMode.wasJustPressed) // Switching center vs pivot
				SwitchPivotMode();

			if (m_TransformInput.pivotRotation.wasJustPressed) // Switching global vs local
				SwitchPivotRotation();

			if (m_TransformInput.manipulatorType.wasJustPressed)
				SwitchManipulator();

			var manipulator = m_CurrentManipulator.GetComponent<IManipulator>();
			if (manipulator != null && !manipulator.dragging)
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
		var distance = Vector3.Distance(camera.transform.position, m_CurrentManipulator.transform.position);
		m_CurrentManipulator.transform.localScale = Vector3.one * distance * kBaseManipulatorSize;
	}

	private GameObject CreateManipulator(GameObject prefab)
	{
		var go = U.Object.Instantiate(prefab, transform, active: false);
		var manipulator = go.GetComponent<IManipulator>();
		manipulator.translate = Translate;
		manipulator.rotate = Rotate;
		manipulator.scale = Scale;
		return go;
	}

	private void UpdateCurrentManipulator()
	{
		if (m_SelectionTransforms.Length <= 0)
			return;

		UpdateSelectionBounds();
		m_CurrentManipulator.SetActive(true);
		var manipulatorTransform = m_CurrentManipulator.transform;
		manipulatorTransform.position = (m_PivotMode == PivotMode.Pivot) ? m_SelectionTransforms[0].position : m_SelectionBounds.center;
		manipulatorTransform.rotation = (m_PivotRotation == PivotRotation.Global) ? Quaternion.identity : m_SelectionTransforms[0].rotation;
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
			manipulator.SetActive(false);

		// Go to the next manipulator type in the list
		m_CurrentManipulatorIndex = (m_CurrentManipulatorIndex + 1) % m_AllManipulators.Count;
		m_CurrentManipulator = m_AllManipulators[m_CurrentManipulatorIndex];
		UpdateCurrentManipulator();
	}
}