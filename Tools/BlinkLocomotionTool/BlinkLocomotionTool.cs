using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.EditorVR;
using UnityEngine.Experimental.EditorVR.Proxies;
using UnityEngine.Experimental.EditorVR.Tools;
using UnityEngine.Experimental.EditorVR.Utilities;
using UnityEngine.InputNew;

public class BlinkLocomotionTool : MonoBehaviour, ITool, ILocomotor, ICustomRay, IUsesHandedRayOrigin, ICustomActionMap, ILinkedTool, IUsesProxyType
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

	bool m_EnableJoystick;
	bool m_AllowScaling = true;
	bool m_Scaling;
	float m_StartScale;
	float m_StartDistance;
	Vector3 m_StartPosition;
	Vector3 m_PlayerVector;

	InputControl m_Grip;
	InputControl m_Thumb;

	public Transform viewerPivot { private get; set; }

	public ActionMap actionMap { get { return m_BlinkActionMap; } }
	[SerializeField]
	private ActionMap m_BlinkActionMap;

	public List<ILinkedTool> otherTools { get { return m_OtherTools; } }
	readonly List<ILinkedTool> m_OtherTools = new List<ILinkedTool>();

	public DefaultRayVisibilityDelegate showDefaultRay { private get; set; }
	public DefaultRayVisibilityDelegate hideDefaultRay { private get; set; }
	public Func<Transform, object, bool> lockRay { private get; set; }
	public Func<Transform, object, bool> unlockRay { private get; set; }

	public Transform rayOrigin { private get; set; }
	public Node node { private get; set; }

	public bool primary { get; set; }

	public Type proxyType { private get; set; }

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

		var yawValue = blinkInput.yaw.value;
		var forwardValue = blinkInput.forward.value;

		m_Grip = blinkInput.grip.isHeld ? blinkInput.grip : null;
		m_Thumb = blinkInput.thumb.isHeld ? blinkInput.thumb : null;

		if (primary)
		{
			if (m_Grip != null)
			{
				if (m_AllowScaling)
				{
					foreach (var linkedTool in otherTools)
					{
						var blinkTool = (BlinkLocomotionTool)linkedTool;
						if (blinkTool.m_Grip != null)
						{
							consumeControl(m_Grip);
							consumeControl(blinkTool.m_Grip);

							var distance = Vector3.Distance(viewerPivot.InverseTransformPoint(rayOrigin.position),
								viewerPivot.InverseTransformPoint(blinkTool.rayOrigin.position));

							if (!m_Scaling)
							{
								m_StartScale = viewerPivot.localScale.x;
								m_StartDistance = distance;
								m_PlayerVector = viewerPivot.position - U.Camera.GetMainCamera().transform.position;
								m_PlayerVector.y = 0;
								m_StartPosition = viewerPivot.position - m_PlayerVector;

								m_EnableJoystick = false;
								blinkTool.m_EnableJoystick = false;
							}

							m_Scaling = true;

							var scaleFactor = m_StartDistance / distance;

							if (m_Thumb != null && blinkTool.m_Thumb != null)
							{
								m_AllowScaling = false;
								scaleFactor = 1 / m_StartScale;
								consumeControl(m_Thumb);
								consumeControl(blinkTool.m_Thumb);
							}

							viewerPivot.position = m_StartPosition + m_PlayerVector * scaleFactor;
							viewerPivot.localScale = Vector3.one * m_StartScale * scaleFactor;
							break;
						}
					}
				}
			}
			else
			{
				m_AllowScaling = true;
				m_Scaling = false;

				if (!m_EnableJoystick)
				{
					m_EnableJoystick = true;
					foreach (var linkedTool in otherTools)
					{
						((BlinkLocomotionTool)linkedTool).m_EnableJoystick = true;
					}
				}
			}
		}

		if (m_EnableJoystick && (proxyType != typeof(ViveProxy) || m_Thumb != null))
		{
			var viewerCamera = U.Camera.GetMainCamera();

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
					forwardValue = forwardValue * forwardValue * Mathf.Sign(forwardValue) * viewerPivot.localScale.x;

					var direction = Vector3.up;

					if (node == Node.LeftHand)
					{
						direction = viewerCamera.transform.forward;
						direction.y = 0;
						direction.Normalize();
					}

					viewerPivot.Translate(direction * forwardValue * kMoveSpeed * Time.unscaledDeltaTime, Space.World);
					consumeControl(blinkInput.forward);
				}
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
