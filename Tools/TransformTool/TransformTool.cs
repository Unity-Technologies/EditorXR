using UnityEngine;
using System.Collections.Generic;
using UnityEngine.VR.Tools;
using UnityEditor;
using UnityEditor.VR;
using UnityEngine.VR.Utilities;
using UnityEngine.InputNew;

public class TransformTool : MonoBehaviour, ITool, ICustomActionMap
{
	[SerializeField]
	private GameObject m_StandardManipulatorPrefab;
	[SerializeField]
	private GameObject m_ScaleManipulatorPrefab;
	[SerializeField]
	private const float kBaseManipulatorSize = .3f;
	[SerializeField]
	private const float kLazyFollowTranslate = 8f;
	[SerializeField]
	private const float kLazyFollowRotate = 12f;
	[SerializeField]
	private ActionMap m_ActionMap;

	private readonly List<GameObject> m_AllManipulatorPrefabs = new List<GameObject>();
	private GameObject m_CurrentManipulatorGameObject;
	private int m_CurrentManipulatorIndex;
	private IManipulator m_Manipulator;

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

	public ActionMap actionMap
	{
		get { return m_ActionMap; }
	}

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
			m_AllManipulatorPrefabs.Add(m_StandardManipulatorPrefab);
		if(m_ScaleManipulatorPrefab != null)
			m_AllManipulatorPrefabs.Add(m_ScaleManipulatorPrefab);

