using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine.VR.Tools;
using UnityEditor;
using UnityEditor.VR;
using UnityEngine.VR.Utilities;
using System;
using UnityEngine.InputNew;

public class TransformTool : MonoBehaviour, ITool, ICustomActionMap
{
	[SerializeField]
	private GameObject m_StandardManipulatorPrefab;

	[SerializeField]
	private GameObject m_ScaleManipulatorPrefab;

	[SerializeField]
	private GameObject m_DirectManipulatorPrefab;

	[SerializeField]
	private const float kBaseManipulatorSize = .3f;

	[SerializeField]
	private const float kLazyFollowSpeed = 8f;


	private List<GameObject> m_AllManipulatorPrefabs = new List<GameObject>();
	private GameObject m_CurrentManipulatorGameObject;
	private int m_CurrentManipulatorIndex;
	private IManipulator m_Manipulator;
	private Vector3 m_TargetPosition;
	private Quaternion m_TargetRotation;
	private Vector3 m_TargetScale;

	private Bounds? m_SelectionBounds;
	private Transform[] m_SelectionTransforms;
	private Dictionary<Transform, Vector3> m_PositionOffsets = new Dictionary<Transform, Vector3>();
	private Dictionary<Transform, Vector3> m_ScaleOffsets = new Dictionary<Transform, Vector3>();
	private Dictionary<Transform, Quaternion> m_RotationOffsets = new Dictionary<Transform, Quaternion>();


	private PivotRotation m_PivotRotation = PivotRotation.Global;

	public ActionMap actionMap
	{
		get { return m_ActionMap; }
	}
	[SerializeField]
	private ActionMap m_ActionMap;

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
		m_AllManipulatorPrefabs.Add(m_StandardManipulatorPrefab);
		m_AllManipulatorPrefabs.Add(m_ScaleManipulatorPrefab);
		m_AllManipulatorPrefabs.Add(m_DirectManipulatorPrefab);
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
			//TODO change intial rotation if transforming local / world
		}

	}

	void Update()
	{

		if (m_TransformInput.pivotMode.wasJustPressed)
			m_PivotRotation = m_PivotRotation == PivotRotation.Global ? PivotRotation.Local : PivotRotation.Global;

		if (m_TransformInput.manipulatorType.wasJustPressed)
		{
			m_CurrentManipulatorIndex = (m_CurrentManipulatorIndex + 1) % m_AllManipulatorPrefabs.Count;
			CreateManipulator(m_AllManipulatorPrefabs[m_CurrentManipulatorIndex]);
			MoveManipulatorToSelection();
		}
		if (m_Manipulator != null && !m_Manipulator.dragging)
			UpdateManipulatorSize();

		if (m_SelectionTransforms != null && m_SelectionTransforms.Length > 0)
		{
			m_CurrentManipulatorGameObject.transform.position = Vector3.Lerp(m_CurrentManipulatorGameObject.transform.position,
				m_TargetPosition, kLazyFollowSpeed * Time.unscaledDeltaTime);

			foreach (var t in m_SelectionTransforms)
			{
				t.position = m_CurrentManipulatorGameObject.transform.position + m_PositionOffsets[t];
				t.rotation = Quaternion.Slerp(t.rotation,
					m_TargetRotation * m_RotationOffsets[t], kLazyFollowSpeed * Time.unscaledDeltaTime);

				t.localScale = Vector3.Lerp(t.localScale, new Vector3 (m_TargetScale.x * m_ScaleOffsets[t].x, m_TargetScale.y * m_ScaleOffsets[t].y , m_TargetScale.z * m_ScaleOffsets[t].z), kLazyFollowSpeed * Time.unscaledDeltaTime);
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
		Bounds newBounds = default(Bounds);
		bool boundsInitialized = false;
		foreach (var selectedObj in m_SelectionTransforms)
		{
			var renderers = selectedObj.GetComponentsInChildren<Renderer>();
			if (renderers.Length == 0)
				continue;
			foreach (var r in renderers)
			{
				if (Mathf.Approximately(r.bounds.extents.sqrMagnitude, 0f)) // Particle systems have renderer components where extents and center are (0,0,0)
					continue;
				if (!boundsInitialized)
				{
					newBounds = r.bounds;
					boundsInitialized = true;
				}
				else
					newBounds.Encapsulate(r.bounds);
			}
		}
		m_SelectionBounds = newBounds;
	}

	private void UpdateManipulatorSize()
	{
		if (m_SelectionBounds == null)
			return;
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
		UpdateSelectionBounds();
		m_CurrentManipulatorGameObject.SetActive(true);
		m_CurrentManipulatorGameObject.transform.position = m_SelectionBounds.Value.center;
		m_CurrentManipulatorGameObject.transform.rotation = Quaternion.identity;
		m_TargetPosition = m_CurrentManipulatorGameObject.transform.position;
		m_TargetRotation = m_CurrentManipulatorGameObject.transform.rotation = Quaternion.identity;
		m_TargetScale = Vector3.one;
		
		// Save the initial rotation and position relative to selection center
		m_PositionOffsets.Clear();
		m_RotationOffsets.Clear();
		m_ScaleOffsets.Clear();
		foreach (var t in m_SelectionTransforms)
		{
			m_PositionOffsets.Add(t, t.position - m_SelectionBounds.Value.center);
			m_ScaleOffsets.Add(t, t.localScale);
			m_RotationOffsets.Add(t, t.rotation);
		}

	}
}
