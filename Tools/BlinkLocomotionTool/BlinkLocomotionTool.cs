using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputNew;
using UnityEngine.UI;
using UnityEngine.VR.Helpers;
using UnityEngine.VR;
using UnityEngine.VR.Tools;
using UnityEngine.VR.Utilities;

public class BlinkLocomotionTool : MonoBehaviour, ITool, ILocomotor, ICustomRay, ICustomActionMap
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

	public Node selfNode { get; set; }

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
		else if (s_ActiveTool == this && m_BlinkLocomotionInput.blink.wasJustReleased)
		{
			var outOfRange = m_BlinkVisuals.HideVisuals();
			showDefaultRay();

			if (!outOfRange)
				StartCoroutine(MoveTowardTarget(m_BlinkVisuals.locatorPosition));
		}
	}

	private IEnumerator MoveTowardTarget(Vector3 targetPosition)
	{
		// Smooth motion will cause Workspaces to lag behind camera
		var components = viewerPivot.GetComponentsInChildren<SmoothMotion>();
		foreach (var smoothMotion in components)
		{
			smoothMotion.enabled = false;
		}

		m_State = State.Moving;
		targetPosition = new Vector3(targetPosition.x + (viewerPivot.position.x - U.Camera.GetMainCamera().transform.position.x), viewerPivot.position.y, targetPosition.z + (viewerPivot.position.z - U.Camera.GetMainCamera().transform.position.z));
		const float kTargetDuration = 1f;
		var currentPosition = viewerPivot.position;
		var velocity = new Vector3();
		var currentDuration = 0f;
		while (currentDuration < kTargetDuration)
		{
			currentDuration += Time.unscaledDeltaTime;
			currentPosition = U.Math.SmoothDamp(currentPosition, targetPosition, ref velocity, kTargetDuration, Mathf.Infinity, Time.unscaledDeltaTime);
			viewerPivot.position = currentPosition;
			yield return null;
		}

		foreach (var smoothMotion in components)
		{
			smoothMotion.enabled = true;
		}

		viewerPivot.position = targetPosition;
		m_State = State.Inactive;
		s_ActiveTool = null;
	}
}
