using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputNew;
using UnityEngine.Experimental.EditorVR.Helpers;
using UnityEngine.Experimental.EditorVR.Tools;
using UnityEngine.Experimental.EditorVR.Utilities;

public class BlinkLocomotionTool : MonoBehaviour, ITool, ILocomotor, ICustomRay, IUsesRayOrigin, ICustomActionMap
{
	const float kRotationSpeed = 300f;
	const float kMoveSpeed = 5f;

	private enum State
	{
		Inactive = 0,
		Moving = 3
	}

	[SerializeField]
	private GameObject m_BlinkVisualsPrefab;

	// It doesn't make sense to be able to activate another blink tool when you already have one active, since you can't
	// blink to two locations at the same time;
	static BlinkLocomotionTool s_ActiveBlinkTool;

	private GameObject m_BlinkVisualsGO;
	private BlinkVisuals m_BlinkVisuals;

	private State m_State = State.Inactive;

	public Transform viewerPivot { private get; set; }

	public ActionMap actionMap { get { return m_BlinkActionMap; } }
	[SerializeField]
	private ActionMap m_BlinkActionMap;

	public DefaultRayVisibilityDelegate showDefaultRay { get; set; }
	public DefaultRayVisibilityDelegate hideDefaultRay { get; set; }
	public Func<Transform, object, bool> lockRay { get; set; }
	public Func<Transform, object, bool> unlockRay { get; set; }

	public Transform rayOrigin { private get; set; }

	private void Start()
	{
		m_BlinkVisualsGO = U.Object.Instantiate(m_BlinkVisualsPrefab, rayOrigin);
		m_BlinkVisuals = m_BlinkVisualsGO.GetComponentInChildren<BlinkVisuals>();
		m_BlinkVisuals.enabled = false;
		m_BlinkVisuals.showValidTargetIndicator = false; // We don't define valid targets, so always show green
		m_BlinkVisualsGO.transform.parent = rayOrigin;
		m_BlinkVisualsGO.transform.localPosition = Vector3.zero;
		m_BlinkVisualsGO.transform.localRotation = Quaternion.identity;
	}

	private void OnDisable()
	{
		m_State = State.Inactive;
		if (s_ActiveBlinkTool == this)
			s_ActiveBlinkTool = null;
	}

	private void OnDestroy()
	{
		showDefaultRay(rayOrigin);
	}

	public void ProcessInput(ActionMapInput input, Action<InputControl> consumeControl)
	{
		var blinkInput = (BlinkLocomotion)input;
		if (m_State == State.Moving || (s_ActiveBlinkTool != null && s_ActiveBlinkTool != this))
			return;

		var viewerCamera = U.Camera.GetMainCamera();

		var yawValue = blinkInput.yaw.value;
		var forwardValue = blinkInput.forward.value;

		if (Mathf.Abs(yawValue) > Mathf.Abs(forwardValue))
		{
			if (!Mathf.Approximately(yawValue, 0))
			{
				yawValue = yawValue * yawValue * Mathf.Sign(yawValue);

				viewerPivot.RotateAround(viewerCamera.transform.position, Vector3.up, yawValue * kRotationSpeed * Time.unscaledDeltaTime);
				consumeControl(blinkInput.yaw);
			}
		}
		else
		{
			if (!Mathf.Approximately(forwardValue, 0))
			{
				var forward = viewerCamera.transform.forward;
				forward.y = 0;
				forward.Normalize();
				forwardValue = forwardValue * forwardValue * Mathf.Sign(forwardValue);

				viewerPivot.Translate(forward * forwardValue * kMoveSpeed * Time.unscaledDeltaTime, Space.World);
				consumeControl(blinkInput.forward);
			}
		}

		if (blinkInput.blink.wasJustPressed && !m_BlinkVisuals.outOfMaxRange)
		{
			s_ActiveBlinkTool = this;
			hideDefaultRay(rayOrigin);
			lockRay(rayOrigin, this);

			m_BlinkVisuals.ShowVisuals();

			consumeControl(blinkInput.blink);
		}
		else if (s_ActiveBlinkTool == this && blinkInput.blink.wasJustReleased)
		{
			unlockRay(rayOrigin, this);
			showDefaultRay(rayOrigin);

			if (!m_BlinkVisuals.outOfMaxRange)
			{
				m_BlinkVisuals.HideVisuals();
				StartCoroutine(MoveTowardTarget(m_BlinkVisuals.locatorPosition));
			}
			else
			{
				m_BlinkVisuals.enabled = false;
			}

			consumeControl(blinkInput.blink);
		}
	}

	private IEnumerator MoveTowardTarget(Vector3 targetPosition)
	{
		m_State = State.Moving;
		targetPosition = new Vector3(targetPosition.x + (viewerPivot.position.x - U.Camera.GetMainCamera().transform.position.x), viewerPivot.position.y, targetPosition.z + (viewerPivot.position.z - U.Camera.GetMainCamera().transform.position.z));
		const float kTargetDuration = 0.05f;
		var currentPosition = viewerPivot.position;
		var currentDuration = 0f;
		while (currentDuration < kTargetDuration)
		{
			currentDuration += Time.unscaledDeltaTime;
			currentPosition = Vector3.Lerp(currentPosition, targetPosition, currentDuration / kTargetDuration);
			viewerPivot.position = currentPosition;
			yield return null;
		}

		viewerPivot.position = targetPosition;
		m_State = State.Inactive;
		s_ActiveBlinkTool = null;
	}
}
