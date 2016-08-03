using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.VR.Tools;
using UnityEditor;
using UnityEditor.VR;
using UnityEngine.VR.Utilities;

public class TransformTool : MonoBehaviour, ITool
{
	[SerializeField]
	private List<GameObject> m_ManipulatorPrefabs = new List<GameObject>();

	[SerializeField]
	private GameObject m_ManipulatorPrefab;

	private GameObject m_CurrentManipulator;
	private Vector3 m_TargetPosition;
	private Quaternion m_TargetRotation;

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
		if (Selection.activeGameObject == null)
		{
			m_CurrentManipulator.SetActive(false);
		}
		else
		{
			if (m_CurrentManipulator == null)
			{
				m_CurrentManipulator = U.Object.InstantiateAndSetActive(m_ManipulatorPrefab, transform);
				var manip = m_CurrentManipulator.GetComponent<IManipulator>();
				if (manip != null)
				{
					manip.translate = Translate;
					manip.rotate = Rotate;
				}
			}
			m_CurrentManipulator.SetActive(true);
			m_CurrentManipulator.transform.position = Selection.activeGameObject.transform.position;
			m_CurrentManipulator.transform.rotation = Quaternion.identity;
			m_TargetPosition = GetSelectionCenter();
			m_TargetRotation = Selection.activeGameObject.transform.rotation; //TODO change intial rotation if transforming local / world
		}
	}

	void Update()
	{
		if (Selection.activeGameObject != null && m_CurrentManipulator != null)
		{
			m_CurrentManipulator.transform.position = Vector3.Lerp(m_CurrentManipulator.transform.position,
				m_TargetPosition, 0.2f);
			Selection.activeGameObject.transform.position = m_CurrentManipulator.transform.position;

			Selection.activeGameObject.transform.rotation = Quaternion.Slerp(Selection.activeGameObject.transform.rotation,
				m_TargetRotation, 0.2f);

			UpdateManipulatorSize();
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

	private Vector3 GetSelectionCenter()
	{
		//TODO get center of all selected objects
		return Selection.activeGameObject.transform.position;
	}

	private void GetSelectionBounds()
	{
		//TODO calculate bounds of selection
	}
	private void UpdateManipulatorSize()
	{
		var distance = Vector3.Distance(VRView.viewerCamera.transform.position, m_CurrentManipulator.transform.position);
		//TODO resize manipulator based on distance
											
	}
}
