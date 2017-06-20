#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR.Tools
{
	sealed class LocomotionTool : MonoBehaviour, ITool, ILocomotor, IUsesRayOrigin, ISetDefaultRayVisibility, ICustomActionMap,
		ILinkedObject, IUsesViewerScale, ISettingsMenuItemProvider, ISerializePreferences
	{
		const float k_FastMoveSpeed = 20f;
		const float k_SlowMoveSpeed = 1f;
		const float k_RotationDamping = 0.2f;
		const float k_RotationThreshold = 0.75f;

		//TODO: Fix triangle intersection test at tiny scales, so this can go back to 0.01
		const float k_MinScale = 0.1f;
		const float k_MaxScale = 1000f;

		const string k_WorldScaleProperty = "_WorldScale";

		const int k_RotationSegments = 32;

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

		[SerializeField]
		ActionMap m_BlinkActionMap;

		[SerializeField]
		GameObject m_SettingsMenuItemPrefab;

		[Serializable]
		class Preferences
		{
			[SerializeField]
			bool m_BlinkMode;

			public bool blinkMode { get { return m_BlinkMode; } set { m_BlinkMode = value; } }
		}

		Preferences m_Preferences;

		ViewerScaleVisuals m_ViewerScaleVisuals;

		GameObject m_BlinkVisualsGO;
		BlinkVisuals m_BlinkVisuals;

		State m_State = State.Inactive;

		bool m_AllowScaling = true;
		bool m_Scaling;
		float m_StartScale;
		float m_StartDistance;
		Vector3 m_StartPosition;
		Vector3 m_StartMidPoint;
		Vector3 m_StartDirection;
		float m_StartYaw;

		bool m_Rotating;
		bool m_WasRotating;
		bool m_Crawling;
		Vector3 m_RayOriginStartPosition;
		Vector3 m_RayOriginStartForward;
		Vector3 m_RayOriginStartRight;
		Quaternion m_RigStartRotation;
		Vector3 m_RigStartPosition;
		Vector3 m_CameraStartPosition;
		Quaternion m_LastRotationDiff;

		// Allow shared updater to consume these controls for another linked instance
		InputControl m_Grip;
		InputControl m_Thumb;
		InputControl m_Trigger;

		Camera m_MainCamera;
		float m_OriginalNearClipPlane;
		float m_OriginalFarClipPlane;

		Toggle m_FlyToggle;
		Toggle m_BlinkToggle;
		bool m_BlockValueChangedListener;

		public ActionMap actionMap { get { return m_BlinkActionMap; } }

		public Transform rayOrigin { get; set; }

		public Transform cameraRig { private get; set; }

		public List<ILinkedObject> linkedObjects { private get; set; }

		public GameObject settingsMenuItemPrefab { get { return m_SettingsMenuItemPrefab; } }

		public GameObject settingsMenuItemInstance
		{
			set
			{
				foreach (var toggle in value.GetComponentsInChildren<Toggle>())
				{
					if (toggle.isOn)
					{
						m_FlyToggle = toggle;
						toggle.onValueChanged.AddListener(isOn =>
						{
							if (m_BlockValueChangedListener)
								return;

							// m_Preferences on all instances refer
							m_Preferences.blinkMode = !isOn;
							foreach (LocomotionTool linkedObject in linkedObjects)
							{
								if (linkedObject != this)
								{
									linkedObject.m_BlockValueChangedListener = true;
									//linkedObject.m_ToggleGroup.NotifyToggleOn(isOn ? m_FlyToggle : m_BlinkToggle);
									// HACK: Toggle Group claims these toggles are not a part of the group
									linkedObject.m_FlyToggle.isOn = isOn;
									linkedObject.m_BlinkToggle.isOn = !isOn;
									linkedObject.m_BlockValueChangedListener = false;
								}
							}
						});
					}
					else
					{
						m_BlinkToggle = toggle;
					}
				}
			}
		}

		void Start()
		{
			if (this.IsSharedUpdater(this) && m_Preferences == null)
			{
				m_Preferences = new Preferences();

				// Share one preferences object across all instances
				foreach (LocomotionTool locomotionTool in linkedObjects)
				{
					locomotionTool.m_Preferences = m_Preferences;
				}
			}

			m_BlinkVisualsGO = ObjectUtils.Instantiate(m_BlinkVisualsPrefab, rayOrigin);
			m_BlinkVisuals = m_BlinkVisualsGO.GetComponentInChildren<BlinkVisuals>();
			m_BlinkVisuals.enabled = false;
			m_BlinkVisualsGO.transform.parent = rayOrigin;
			m_BlinkVisualsGO.transform.localPosition = Vector3.zero;
			m_BlinkVisualsGO.transform.localRotation = Quaternion.identity;

			m_MainCamera = CameraUtils.GetMainCamera();
			m_OriginalNearClipPlane = m_MainCamera.nearClipPlane;
			m_OriginalFarClipPlane = m_MainCamera.farClipPlane;

			Shader.SetGlobalFloat(k_WorldScaleProperty, 1);
		}

		void OnDisable()
		{
			m_State = State.Inactive;
		}

		void OnDestroy()
		{
			this.SetDefaultRayVisibility(rayOrigin, true);
		}

		public void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl)
		{
			var blinkInput = (Locomotion)input;

			if (m_State == State.Moving)
				return;

			foreach (LocomotionTool locomotionTool in linkedObjects)
			{
				if (locomotionTool == this)
					continue;

				if (locomotionTool.m_State != State.Inactive)
					return;
			}

			m_Grip = blinkInput.grip.isHeld ? blinkInput.grip : null;
			m_Thumb = blinkInput.thumb.isHeld ? blinkInput.thumb : null;
			m_Trigger = blinkInput.trigger.isHeld ? blinkInput.trigger : null;

			DoTwoHandedScaling(consumeControl);

			if (!m_Scaling)
			{
				if (!m_WasRotating)
					DoCrawl(blinkInput);

				if (m_Preferences.blinkMode)
					DoBlink(consumeControl, blinkInput);
				else
					DoFlying(consumeControl, blinkInput);
			}
			else
			{
				m_Crawling = false;
			}
		}

		void DoFlying(ConsumeControlDelegate consumeControl, Locomotion blinkInput)
		{
			var reverse = blinkInput.reverse.isHeld;
			var moving = blinkInput.forward.isHeld || reverse;
			if (moving)
			{
				if (blinkInput.grip.isHeld)
				{
					var localRayRotation = Quaternion.Inverse(cameraRig.rotation) * rayOrigin.rotation;
					var localRayForward = localRayRotation * Vector3.forward;
					if (Mathf.Abs(Vector3.Dot(localRayForward, Vector3.up)) > k_RotationThreshold)
						return;

					localRayForward.y = 0;
					localRayForward.Normalize();
					if (!m_Rotating)
					{
						m_Rotating = true;
						m_WasRotating = true;
						m_RigStartPosition = cameraRig.position;
						m_RigStartRotation = cameraRig.rotation;

						m_RayOriginStartForward = localRayForward;
						m_RayOriginStartRight = localRayRotation * (reverse ? Vector3.left : Vector3.right);
						m_RayOriginStartRight.y = 0;
						m_RayOriginStartRight.Normalize();

						m_CameraStartPosition = CameraUtils.GetMainCamera().transform.position;
						m_LastRotationDiff = Quaternion.identity;
					}

					consumeControl(blinkInput.grip);
					var startOffset = m_RigStartPosition - m_CameraStartPosition;

					var angle = Vector3.Angle(m_RayOriginStartForward, localRayForward);
					var dot = Vector3.Dot(m_RayOriginStartRight, localRayForward);
					var rotation = Quaternion.Euler(0, angle * Mathf.Sign(dot), 0);
					var filteredRotation = Quaternion.Lerp(m_LastRotationDiff, rotation, k_RotationDamping);

					const float segmentSize = 360f / k_RotationSegments;
					var segmentedRotation = Quaternion.Euler(0, Mathf.Round(filteredRotation.eulerAngles.y / segmentSize) * segmentSize, 0);

					cameraRig.rotation = m_RigStartRotation * segmentedRotation;
					cameraRig.position = m_CameraStartPosition + segmentedRotation * startOffset;

					m_LastRotationDiff = filteredRotation;
				}
				else
				{
					var speed = k_SlowMoveSpeed;
					var speedControl = blinkInput.speed;
					var speedControlValue = speedControl.value;
					if (!Mathf.Approximately(speedControlValue, 0)) // Consume control to block selection
					{
						speed = k_SlowMoveSpeed + speedControlValue * (k_FastMoveSpeed - k_SlowMoveSpeed);
						consumeControl(speedControl);
					}

					speed *= this.GetViewerScale();
					if (reverse)
						speed *= -1;

					m_Rotating = false;
					cameraRig.Translate(Quaternion.Inverse(cameraRig.rotation) * rayOrigin.forward * speed * Time.unscaledDeltaTime);
				}

				consumeControl(blinkInput.forward);
			}
			else
			{
				if (!blinkInput.grip.isHeld)
					m_WasRotating = false;

				m_Rotating = false;
			}
		}

		void DoCrawl(Locomotion blinkInput)
		{
			if (!blinkInput.forward.isHeld && !blinkInput.blink.isHeld && blinkInput.grip.isHeld)
			{
				if (!m_Crawling)
				{
					m_Crawling = true;
					m_RigStartPosition = cameraRig.position;
					m_RayOriginStartPosition = m_RigStartPosition - rayOrigin.position;
				}

				var localRayPosition = cameraRig.position - rayOrigin.position;
				cameraRig.position = m_RigStartPosition + (localRayPosition - m_RayOriginStartPosition);

				// Do not consume grip control to allow passing through for multi-select
			}
			else
			{
				m_Crawling = false;
			}
		}

		void DoBlink(ConsumeControlDelegate consumeControl, Locomotion blinkInput)
		{
			m_Rotating = false;
			if (blinkInput.blink.wasJustPressed && !m_BlinkVisuals.outOfMaxRange)
			{
				m_State = State.Aiming;
				this.SetDefaultRayVisibility(rayOrigin, false);
				this.LockRay(rayOrigin, this);

				m_BlinkVisuals.ShowVisuals();

				consumeControl(blinkInput.blink);
			}
			else if (m_State == State.Aiming && blinkInput.blink.wasJustReleased)
			{
				this.UnlockRay(rayOrigin, this);
				this.SetDefaultRayVisibility(rayOrigin, true);

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

		void DoTwoHandedScaling(ConsumeControlDelegate consumeControl)
		{
			if (this.IsSharedUpdater(this))
			{
				if (m_Grip != null)
				{
					if (m_AllowScaling)
					{
						var otherGrip = false;
						foreach (LocomotionTool locomotionTool in linkedObjects)
						{
							if (locomotionTool == this)
								continue;

							if (locomotionTool.m_Grip != null)
							{
								otherGrip = true;
								consumeControl(m_Grip);
								consumeControl(locomotionTool.m_Grip);

								var thisPosition = cameraRig.InverseTransformPoint(rayOrigin.position);
								var otherRayOrigin = locomotionTool.rayOrigin;
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

									locomotionTool.m_Scaling = true;

									CreateViewerScaleVisuals(rayOrigin, otherRayOrigin);
								}

								m_Scaling = true;

								var currentScale = Mathf.Clamp(m_StartScale * (m_StartDistance / distance), k_MinScale, k_MaxScale);

								if (m_Thumb != null)
									consumeControl(m_Thumb);

								if (locomotionTool.m_Thumb != null)
									consumeControl(locomotionTool.m_Thumb);

								// Press both thumb buttons to reset scale
								if (m_Thumb != null && locomotionTool.m_Thumb != null)
								{
									m_AllowScaling = false;

									rayToRay = otherRayOrigin.position - rayOrigin.position;
									midPoint = rayOrigin.position + rayToRay * 0.5f;
									var currOffset = midPoint - cameraRig.position;

									cameraRig.position = midPoint - currOffset / currentScale;
									cameraRig.rotation = Quaternion.AngleAxis(m_StartYaw, Vector3.up);

									ResetViewerScale();
								}

								if (m_Thumb != null)
									consumeControl(m_Thumb);

								if (locomotionTool.m_Thumb != null)
									consumeControl(locomotionTool.m_Thumb);

								// Press both triggers to reset to origin
								if (m_Trigger != null && locomotionTool.m_Trigger != null)
								{
									m_AllowScaling = false;

									cameraRig.position = Vector3.up * VRView.HeadHeight;
									cameraRig.rotation = Quaternion.identity;

									ResetViewerScale();
								}

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
		}

		void ResetViewerScale()
		{
			cameraRig.localScale = Vector3.one;
			m_MainCamera.nearClipPlane = m_OriginalNearClipPlane;
			m_MainCamera.farClipPlane = m_OriginalFarClipPlane;

			if (m_ViewerScaleVisuals)
				ObjectUtils.Destroy(m_ViewerScaleVisuals.gameObject);
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
				((LocomotionTool)linkedObject).m_Scaling = false;
			}

			if (m_ViewerScaleVisuals)
				ObjectUtils.Destroy(m_ViewerScaleVisuals.gameObject);
		}

		IEnumerator MoveTowardTarget(Vector3 targetPosition)
		{
			m_State = State.Moving;
			targetPosition = new Vector3(targetPosition.x + (cameraRig.position.x - CameraUtils.GetMainCamera().transform.position.x), cameraRig.position.y, targetPosition.z + (cameraRig.position.z - CameraUtils.GetMainCamera().transform.position.z));
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
		}

		public object OnSerializePreferences()
		{
			if (this.IsSharedUpdater(this))
			{
				// Share one preferences object across all instances
				foreach (LocomotionTool locomotionTool in linkedObjects)
				{
					locomotionTool.m_Preferences = m_Preferences;
				}

				return m_Preferences;
			}

			return null;
		}

		public void OnDeserializePreferences(object obj)
		{
			if (this.IsSharedUpdater(this))
			{
				var preferences = obj as Preferences;
				if (preferences != null)
					m_Preferences = preferences;

				// Share one preferences object across all instances
				foreach (LocomotionTool locomotionTool in linkedObjects)
				{
					locomotionTool.m_Preferences = m_Preferences;
				}
			}
		}
	}
}
#endif