		m_CurrentManipulatorIndex = 0;
	}

	void OnEnable()
	{
		Selection.selectionChanged += OnSelectionChanged;
	}

	void OnDisable()
	{
		Selection.selectionChanged -= OnSelectionChanged;
	}

	private void OnSelectionChanged()
	{
		m_SelectionTransforms = Selection.GetTransforms(SelectionMode.Editable);

		if (m_SelectionTransforms.Length == 0)
		{
			if (m_CurrentManipulatorGameObject != null)
				m_CurrentManipulatorGameObject.SetActive(false);
		}
		else
		{
			if (m_CurrentManipulatorGameObject == null)
				CreateManipulator(m_AllManipulatorPrefabs[m_CurrentManipulatorIndex]);

			MoveManipulatorToSelection();
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

			if (m_Manipulator != null && !m_Manipulator.dragging)
			{
				UpdateManipulatorSize();
				MoveManipulatorToSelection();
			}

			m_CurrentManipulatorGameObject.transform.position = Vector3.Lerp(m_CurrentManipulatorGameObject.transform.position,
				m_TargetPosition, kLazyFollowTranslate * Time.unscaledDeltaTime);

			if(m_PivotRotation == PivotRotation.Local) // Manipulator does not rotate when in global mode
				m_CurrentManipulatorGameObject.transform.rotation = Quaternion.Slerp(m_CurrentManipulatorGameObject.transform.rotation, m_TargetRotation, kLazyFollowRotate * Time.unscaledDeltaTime);

			foreach (var t in m_SelectionTransforms)
			{
				t.rotation = Quaternion.Slerp(t.rotation,
					m_TargetRotation * m_RotationOffsets[t], kLazyFollowRotate * Time.unscaledDeltaTime);

				if (m_PivotMode == PivotMode.Center) // Rotate the position offset from the manipulator when rotating around center
				{
					m_PositionOffsetRotation = Quaternion.Slerp(m_PositionOffsetRotation, m_TargetRotation * Quaternion.Inverse(m_StartRotation),
						kLazyFollowRotate * Time.unscaledDeltaTime);
					t.position = m_CurrentManipulatorGameObject.transform.position + m_PositionOffsetRotation * m_PositionOffsets[t];
				}
				else
					t.position = m_CurrentManipulatorGameObject.transform.position + m_PositionOffsets[t];
				
				t.localScale = Vector3.Lerp(t.localScale, Vector3.Scale(m_TargetScale, m_ScaleOffsets[t]),
					kLazyFollowTranslate * Time.unscaledDeltaTime);
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
		var newBounds = default(Bounds); // By default centered at (0,0,0)
		var boundsInitialized = false;
		foreach (var selectedObj in m_SelectionTransforms)
		{
			var renderers = selectedObj.GetComponentsInChildren<Renderer>();
			if (renderers.Length == 0)
				continue;
			foreach (var r in renderers)
			{
				if (Mathf.Approximately(r.bounds.extents.sqrMagnitude, 0f)) // Necessary because Particle Systems have renderer components with center and extents (0,0,0)
					continue;

				if (boundsInitialized) // Only use encapsulate after the first renderer, otherwise bounds will always encapsulate point (0,0,0)
					newBounds.Encapsulate(r.bounds);
				else
				{
					newBounds = r.bounds;
					boundsInitialized = true;
				}
			}
		}
		m_SelectionBounds = newBounds;
	}

	private void UpdateManipulatorSize()
	{
		var distance = Vector3.Distance(VRView.viewerCamera.transform.position, m_CurrentManipulatorGameObject.transform.position);
		m_CurrentManipulatorGameObject.transform.localScale = Vector3.one * distance * kBaseManipulatorSize;
	}

	private void CreateManipulator(GameObject prefab)
	{
		if (m_CurrentManipulatorGameObject != null)
			DestroyImmediate(m_CurrentManipulatorGameObject);

		m_CurrentManipulatorGameObject = U.Object.InstantiateAndSetActive(prefab, transform);
		m_Manipulator = m_CurrentManipulatorGameObject.GetComponent<IManipulator>();
		if (m_Manipulator != null)
		{
			m_Manipulator.translate = Translate;
			m_Manipulator.rotate = Rotate;
			m_Manipulator.scale = Scale;
		}
	}

	private void MoveManipulatorToSelection()
	{
		if (m_SelectionTransforms.Length <= 0)
			return;

		UpdateSelectionBounds();
		m_CurrentManipulatorGameObject.SetActive(true);
		m_CurrentManipulatorGameObject.transform.position = (m_PivotMode == PivotMode.Pivot) ? m_SelectionTransforms[0].position : m_SelectionBounds.center;
		m_CurrentManipulatorGameObject.transform.rotation = (m_PivotRotation == PivotRotation.Global) ? Quaternion.identity : m_SelectionTransforms[0].rotation;
		m_TargetPosition = m_CurrentManipulatorGameObject.transform.position;
		m_TargetRotation = m_CurrentManipulatorGameObject.transform.rotation;
		m_StartRotation = m_TargetRotation;
		m_PositionOffsetRotation = Quaternion.identity;
		m_TargetScale = Vector3.one;
		
		// Save the initial position, rotation, and scale realtive to the manipulator
		m_PositionOffsets.Clear();
		m_RotationOffsets.Clear();
		m_ScaleOffsets.Clear();
		foreach (var t in m_SelectionTransforms)
		{
			m_PositionOffsets.Add(t, t.position - m_CurrentManipulatorGameObject.transform.position);
			m_ScaleOffsets.Add(t, t.localScale);
			m_RotationOffsets.Add(t, Quaternion.Inverse(m_CurrentManipulatorGameObject.transform.rotation) * t.rotation);
		}
	}

	public PivotMode SwitchPivotMode()
	{
		m_PivotMode = m_PivotMode == PivotMode.Pivot ?  PivotMode.Center :  PivotMode.Pivot;
		MoveManipulatorToSelection();
		return m_PivotMode;
	}

	public PivotRotation SwitchPivotRotation()
	{
		m_PivotRotation = m_PivotRotation == PivotRotation.Global ? PivotRotation.Local : PivotRotation.Global;
		MoveManipulatorToSelection();
		return m_PivotRotation;
	}

	public void SwitchManipulator()
	{
		// Go to the next manipulator type in the list
		m_CurrentManipulatorIndex = (m_CurrentManipulatorIndex + 1) % m_AllManipulatorPrefabs.Count;
		CreateManipulator(m_AllManipulatorPrefabs[m_CurrentManipulatorIndex]);
		MoveManipulatorToSelection();
	}
}
