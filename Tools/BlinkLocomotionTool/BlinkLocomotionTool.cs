using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.EditorVR;
using UnityEngine.Experimental.EditorVR.Proxies;
using UnityEngine.Experimental.EditorVR.Tools;
using UnityEngine.Experimental.EditorVR.Utilities;
using UnityEngine.InputNew;

public class BlinkLocomotionTool : MonoBehaviour, ITool, ILocomotor, ICustomRay, IUsesHandedRayOrigin,
	ICustomActionMap, ILinkedTool, IUsesProxyType, IConnectInterfaces
{
	const float kFastRotationSpeed = 300f;
	const float kRotationThreshold = 0.9f;
	const float kSlowRotationSpeed = 15f;
	const float kFastMoveSpeed = 10f;
	const float kMoveThreshold = 0.9f;
	const float kSlowMoveSpeed = 3f;

	const float kMoveThresholdVive = 0.8f;
	const float kRotationThresholdVive = 0.8f;

	const float kMinScale = 0.01f;
	const float kMaxScale = 1000f;

	private enum State
	{
		Inactive = 0,
		Moving = 3
	}

	[SerializeField]
	private GameObject m_BlinkVisualsPrefab;

	[SerializeField]
	GameObject m_WorldScaleVisualsPrefab;

	WorldScaleVisuals m_WorldScaleVisuals;

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
	Vector3 m_StartMidPoint;
	Vector3 m_StartDirection;
	float m_StartYaw;

	InputControl m_Grip;
	InputControl m_Thumb;

	public ActionMap actionMap { get { return m_BlinkActionMap; } }
	[SerializeField]
	private ActionMap m_BlinkActionMap;

	public List<ILinkedTool> otherTools { get { return m_OtherTools; } }
	readonly List<ILinkedTool> m_OtherTools = new List<ILinkedTool>();

	Camera m_MainCamera;
	float m_OriginalNearClipPlane;
	float m_OriginalFarClipPlane;

	public DefaultRayVisibilityDelegate showDefaultRay { private get; set; }
	public DefaultRayVisibilityDelegate hideDefaultRay { private get; set; }
	public Func<Transform, object, bool> lockRay { private get; set; }
	public Func<Transform, object, bool> unlockRay { private get; set; }

	public Transform rayOrigin { private get; set; }
	public Node node { private get; set; }

	public bool primary { get; set; }

	public Type proxyType { private get; set; }

	public ConnectInterfacesDelegate connectInterfaces { get; set; }

	public Transform cameraRig { private get; set; }

	private void Start()
	{
		m_BlinkVisualsGO = U.Object.Instantiate(m_BlinkVisualsPrefab, rayOrigin);
		m_BlinkVisuals = m_BlinkVisualsGO.GetComponentInChildren<BlinkVisuals>();
		connectInterfaces(m_BlinkVisuals);
		m_BlinkVisuals.enabled = false;
		m_BlinkVisuals.showValidTargetIndicator = false; // We don't define valid targets, so always show green
		m_BlinkVisualsGO.transform.parent = rayOrigin;
		m_BlinkVisualsGO.transform.localPosition = Vector3.zero;
		m_BlinkVisualsGO.transform.localRotation = Quaternion.identity;

		m_MainCamera = U.Camera.GetMainCamera();
		m_OriginalNearClipPlane = m_MainCamera.nearClipPlane;
		m_OriginalFarClipPlane = m_MainCamera.farClipPlane;
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

	public void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl)
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
					var otherGrip = false;
					foreach (var linkedTool in otherTools)
					{
						var blinkTool = (BlinkLocomotionTool)linkedTool;
						if (blinkTool.m_Grip != null)
						{
							otherGrip = true;
							consumeControl(m_Grip);
							consumeControl(blinkTool.m_Grip);

							var thisPosition = cameraRig.InverseTransformPoint(rayOrigin.position);
							var otherRayOrigin = blinkTool.rayOrigin;
							var otherPosition = cameraRig.InverseTransformPoint(otherRayOrigin.position);
							var distance = Vector3.Distance(thisPosition, otherPosition);

							var rayToRay = otherPosition - thisPosition;
							var midPoint = thisPosition + rayToRay * 0.5f;

							rayToRay.y = 0; // Use for yaw rotation

							var pivotYaw = U.Math.ConstrainYawRotation(cameraRig.rotation);

							if (!m_Scaling)
							{
								m_StartScale = cameraRig.localScale.x;
								m_StartDistance = distance;
								m_StartMidPoint = pivotYaw * midPoint * m_StartScale;
								m_StartPosition = cameraRig.position;
								m_StartDirection = rayToRay;
								m_StartYaw = cameraRig.rotation.eulerAngles.y;

								m_EnableJoystick = false;
								blinkTool.m_EnableJoystick = false;

								CreateWorldScaleVisuals(rayOrigin, otherRayOrigin);
							}

							m_Scaling = true;

							var currentScale = m_StartScale * (m_StartDistance / distance);

							// Press both thumb buttons to reset
							if (m_Thumb != null && blinkTool.m_Thumb != null)
							{
								m_AllowScaling = false;

								rayToRay = otherRayOrigin.position - rayOrigin.position;
								midPoint = rayOrigin.position + rayToRay * 0.5f;
								var currOffset = midPoint - cameraRig.position;
								cameraRig.localScale = Vector3.one;
								cameraRig.position = midPoint - currOffset / currentScale;
								cameraRig.rotation = Quaternion.AngleAxis(m_StartYaw, Vector3.up);

								m_MainCamera.nearClipPlane = m_OriginalNearClipPlane;
								m_MainCamera.farClipPlane = m_OriginalFarClipPlane;

								consumeControl(m_Thumb);
								consumeControl(blinkTool.m_Thumb);

								if (m_WorldScaleVisuals)
									U.Object.Destroy(m_WorldScaleVisuals.gameObject);
							}

							if (currentScale < kMinScale)
								currentScale = kMinScale;

							if (currentScale > kMaxScale)
								currentScale = kMaxScale;

							if (m_AllowScaling)
							{
								var yawSign = Mathf.Sign(Vector3.Dot(Quaternion.AngleAxis(90, Vector3.down) * m_StartDirection, rayToRay));
								var currentYaw = m_StartYaw + Vector3.Angle(m_StartDirection, rayToRay) * yawSign;
								var currentRotation = Quaternion.AngleAxis(currentYaw, Vector3.up);
								midPoint = currentRotation * midPoint * currentScale;

								cameraRig.position = m_StartPosition + m_StartMidPoint - midPoint;
								cameraRig.localScale = Vector3.one * currentScale;
								cameraRig.rotation = currentRotation;

								m_MainCamera.nearClipPlane = m_OriginalNearClipPlane * currentScale;
								m_MainCamera.farClipPlane = m_OriginalFarClipPlane * currentScale;
							}
							break;
						}
					}

					if (!otherGrip)
						CancelScale();
				}
			}
			else
			{
				CancelScale();
			}
		}

		bool isVive = proxyType == typeof(ViveProxy);

		if (m_EnableJoystick && (!isVive || m_Thumb != null))
		{
			var viewerCamera = U.Camera.GetMainCamera();

			if (Mathf.Abs(yawValue) > Mathf.Abs(forwardValue))
			{
				if (!Mathf.Approximately(yawValue, 0))
				{
					var speed = yawValue * kSlowRotationSpeed;
					var threshold = isVive ? kRotationThresholdVive : kRotationThreshold;
					if (Mathf.Abs(yawValue) > threshold)
						speed = kFastRotationSpeed * Mathf.Sign(yawValue);

					cameraRig.RotateAround(viewerCamera.transform.position, Vector3.up, speed * Time.unscaledDeltaTime);
					consumeControl(blinkInput.yaw);
				}
			}
			else
			{
				if (!Mathf.Approximately(forwardValue, 0))
				{
					var direction = Vector3.up;

					if (node == Node.LeftHand)
					{
						direction = viewerCamera.transform.forward;
						direction.y = 0;
						direction.Normalize();
					}

					var speed = forwardValue * kSlowMoveSpeed;
					var threshold = isVive ? kMoveThresholdVive : kMoveThreshold;
					if (Mathf.Abs(forwardValue) > threshold)
						speed = kFastMoveSpeed * Mathf.Sign(forwardValue);

					speed *= cameraRig.localScale.x;

					cameraRig.Translate(direction * speed * Time.unscaledDeltaTime, Space.World);
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
				s_ActiveBlinkTool = null;
			}

			consumeControl(blinkInput.blink);
		}
	}

	void CreateWorldScaleVisuals(Transform leftHand, Transform rightHand)
	{
		m_WorldScaleVisuals = U.Object.Instantiate(m_WorldScaleVisualsPrefab, cameraRig, false)
			.GetComponent<WorldScaleVisuals>();
		m_WorldScaleVisuals.leftHand = leftHand;
		m_WorldScaleVisuals.rightHand = rightHand;
		connectInterfaces(m_WorldScaleVisuals);
	}

	void CancelScale()
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

		if (m_WorldScaleVisuals)
			U.Object.Destroy(m_WorldScaleVisuals.gameObject);
	}

	private IEnumerator MoveTowardTarget(Vector3 targetPosition)
	{
		m_State = State.Moving;
		targetPosition = new Vector3(targetPosition.x + (cameraRig.position.x - U.Camera.GetMainCamera().transform.position.x), cameraRig.position.y, targetPosition.z + (cameraRig.position.z - U.Camera.GetMainCamera().transform.position.z));
		const float kTargetDuration = 0.05f;
		var currentPosition = cameraRig.position;
		var currentDuration = 0f;
		while (currentDuration < kTargetDuration)
		{
			currentDuration += Time.unscaledDeltaTime;
			currentPosition = Vector3.Lerp(currentPosition, targetPosition, currentDuration / kTargetDuration);
			cameraRig.position = currentPosition;
			yield return null;
		}

		cameraRig.position = targetPosition;
		m_State = State.Inactive;
		s_ActiveBlinkTool = null;
	}
}
