using UnityEngine;
using System.Collections.Generic;
using UnityEngine.VR.Tools;
using UnityEditor;
using UnityEngine.VR.Utilities;
using UnityEngine.InputNew;

public class TransformTool : MonoBehaviour, ITool, ICustomActionMap, ITransformTool, ISelectionChanged
{
	[SerializeField]
	private GameObject m_DirectManipulatorPrefab;
	[SerializeField]
	private GameObject m_StandardManipulatorPrefab;
	[SerializeField]
	private GameObject m_ScaleManipulatorPrefab;

	public ActionMap actionMap
	{
		get { return m_ActionMap; }
	}
	[SerializeField]
	private ActionMap m_ActionMap;

	private const float kBaseManipulatorSize = 0.3f;
	private const float kLazyFollowTranslate = 8f;
	private const float kLazyFollowRotate = 12f;

	private readonly List<GameObject> m_AllManipulators = new List<GameObject>();
	private GameObject m_DirectManipulator;
	private GameObject m_CurrentManipulator;
	private int m_CurrentManipulatorIndex;

	private Transform[] m_SelectionTransforms;
	private Bounds m_SelectionBounds;
	private Vector3 m_TargetPosition;
	private Quaternion m_TargetRotation;
	private Vector3 m_TargetScale;
	private Quaternion m_PositionOffsetRotation;
	private Quaternion m_StartRotation;

	private readonly Dictionary<Transform, Vector3> m_PositionOffsets = new Dictionary<Transform, Vector3>();
	private readonly Dictionary<Transform, Quaternion> m_RotationOffsets = new Dictionary<Transform, Quaternion>();
	private readonly Dictionary<Transform, Vector3> m_ScaleOffsets = new Dictionary<Transform, Vector3>();

	private PivotRotation m_PivotRotation = PivotRotation.Local;
	private PivotMode m_PivotMode = PivotMode.Pivot;

	public TransformMode mode { private get; set; }

	public ActionMapInput actionMapInput
	{
		get
		{
			return (ActionMapInput)m_TransformInput;
		}

		set
		{
			m_TransformInput = (TransformInput)value;
		}
	}
	private TransformInput m_TransformInput;

	void Awake()
	{
		// Add standard and scale manipulator prefabs to a list (because you cannot add asset references directly to a serialized list)
		if(m_StandardManipulatorPrefab != null)
			m_AllManipulators.Add(CreateManipulator(m_StandardManipulatorPrefab));

		if (m_ScaleManipulatorPrefab != null)
			m_AllManipulators.Add(CreateManipulator(m_ScaleManipulatorPrefab));

		if (m_DirectManipulatorPrefab != null)
			m_DirectManipulator = CreateManipulator(m_DirectManipulatorPrefab);

		m_CurrentManipulatorIndex = 0;
		m_CurrentManipulator = m_AllManipulators[m_CurrentManipulatorIndex];
	}

	public void OnSelectionChanged()
	{
		m_SelectionTransforms = Selection.GetTransforms(SelectionMode.Editable);

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

			if(m_PivotRotation == PivotRotation.Local) // Manipulator does not rotate when in global mode
				manipulatorTransform.rotation = Quaternion.Slerp(manipulatorTransform.rotation, m_TargetRotation, kLazyFollowRotate * deltaTime);

			foreach (var t in m_SelectionTransforms)
			{
				t.rotation = Quaternion.Slerp(t.rotation, m_TargetRotation * m_RotationOffsets[t], kLazyFollowRotate * deltaTime);

				if (m_PivotMode == PivotMode.Center) // Rotate the position offset from the manipulator when rotating around center
				{
					m_PositionOffsetRotation = Quaternion.Slerp(m_PositionOffsetRotation, m_TargetRotation * Quaternion.Inverse(m_StartRotation),
																kLazyFollowRotate * deltaTime);
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
		m_TargetRotation *= delta;
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

		// If we haven't encountered any Renderers, return bounds of (1,1,1) at the center of the selection
		// for the purposes of positioning and  scaling the DirectManipulator
		if (newBounds == null)
		{
			var bounds = new Bounds();
			bounds.size = Vector3.one;
			foreach (var selectedObj in m_SelectionTransforms)
				bounds.center += selectedObj.transform.position / m_SelectionTransforms.Length;
			newBounds = bounds;
		}

		m_SelectionBounds = newBounds.Value;
	}

	private void UpdateManipulatorSize()
	{
		switch (mode)
		{
			case TransformMode.Direct:
				m_CurrentManipulator.transform.localScale = Vector3.one * m_SelectionBounds.size.MaxComponent();
				break;
			default:
				var camera = U.Camera.GetMainCamera();
				var distance = Vector3.Distance(camera.transform.position, m_CurrentManipulator.transform.position);
				m_CurrentManipulator.transform.localScale = Vector3.one * distance * kBaseManipulatorSize;
				break;
		}
	}

	private GameObject CreateManipulator(GameObject prefab)
	{
		var go = U.Object.Instantiate(prefab, transform, active:false);
		var manipulator = go.GetComponent<IManipulator>();
		manipulator.translate = Translate;
		manipulator.rotate = Rotate;
		manipulator.scale = Scale;
		return go;
	}

	private void UpdateCurrentManipulator()
	{
		switch (mode)
		{
			case TransformMode.Direct:
				foreach (var manipulator in m_AllManipulators)
					manipulator.SetActive(false);
				m_CurrentManipulator = m_DirectManipulator;
				break;
			default:
				m_DirectManipulator.SetActive(false);
				m_CurrentManipulator = m_AllManipulators[m_CurrentManipulatorIndex];
				break;
		}
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
		if (mode == TransformMode.Direct)
			return;

		m_PivotMode = m_PivotMode == PivotMode.Pivot ?  PivotMode.Center :  PivotMode.Pivot;
		UpdateCurrentManipulator();
	}

	private void SwitchPivotRotation()
	{
		if (mode == TransformMode.Direct)
			return;

		m_PivotRotation = m_PivotRotation == PivotRotation.Global ? PivotRotation.Local : PivotRotation.Global;
		UpdateCurrentManipulator();
	}

	private void SwitchManipulator()
	{
		if (mode == TransformMode.Direct)
			return;

		foreach (var manipulator in m_AllManipulators)
			manipulator.SetActive(false);

		// Go to the next manipulator type in the list
		m_CurrentManipulatorIndex = (m_CurrentManipulatorIndex + 1) % m_AllManipulators.Count;
		m_CurrentManipulator = m_AllManipulators[m_CurrentManipulatorIndex];
		UpdateCurrentManipulator();
	}
}