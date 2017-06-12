#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR.Tools
{
	sealed class LocomotionTool : MonoBehaviour, ITool, ILocomotor, IUsesRayOrigin, ISetDefaultRayVisibility, ICustomActionMap,
		ILinkedObject, IUsesViewerScale, ISettingsMenuItemProvider
	{
		const float k_FastMoveSpeed = 25f;
		const float k_SlowMoveSpeed = 5f;
		const float k_RotationDamping = 0.2f;

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

		[SerializeField]
		ActionMap m_BlinkActionMap;

		[SerializeField]
		GameObject m_SettingsMenuItemPrefab;

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
		bool m_GrabMoving;
		Vector3 m_RayOriginStartPosition;
		Quaternion m_RayOriginStartRotation;
		Quaternion m_RigStartRotation;
		Vector3 m_RigStartPosition;
		Vector3 m_CameraStartPosition;
		Quaternion m_LastRotationDiff;

		// Allow shared updater to consume these controls for another linked instance
		InputControl m_Grip;
		InputControl m_Thumb;

		Camera m_MainCamera;
		float m_OriginalNearClipPlane;
		float m_OriginalFarClipPlane;

		public ActionMap actionMap { get { return m_BlinkActionMap; } }

		public Transform rayOrigin { private get; set; }

		public Transform cameraRig { private get; set; }

		public List<ILinkedObject> linkedObjects { private get; set; }

		public bool blinkMode { private get; set; }

		public GameObject settingsMenuItemPrefab { get { return m_SettingsMenuItemPrefab; } }

		public GameObject settingsMenuItemInstance
		{
			set
			{
				foreach (var toggle in value.GetComponentsInChildren<Toggle>())
				{
					if (toggle.isOn)
					{
						toggle.onValueChanged.AddListener(isOn =>
						{
							blinkMode = !isOn;
						});
					}
				}
			}
		}

		void Start()
		{
			m_BlinkVisualsGO = ObjectUtils.Instantiate(m_BlinkVisualsPrefab, rayOrigin);
			m_BlinkVisuals = m_BlinkVisualsGO.GetComponentInChildren<BlinkVisuals>();
			m_BlinkVisuals.enabled = false;
			m_BlinkVisuals.showValidTargetIndicator = false; // We don't define valid targets, so always show green
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

		void Update()
		{
			if (UnityEngine.Input.GetKeyUp(KeyCode.Space))
				blinkMode = !blinkMode;
		}

		public void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl)
		{
			var blinkInput = (Locomotion)input;

			if (m_State == State.Moving)
				return;

			foreach (var linkedObject in linkedObjects)
			{
				if (linkedObject == this)
					continue;

				var blinkTool = (LocomotionTool)linkedObject;
				if (blinkTool.m_State != State.Inactive)
					return;
			}

			m_Grip = blinkInput.grip.isHeld ? blinkInput.grip : null;
			m_Thumb = blinkInput.thumb.isHeld ? blinkInput.thumb : null;

			DoTwoHandedScaling(consumeControl);

			if (!m_Scaling)
			{
				DoCrawl(blinkInput);

				if (blinkMode)
					DoBlink(consumeControl, blinkInput);
				else
					DoFlying(consumeControl, blinkInput);
			}
			else
			{
				m_GrabMoving = false;
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
					if (!m_Rotating)
					{
						m_Rotating = true;
						m_RigStartPosition = cameraRig.position;
						m_RigStartRotation = cameraRig.rotation;
						m_RayOriginStartRotation = MathUtilsExt.ConstrainYawRotation(Quaternion.Inverse(cameraRig.rotation) * rayOrigin.rotation);
						m_CameraStartPosition = CameraUtils.GetMainCamera().transform.position;
						m_LastRotationDiff = Quaternion.identity;
					}

					consumeControl(blinkInput.grip);
					var startOffset = m_RigStartPosition - m_CameraStartPosition;
					var localRayRotation = MathUtilsExt.ConstrainYawRotation(Quaternion.Inverse(cameraRig.rotation) * rayOrigin.rotation);
					var rotation = Quaternion.Inverse(m_RayOriginStartRotation) * localRayRotation;
					var filteredRotation = Quaternion.Lerp(m_LastRotationDiff, rotation, k_RotationDamping);

					cameraRig.rotation = m_RigStartRotation * filteredRotation;
					cameraRig.position = m_CameraStartPosition + filteredRotation * startOffset;

					m_LastRotationDiff = filteredRotation;
				}
				else
				{
					var speed = k_SlowMoveSpeed;
					if (blinkInput.trigger.isHeld || blinkInput.trigger.wasJustReleased) // Consume control on release to block selection
					{
						speed = k_FastMoveSpeed;
						consumeControl(blinkInput.trigger);
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
				m_Rotating = false;
			}
		}

		void DoCrawl(Locomotion blinkInput)
		{
			if (!blinkInput.forward.isHeld && !blinkInput.blink.isHeld && blinkInput.grip.isHeld)
			{
				if (!m_GrabMoving)
				{
					m_GrabMoving = true;
					m_RigStartPosition = cameraRig.position;
					m_RayOriginStartPosition = m_RigStartPosition - rayOrigin.position;
				}

				var localRayPosition = cameraRig.position - rayOrigin.position;
				cameraRig.position = m_RigStartPosition + (localRayPosition - m_RayOriginStartPosition);

				// Do not consume grip control to allow passing through for multi-select
			}
			else
			{
				m_GrabMoving = false;
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
						foreach (var linkedObject in linkedObjects)
						{
							if (linkedObject == this)
								continue;

							var blinkTool = (LocomotionTool)linkedObject;
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

									blinkTool.m_Scaling = true;

									CreateViewerScaleVisuals(rayOrigin, otherRayOrigin);
								}

								m_Scaling = true;

								var currentScale = Mathf.Clamp(m_StartScale * (m_StartDistance / distance), k_MinScale, k_MaxScale);

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

									if (m_ViewerScaleVisuals)
										ObjectUtils.Destroy(m_ViewerScaleVisuals.gameObject);
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
	}
}
#endif
