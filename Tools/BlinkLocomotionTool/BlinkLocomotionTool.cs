using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputNew;
using UnityEngine.UI;
using UnityEngine.VR.Tools;
using UnityEngine.VR.Utilities;

public class BlinkLocomotionTool : MonoBehaviour, ITool, ILocomotion, ICustomRay, ICustomActionMap
{
	private enum State
	{
		Inactive = 0,
		Moving = 3
	}

	[SerializeField]
	private GameObject m_BlinkVisualsPrefab;

	// It doesn't make sense to be able to activate another blink tool when you already have one active, since you can't
	// blink to two locations at the same time;
	private static BlinkLocomotionTool s_ActiveTool;

	private GameObject m_BlinkVisualsGO;
	private BlinkVisuals m_BlinkVisuals;

	private float m_MovementSpeed = 8f;
	private State m_State = State.Inactive;

	public Transform viewerPivot { private get; set; }

	public Action showDefaultRay { get; set; }
	public Action hideDefaultRay { get; set; }
	public Transform rayOrigin { private get; set; }

	public ActionMap actionMap { get { return m_BlinkActionMap; } }
	[SerializeField]
	private ActionMap m_BlinkActionMap;

	public ActionMapInput actionMapInput
	{
		get { return m_BlinkLocomotionInput; }
		set { m_BlinkLocomotionInput = (BlinkLocomotion)value; }
	}
	private BlinkLocomotion m_BlinkLocomotionInput;

	private void Start()
	{
		m_BlinkVisualsGO = U.Object.Instantiate(m_BlinkVisualsPrefab, rayOrigin);
		m_BlinkVisuals = m_BlinkVisualsGO.GetComponentInChildren<BlinkVisuals>();
		m_BlinkVisualsGO.transform.parent = rayOrigin;
		m_BlinkVisualsGO.transform.localPosition = Vector3.zero;
		m_BlinkVisualsGO.transform.localRotation = Quaternion.identity;
	}

	private void OnDisable()
	{
		m_State = State.Inactive;
		if (s_ActiveTool == this)
			s_ActiveTool = null;
	}

	private void OnDestroy()
	{
		showDefaultRay();
	}

	private void Update()
	{
		if (m_State == State.Moving || (s_ActiveTool != null && s_ActiveTool != this))
			return;

		if (m_BlinkLocomotionInput.blink.wasJustPressed)
		{
			s_ActiveTool = this;
			hideDefaultRay();
			m_BlinkVisuals.ShowVisuals();
		}
		else if (m_BlinkLocomotionInput.blink.wasJustReleased)
		{
			m_BlinkVisuals.HideVisuals();
			showDefaultRay();

			StartCoroutine(MoveTowardTarget(m_BlinkVisuals.locatorPosition));
		}
	}

	private IEnumerator MoveTowardTarget(Vector3 targetPosition)
	{
		m_State = State.Moving;

		targetPosition = new Vector3(targetPosition.x, viewerPivot.position.y, targetPosition.z);
		while ((viewerPivot.position - targetPosition).magnitude > 0.1f)
		{
			viewerPivot.position = Vector3.Lerp(viewerPivot.position, targetPosition, Time.unscaledDeltaTime * m_MovementSpeed);
			yield return null;
		}

		m_State = State.Inactive;
		s_ActiveTool = null;
	}
}