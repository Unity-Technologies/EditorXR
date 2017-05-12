#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Proxies;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Tools
{
	sealed class BlinkLocomotionTool : MonoBehaviour, ITool, ILocomotor, ICustomRay, IUsesHandedRayOrigin,
		ICustomActionMap, ILinkedObject, IUsesProxyType, IUsesViewerScale

	{
		const float k_FastRotationSpeed = 300f;
		const float k_RotationThreshold = 0.9f;
		const float k_SlowRotationSpeed = 15f;
		const float k_FastMoveSpeed = 10f;
		const float k_MoveThreshold = 0.9f;
		const float k_SlowMoveSpeed = 3f;

		const float k_MoveThresholdVive = 0.8f;
		const float k_RotationThresholdVive = 0.8f;

		//TODO: Fix triangle intersection test at tiny scales, so this can go back to 0.01
		const float k_MinScale = 0.1f;
		const float k_MaxScale = 1000f;

		const string k_WorldScaleProperty = "_WorldScale";

		enum State
		{
			Inactive = 0,
			Aiming = 1,
			Moving = 3
		}

		[SerializeField]
		GameObject m_BlinkVisualsPrefab;

		[SerializeField]
		GameObject m_ViewerScaleVisualsPrefab;

		ViewerScaleVisuals m_ViewerScaleVisuals;

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

		// Allow shared updater to consume these controls for another linked instance
		InputControl m_Grip;
		InputControl m_Thumb;

		public ActionMap actionMap
		{
			get { return m_BlinkActionMap; }
		}

		[SerializeField]
		private ActionMap m_BlinkActionMap;

		public Transform rayOrigin { private get; set; }
		public Node? node { private get; set; }

		public Type proxyType { private get; set; }

		public Transform cameraRig { private get; set; }

		public List<ILinkedObject> linkedObjects { private get; set; }

		private void Start()
		{
			m_BlinkVisualsGO = ObjectUtils.Instantiate(m_BlinkVisualsPrefab, rayOrigin);
			m_BlinkVisuals = m_BlinkVisualsGO.GetComponentInChildren<BlinkVisuals>();
			m_BlinkVisuals.enabled = false;
			m_BlinkVisuals.showValidTargetIndicator = false; // We don't define valid targets, so always show green
			m_BlinkVisualsGO.transform.parent = rayOrigin;
			m_BlinkVisualsGO.transform.localPosition = Vector3.zero;
			m_BlinkVisualsGO.transform.localRotation = Quaternion.identity;

			Shader.SetGlobalFloat(k_WorldScaleProperty, 1);
		}

		private void OnDisable()
		{
			m_State = State.Inactive;
		}

		private void OnDestroy()
		{
			this.ShowDefaultRay(rayOrigin);
		}

		public void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl)
		{
			var blinkInput = (BlinkLocomotion)input;

			if (m_State == State.Moving)
				return;

			foreach (var linkedObject in linkedObjects)
			{
				if (linkedObject.Equals(this))
					continue;

				var blinkTool = (BlinkLocomotionTool)linkedObject;
				if (blinkTool.m_State != State.Inactive)
					return;
			}

			var yawValue = blinkInput.yaw.value;
			var forwardValue = blinkInput.forward.value;

			m_Grip = blinkInput.grip.isHeld ? blinkInput.grip : null;
			m_Thumb = blinkInput.thumb.isHeld ? blinkInput.thumb : null;

			if (this.IsSharedUpdater(this))
			{
				if (m_Grip != null)
				{
					if (m_AllowScaling)
					{
						var otherGrip = false;
						foreach (var linkedObject in linkedObjects)
						{
							if (linkedObject.Equals(this))
								continue;

							var blinkTool = (BlinkLocomotionTool)linkedObject;
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

								var pivotYaw = MathUtilsExt.ConstrainYawRotation(cameraRig.rotation);

								if (!m_Scaling)
								{
									m_StartScale = this.GetViewerScale();
									m_StartDistance = distance;
									m_StartMidPoint = pivotYaw * midPoint * m_StartScale;
									m_StartPosition = cameraRig.position;
									m_StartDirection = rayToRay;
									m_StartYaw = cameraRig.rotation.eulerAngles.y;

									m_EnableJoystick = false;
									blinkTool.m_EnableJoystick = false;

									CreateViewerScaleVisuals(rayOrigin, otherRayOrigin);
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
									this.SetViewerScale(1f);
									cameraRig.position = midPoint - currOffset / currentScale;
									cameraRig.rotation = Quaternion.AngleAxis(m_StartYaw, Vector3.up);

									consumeControl(m_Thumb);
									consumeControl(blinkTool.m_Thumb);

									if (m_ViewerScaleVisuals)
										ObjectUtils.Destroy(m_ViewerScaleVisuals.gameObject);
								}

								if (currentScale < k_MinScale)
									currentScale = k_MinScale;

								if (currentScale > k_MaxScale)
									currentScale = k_MaxScale;

								if (m_AllowScaling)
								{
									var yawSign = Mathf.Sign(Vector3.Dot(Quaternion.AngleAxis(90, Vector3.down) * m_StartDirection, rayToRay));
									var currentYaw = m_StartYaw + Vector3.Angle(m_StartDirection, rayToRay) * yawSign;
									var currentRotation = Quaternion.AngleAxis(currentYaw, Vector3.up);
									midPoint = currentRotation * midPoint * currentScale;

									cameraRig.position = m_StartPosition + m_StartMidPoint - midPoint;
									cameraRig.rotation = currentRotation;

									this.SetViewerScale(currentScale);

									m_ViewerScaleVisuals.viewerScale = currentScale;
									m_BlinkVisuals.viewerScale = currentScale;
									Shader.SetGlobalFloat(k_WorldScaleProperty, 1f / currentScale);
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
				var viewerCamera = CameraUtils.GetMainCamera();

				if (Mathf.Abs(yawValue) > Mathf.Abs(forwardValue))
				{
					if (!Mathf.Approximately(yawValue, 0))
					{
						if (node == Node.LeftHand)
						{
							var direction = viewerCamera.transform.right;
							direction.y = 0;
							direction.Normalize();

							Translate(yawValue, isVive, direction);
						}
						else
						{
							var speed = yawValue * k_SlowRotationSpeed;
							var threshold = isVive ? k_RotationThresholdVive : k_RotationThreshold;
							if (Mathf.Abs(yawValue) > threshold)
								speed = k_FastRotationSpeed * Mathf.Sign(yawValue);

							cameraRig.RotateAround(viewerCamera.transform.position, Vector3.up, speed * Time.deltaTime);
						}

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

						Translate(forwardValue, isVive, direction);
						consumeControl(blinkInput.forward);
					}
				}
			}

			if (blinkInput.blink.wasJustPressed && !m_BlinkVisuals.outOfMaxRange)
			{
				m_State = State.Aiming;
				this.HideDefaultRay(rayOrigin);
				this.LockRay(rayOrigin, this);

				m_BlinkVisuals.ShowVisuals();

				consumeControl(blinkInput.blink);
			}
			else if (m_State == State.Aiming && blinkInput.blink.wasJustReleased)
			{
				this.UnlockRay(rayOrigin, this);
				this.ShowDefaultRay(rayOrigin);

				if (!m_BlinkVisuals.outOfMaxRange)
				{
					m_BlinkVisuals.HideVisuals();
					StartCoroutine(MoveTowardTarget(m_BlinkVisuals.locatorPosition));
				}
				else
				{
					m_BlinkVisuals.enabled = false;
					m_State = State.Inactive;
				}
			}
		}

		void Translate(float inputValue, bool isVive, Vector3 direction)
		{
			var speed = inputValue * k_SlowMoveSpeed;
			var threshold = isVive ? k_MoveThresholdVive : k_MoveThreshold;
			if (Mathf.Abs(inputValue) > threshold)
				speed = k_FastMoveSpeed * Mathf.Sign(inputValue);

			speed *= this.GetViewerScale();

			cameraRig.Translate(direction * speed * Time.deltaTime, Space.World);
		}

		void CreateViewerScaleVisuals(Transform leftHand, Transform rightHand)
		{
			m_ViewerScaleVisuals = ObjectUtils.Instantiate(m_ViewerScaleVisualsPrefab, cameraRig, false).GetComponent<ViewerScaleVisuals>();
			m_ViewerScaleVisuals.leftHand = leftHand;
			m_ViewerScaleVisuals.rightHand = rightHand;
		}

		void CancelScale()
		{
			m_AllowScaling = true;
			m_Scaling = false;

			foreach (var linkedObject in linkedObjects)
			{
				((BlinkLocomotionTool)linkedObject).m_EnableJoystick = true;
			}

			if (m_ViewerScaleVisuals)
				ObjectUtils.Destroy(m_ViewerScaleVisuals.gameObject);
		}

		private IEnumerator MoveTowardTarget(Vector3 targetPosition)
		{
			m_State = State.Moving;
			targetPosition = new Vector3(targetPosition.x + (cameraRig.position.x - CameraUtils.GetMainCamera().transform.position.x), cameraRig.position.y, targetPosition.z + (cameraRig.position.z - CameraUtils.GetMainCamera().transform.position.z));
			const float kTargetDuration = 0.05f;
			var currentPosition = cameraRig.position;
			var currentDuration = 0f;
			while (currentDuration < kTargetDuration)
			{
				currentDuration += Time.deltaTime;
				currentPosition = Vector3.Lerp(currentPosition, targetPosition, currentDuration / kTargetDuration);
				cameraRig.position = currentPosition;
				yield return null;
			}

			cameraRig.position = targetPosition;
			m_State = State.Inactive;
		}
	}
}
#endif
