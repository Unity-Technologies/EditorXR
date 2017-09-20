#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEditor.Experimental.EditorVR.Proxies;
using UnityEditor.Experimental.EditorVR.UI;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR.Tools
{
	sealed class LocomotionTool : MonoBehaviour, ITool, ILocomotor, IUsesRayOrigin, IRayVisibilitySettings,
		ICustomActionMap, ILinkedObject, IUsesViewerScale, ISettingsMenuItemProvider, ISerializePreferences,
		IUsesProxyType, IGetVRPlayerObjects, IRequestFeedback, IUsesNode
	{
		const float k_FastMoveSpeed = 20f;
		const float k_SlowMoveSpeed = 1f;
		const float k_RotationDamping = 0.2f;
		const float k_RotationThreshold = 0.75f;
		const float k_DistanceThreshold = 0.02f;

		//TODO: Fix triangle intersection test at tiny scales, so this can go back to 0.01
		const float k_MinScale = 0.1f;
		const float k_MaxScale = 1000f;

		const string k_WorldScaleProperty = "_WorldScale";

		const int k_RotationSegments = 32;

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

		bool m_AllowScaling = true;
		bool m_Scaling;
		float m_StartScale;
		float m_StartDistance;
		Vector3 m_StartPosition;
		Vector3 m_StartMidPoint;
		Vector3 m_StartDirection;
		float m_StartYaw;

		bool m_Rotating;
		bool m_StartCrawling;
		bool m_Crawling;
		bool m_WasRotating;
		float m_CrawlStartTime;
		Vector3 m_ActualRayOriginStartPosition;
		Vector3 m_RayOriginStartPosition;
		Vector3 m_RayOriginStartForward;
		Vector3 m_RayOriginStartRight;
		Quaternion m_RigStartRotation;
		Vector3 m_RigStartPosition;
		Vector3 m_CameraStartPosition;
		Quaternion m_LastRotationDiff;

		bool m_BlinkMoving;

		// Allow shared updater to check input values and consume controls
		LocomotionInput m_LocomotionInput;

		Camera m_MainCamera;
		float m_OriginalNearClipPlane;
		float m_OriginalFarClipPlane;

		Toggle m_FlyToggle;
		Toggle m_BlinkToggle;
		bool m_BlockValueChangedListener;

		readonly Dictionary<string, List<VRInputDevice.VRControl>> m_Controls = new Dictionary<string, List<VRInputDevice.VRControl>>();
		readonly List<ProxyFeedbackRequest> m_MainButtonFeedback = new List<ProxyFeedbackRequest>();
		readonly List<ProxyFeedbackRequest> m_SpeedFeedback = new List<ProxyFeedbackRequest>();
		readonly List<ProxyFeedbackRequest> m_CrawlFeedback = new List<ProxyFeedbackRequest>();
		readonly List<ProxyFeedbackRequest> m_ScaleFeedback = new List<ProxyFeedbackRequest>();
		readonly List<ProxyFeedbackRequest> m_RotateFeedback = new List<ProxyFeedbackRequest>();
		readonly List<ProxyFeedbackRequest> m_ResetScaleFeedback = new List<ProxyFeedbackRequest>();


		public ActionMap actionMap { get { return m_BlinkActionMap; } }

		public Transform rayOrigin { get; set; }

		public Transform cameraRig { private get; set; }

		public List<ILinkedObject> linkedObjects { private get; set; }

		public GameObject settingsMenuItemPrefab { get { return m_SettingsMenuItemPrefab; } }

		public GameObject settingsMenuItemInstance
		{
			set
			{
				var defaultToggleGroup = value.GetComponentInChildren<DefaultToggleGroup>();
				foreach (var toggle in value.GetComponentsInChildren<Toggle>())
				{
					if (toggle == defaultToggleGroup.defaultToggle)
					{
						m_FlyToggle = toggle;
						toggle.onValueChanged.AddListener(isOn =>
						{
							if (m_BlockValueChangedListener)
								return;

							// m_Preferences on all instances refer
							m_Preferences.blinkMode = !isOn;
							foreach (var linkedObject in linkedObjects)
							{
								var locomotionTool = (LocomotionTool)linkedObject;
								if (locomotionTool != this)
								{
									locomotionTool.m_BlockValueChangedListener = true;
									//linkedObject.m_ToggleGroup.NotifyToggleOn(isOn ? m_FlyToggle : m_BlinkToggle);
									// HACK: Toggle Group claims these toggles are not a part of the group
									locomotionTool.m_FlyToggle.isOn = isOn;
									locomotionTool.m_BlinkToggle.isOn = !isOn;
									locomotionTool.m_BlockValueChangedListener = false;
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

		public Type proxyType { get; set; }

		public Node? node { private get; set; }

		void Start()
		{
			if (this.IsSharedUpdater(this) && m_Preferences == null)
			{
				m_Preferences = new Preferences();

				// Share one preferences object across all instances
				foreach (var linkedObject in linkedObjects)
				{
					((LocomotionTool)linkedObject).m_Preferences = m_Preferences;
				}
			}

			m_BlinkVisualsGO = ObjectUtils.Instantiate(m_BlinkVisualsPrefab, rayOrigin);
			m_BlinkVisuals = m_BlinkVisualsGO.GetComponentInChildren<BlinkVisuals>();
			m_BlinkVisuals.ignoreList = this.GetVRPlayerObjects();
			m_BlinkVisualsGO.SetActive(false);
			m_BlinkVisualsGO.transform.parent = rayOrigin;
			m_BlinkVisualsGO.transform.localPosition = Vector3.zero;
			m_BlinkVisualsGO.transform.localRotation = Quaternion.identity;

			m_MainCamera = CameraUtils.GetMainCamera();
			m_OriginalNearClipPlane = m_MainCamera.nearClipPlane;
			m_OriginalFarClipPlane = m_MainCamera.farClipPlane;

			Shader.SetGlobalFloat(k_WorldScaleProperty, 1);

			var viewerScaleObject = ObjectUtils.Instantiate(m_ViewerScaleVisualsPrefab, cameraRig, false);
			m_ViewerScaleVisuals = viewerScaleObject.GetComponent<ViewerScaleVisuals>();
			viewerScaleObject.SetActive(false);

			var actions = m_BlinkActionMap.actions;
			foreach (var scheme in m_BlinkActionMap.controlSchemes)
			{
				var bindings = scheme.bindings;
				for (var i = 0; i < bindings.Count; i++)
				{
					var binding = bindings[i];
					var action = actions[i].name;
					List<VRInputDevice.VRControl> controls;
					if (!m_Controls.TryGetValue(action, out controls))
					{
						controls = new List<VRInputDevice.VRControl>();
						m_Controls[action] = controls;
					}

					foreach (var source in binding.sources)
					{
						m_Controls[action].Add((VRInputDevice.VRControl)source.controlIndex);
					}
				}
			}

			ShowCrawlFeedback();
			ShowMainButtonFeedback();
		}

		void OnDestroy()
		{
			this.RemoveRayVisibilitySettings(rayOrigin, this);
			this.ClearFeedbackRequests();

			if (m_ViewerScaleVisuals)
				ObjectUtils.Destroy(m_ViewerScaleVisuals.gameObject);
		}

		public void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl)
		{
			m_LocomotionInput = (LocomotionInput)input;

			if (DoTwoHandedScaling(consumeControl))
				return;

			if (DoRotating(consumeControl))
				return;

			if (m_Preferences.blinkMode)
			{
				if (DoBlink(consumeControl))
					return;
			}
			else
			{
				if (DoFlying(consumeControl))
					return;
			}

			DoCrawl(consumeControl);
		}

		bool DoFlying(ConsumeControlDelegate consumeControl)
		{
			foreach (var linkedObject in linkedObjects)
			{
				var locomotionTool = (LocomotionTool)linkedObject;
				if (locomotionTool.m_LocomotionInput != null && locomotionTool.m_LocomotionInput.crawl.isHeld)
					return false;
			}

			var forwardControl = m_LocomotionInput.forward;
			var reverseControl = m_LocomotionInput.reverse;
			if (forwardControl.wasJustPressed || reverseControl.wasJustPressed)
			{
				foreach (var linkedObject in linkedObjects)
				{
					var locomotionTool = (LocomotionTool)linkedObject;
					if (locomotionTool == this)
					{
						locomotionTool.HideMainButtonFeedback();
						locomotionTool.ShowRotateFeedback();
					}

					locomotionTool.HideCrawlFeedback();
				}
			}

			if (forwardControl.wasJustReleased || reverseControl.wasJustReleased)
			{
				var otherControlHeld = false;
				foreach (var linkedObject in linkedObjects)
				{
					var locomotionTool = (LocomotionTool)linkedObject;
					if (locomotionTool == this)
						continue;

					var input = locomotionTool.m_LocomotionInput;
					if (input.forward.isHeld || input.reverse.isHeld)
					{
						otherControlHeld = true;
						break;
					}
				}

				foreach (var linkedObject in linkedObjects)
				{
					var locomotionTool = (LocomotionTool)linkedObject;
					if (locomotionTool == this)
					{
						locomotionTool.HideSpeedFeedback();
						locomotionTool.HideRotateFeedback();
						locomotionTool.ShowMainButtonFeedback();
					}

					if (!otherControlHeld)
						locomotionTool.ShowCrawlFeedback();
				}
			}

			var reverse = reverseControl.isHeld;
			var moving = forwardControl.isHeld || reverse;
			if (moving)
			{
				var speed = k_SlowMoveSpeed;
				var speedControl = m_LocomotionInput.speed;
				var speedControlValue = speedControl.value;
				if (!Mathf.Approximately(speedControlValue, 0)) // Consume control to block selection
				{
					speed = k_SlowMoveSpeed + speedControlValue * (k_FastMoveSpeed - k_SlowMoveSpeed);
					consumeControl(speedControl);
					HideSpeedFeedback();
				}
				else if (m_SpeedFeedback.Count == 0)
				{
					ShowSpeedFeedback();
				}

				speed *= this.GetViewerScale();
				if (reverse)
					speed *= -1;

				m_Rotating = false;
				cameraRig.Translate(Quaternion.Inverse(cameraRig.rotation) * rayOrigin.forward * speed * Time.unscaledDeltaTime);

				consumeControl(forwardControl);
				return true;
			}

			return false;
		}

		bool DoRotating(ConsumeControlDelegate consumeControl)
		{
			var reverse = m_LocomotionInput.reverse.isHeld;
			var move = m_LocomotionInput.forward.isHeld || reverse;
			if (move)
			{
				if (m_LocomotionInput.rotate.isHeld)
				{
					foreach (var linkedObject in linkedObjects)
					{
						var locomotionTool = (LocomotionTool)linkedObject;
						locomotionTool.HideMainButtonFeedback();
						locomotionTool.HideRotateFeedback();
						locomotionTool.HideScaleFeedback();
						locomotionTool.HideSpeedFeedback();
					}

					var localRayRotation = Quaternion.Inverse(cameraRig.rotation) * rayOrigin.rotation;
					var localRayForward = localRayRotation * Vector3.forward;
					if (Mathf.Abs(Vector3.Dot(localRayForward, Vector3.up)) > k_RotationThreshold)
						return true;

					localRayForward.y = 0;
					localRayForward.Normalize();
					if (!m_Rotating)
					{
						m_Rotating = true;
						m_WasRotating = true;
						m_RigStartPosition = cameraRig.position;
						m_RigStartRotation = cameraRig.rotation;

						m_RayOriginStartForward = localRayForward;
						m_RayOriginStartRight = localRayRotation * (reverse ? Vector3.right : Vector3.left);
						m_RayOriginStartRight.y = 0;
						m_RayOriginStartRight.Normalize();

						m_CameraStartPosition = CameraUtils.GetMainCamera().transform.position;
						m_LastRotationDiff = Quaternion.identity;
					}

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
					m_BlinkVisuals.visible = false;

					m_StartCrawling = false;
					m_Crawling = false;
					return true;
				}
			}

			if (!m_LocomotionInput.rotate.isHeld)
			{
				if (m_WasRotating)
				{
					foreach (var linkedObject in linkedObjects)
					{
						var locomotionTool = (LocomotionTool)linkedObject;
						var input = locomotionTool.m_LocomotionInput;
						if (input.blink.isHeld)
						{
							locomotionTool.ShowSpeedFeedback();
							locomotionTool.ShowRotateFeedback();
						}
						else
						{
							if (locomotionTool == this)
								locomotionTool.ShowAltRotateFeedback();
							else if (!input.crawl.isHeld)
								locomotionTool.ShowMainButtonFeedback();
						}
					}
				}

				m_WasRotating = false;
			}

			m_Rotating = false;
			return false;
		}

		bool DoCrawl(ConsumeControlDelegate consumeControl)
		{
			var crawl = m_LocomotionInput.crawl;
			var blink = m_LocomotionInput.blink;
			if (!m_LocomotionInput.forward.isHeld && !blink.isHeld && crawl.isHeld)
			{
				if (!m_StartCrawling && !m_WasRotating)
				{
					m_StartCrawling = true;
					m_ActualRayOriginStartPosition = m_RayOriginStartPosition;
					m_CrawlStartTime = Time.time;

					foreach (var linkedObject in linkedObjects)
					{
						((LocomotionTool)linkedObject).HideCrawlFeedback();
						((LocomotionTool)linkedObject).HideMainButtonFeedback();
					}
				}

				var localRayPosition = cameraRig.position - rayOrigin.position;
				var distance = Vector3.Distance(m_ActualRayOriginStartPosition, localRayPosition);
				var distanceThreshold = distance > k_DistanceThreshold * this.GetViewerScale();
				var timeThreshold = Time.time > m_CrawlStartTime + UIUtils.DoubleClickIntervalMax;
				if (!m_Crawling && m_StartCrawling && (timeThreshold || distanceThreshold))
				{
					m_Crawling = true;
					m_RigStartPosition = cameraRig.position;
					m_RayOriginStartPosition = m_RigStartPosition - rayOrigin.position;
					this.AddRayVisibilitySettings(rayOrigin, this, false, false);
				}

				if (m_Crawling)
					cameraRig.position = m_RigStartPosition + localRayPosition - m_RayOriginStartPosition;

				if (m_RotateFeedback.Count == 0)
				{
					HideMainButtonFeedback();
					ShowAltRotateFeedback();
				}

				if (m_ScaleFeedback.Count == 0)
					ShowScaleFeedback();

				return true;
			}

			this.RemoveRayVisibilitySettings(rayOrigin, this);

			if (crawl.isHeld && blink.wasJustReleased || crawl.wasJustReleased)
			{
				var otherCrawlHeld = false;
				foreach (var linkedObject in linkedObjects)
				{
					var locomotionTool = (LocomotionTool)linkedObject;
					if (locomotionTool == this)
						continue;

					if (locomotionTool.m_LocomotionInput.crawl.isHeld)
					{
						otherCrawlHeld = true;
						break;
					}
				}

				if (!otherCrawlHeld)
				{
					HideRotateFeedback();
					HideScaleFeedback();
					foreach (var linkedObject in linkedObjects)
					{
						var locomotionTool = (LocomotionTool)linkedObject;
						locomotionTool.ShowCrawlFeedback();
						locomotionTool.ShowMainButtonFeedback();
					}
				}
			}

			m_StartCrawling = false;
			m_Crawling = false;
			return false;
		}

		bool DoBlink(ConsumeControlDelegate consumeControl)
		{
			if (m_LocomotionInput.blink.wasJustPressed)
				ShowRotateFeedback();

			if (m_LocomotionInput.blink.isHeld)
			{
				this.AddRayVisibilitySettings(rayOrigin, this, false, false);
				m_BlinkVisuals.extraSpeed = m_LocomotionInput.speed.value;
				m_BlinkVisuals.visible = true;

				consumeControl(m_LocomotionInput.blink);
				return true;
			}

			if (m_LocomotionInput.blink.wasJustReleased)
			{
				this.RemoveRayVisibilitySettings(rayOrigin, this);

				m_BlinkVisuals.visible = false;

				if (m_BlinkVisuals.targetPosition.HasValue)
					StartCoroutine(MoveTowardTarget(m_BlinkVisuals.targetPosition.Value));

				return true;
			}

			this.RemoveRayVisibilitySettings(rayOrigin, this);

			return m_BlinkMoving;
		}

		bool DoTwoHandedScaling(ConsumeControlDelegate consumeControl)
		{
			foreach (var linkedObject in linkedObjects)
			{
				if (((LocomotionTool)linkedObject).m_Rotating)
					return false;
			}

			if (this.IsSharedUpdater(this))
			{
				var crawl = m_LocomotionInput.crawl;
				if (crawl.isHeld)
				{
					if (m_AllowScaling)
					{
						var otherGripHeld = false;
						foreach (var linkedObject in linkedObjects)
						{
							var otherLocomotionTool = (LocomotionTool)linkedObject;
							if (otherLocomotionTool == this)
								continue;

							var otherLocomotionInput = otherLocomotionTool.m_LocomotionInput;
							if (otherLocomotionInput == null) // This can occur if crawl is pressed when EVR is opened
								continue;

							var otherCrawl = otherLocomotionInput.crawl;
							if (otherCrawl.isHeld)
							{
								otherGripHeld = true;
								consumeControl(crawl);
								consumeControl(otherCrawl);

								// Also consume thumbstick axes to disable radial menu
								consumeControl(m_LocomotionInput.horizontal);
								consumeControl(m_LocomotionInput.vertical);
								consumeControl(otherLocomotionInput.horizontal);
								consumeControl(otherLocomotionInput.vertical);

								var thisPosition = cameraRig.InverseTransformPoint(rayOrigin.position);
								var otherRayOrigin = otherLocomotionTool.rayOrigin;
								var otherPosition = cameraRig.InverseTransformPoint(otherRayOrigin.position);
								var distance = Vector3.Distance(thisPosition, otherPosition);

								this.AddRayVisibilitySettings(rayOrigin, this, false, false);
								this.AddRayVisibilitySettings(otherRayOrigin, this, false, false);

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

									otherLocomotionTool.m_Scaling = true;
									otherLocomotionTool.m_Crawling = false;
									otherLocomotionTool.m_StartCrawling = false;

									m_ViewerScaleVisuals.leftHand = rayOrigin;
									m_ViewerScaleVisuals.rightHand = otherRayOrigin;
									m_ViewerScaleVisuals.gameObject.SetActive(true);

									foreach (var obj in linkedObjects)
									{
										var locomotionTool = (LocomotionTool)obj;
										locomotionTool.HideScaleFeedback();
										locomotionTool.HideRotateFeedback();
										locomotionTool.HideMainButtonFeedback();
										locomotionTool.ShowResetScaleFeedback();
									}
								}

								m_Scaling = true;
								m_StartCrawling = false;
								m_Crawling = false;

								var currentScale = Mathf.Clamp(m_StartScale * (m_StartDistance / distance), k_MinScale, k_MaxScale);

								var scaleReset = m_LocomotionInput.scaleReset;
								var scaleResetHeld = scaleReset.isHeld;
								if (scaleResetHeld)
									consumeControl(scaleReset);

								var otherScaleReset = otherLocomotionInput.scaleReset;
								var otherScaleResetHeld = otherScaleReset.isHeld;
								if (otherScaleResetHeld)
									consumeControl(otherScaleReset);

								// Press both thumb buttons to reset scale
								if (scaleResetHeld && otherScaleResetHeld)
								{
									m_AllowScaling = false;

									rayToRay = otherRayOrigin.position - rayOrigin.position;
									midPoint = rayOrigin.position + rayToRay * 0.5f;
									var currOffset = midPoint - cameraRig.position;

									cameraRig.position = midPoint - currOffset / currentScale;
									cameraRig.rotation = Quaternion.AngleAxis(m_StartYaw, Vector3.up);

									ResetViewerScale();
								}

								var worldReset = m_LocomotionInput.worldReset;
								var worldResetHeld = worldReset.isHeld;
								if (worldResetHeld)
									consumeControl(worldReset);

								var otherWorldReset = otherLocomotionInput.worldReset;
								var otherWorldResetHeld = otherWorldReset.isHeld;
								if (otherWorldResetHeld)
									consumeControl(otherWorldReset);

								// Press both triggers to reset to origin
								if (worldResetHeld && otherWorldResetHeld)
								{
									m_AllowScaling = false;
#if UNITY_EDITORVR
									cameraRig.position = VRView.headCenteredOrigin;
#endif
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

						if (!otherGripHeld)
							CancelScale();
					}
				}
				else
				{
					CancelScale();
				}
			}

			return m_Scaling;
		}

		void ResetViewerScale()
		{
			cameraRig.localScale = Vector3.one;
			m_MainCamera.nearClipPlane = m_OriginalNearClipPlane;
			m_MainCamera.farClipPlane = m_OriginalFarClipPlane;
			m_ViewerScaleVisuals.gameObject.SetActive(false);
		}

		void CancelScale()
		{
			m_AllowScaling = true;
			m_Scaling = false;

			foreach (var linkedObject in linkedObjects)
			{
				var locomotionTool = (LocomotionTool)linkedObject;

				if (!locomotionTool.m_Crawling && !locomotionTool.m_BlinkVisuals.gameObject.activeInHierarchy)
				{
					var rayOrigin = locomotionTool.rayOrigin;
					this.RemoveRayVisibilitySettings(rayOrigin, this);
				}

				locomotionTool.m_Scaling = false;
				locomotionTool.HideResetScaleFeedback();
			}

			m_ViewerScaleVisuals.gameObject.SetActive(false);
		}

		IEnumerator MoveTowardTarget(Vector3 targetPosition)
		{
			m_BlinkMoving = true;

			var offset = cameraRig.position - CameraUtils.GetMainCamera().transform.position;
			offset.y = 0;
#if UNITY_EDITORVR
			offset += VRView.HeadHeight * Vector3.up * this.GetViewerScale();
#endif
			targetPosition += offset;
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
			m_BlinkMoving = false;
		}

		void ShowCrawlFeedback()
		{
			List<VRInputDevice.VRControl> ids;
			if (m_Controls.TryGetValue("Crawl", out ids))
			{
				foreach (var id in ids)
				{
					var request = new ProxyFeedbackRequest
					{
						node = node.Value,
						control = id,
						tooltipText = "Crawl"
					};

					this.AddFeedbackRequest(request);
					m_CrawlFeedback.Add(request);
				}
			}
		}

		void ShowMainButtonFeedback()
		{
			List<VRInputDevice.VRControl> ids;
			if (m_Controls.TryGetValue("Blink", out ids))
			{
				foreach (var id in ids)
				{
					var request = new ProxyFeedbackRequest
					{
						node = node.Value,
						control = id,
						tooltipText = m_Preferences.blinkMode ? "Blink" : "Fly"
					};

					this.AddFeedbackRequest(request);
					m_MainButtonFeedback.Add(request);
				}
			}
		}

		void ShowRotateFeedback()
		{
			List<VRInputDevice.VRControl> ids;
			if (m_Controls.TryGetValue("Rotate", out ids))
			{
				foreach (var id in ids)
				{
					var request = new ProxyFeedbackRequest
					{
						control = id,
						node = node.Value,
						tooltipText = "Rotate"
					};

					this.AddFeedbackRequest(request);
					m_RotateFeedback.Add(request);
				}
			}
		}

		void ShowAltRotateFeedback()
		{
			List<VRInputDevice.VRControl> ids;
			if (m_Controls.TryGetValue("Blink", out ids))
			{
				foreach (var id in ids)
				{
					var request = new ProxyFeedbackRequest
					{
						control = id,
						node = node.Value,
						tooltipText = "Rotate"
					};

					this.AddFeedbackRequest(request);
					m_RotateFeedback.Add(request);
				}
			}
		}

		void ShowScaleFeedback()
		{
			List<VRInputDevice.VRControl> ids;
			if (m_Controls.TryGetValue("Crawl", out ids))
			{
				foreach (var id in ids)
				{
					var request = new ProxyFeedbackRequest
					{
						control = id,
						node = node == Node.LeftHand ? Node.RightHand : Node.LeftHand,
						tooltipText = "Scale"
					};

					this.AddFeedbackRequest(request);
					m_ScaleFeedback.Add(request);
				}
			}
		}

		void ShowResetScaleFeedback()
		{
			List<VRInputDevice.VRControl> ids;
			if (m_Controls.TryGetValue("ScaleReset", out ids))
			{
				foreach (var id in ids)
				{
					var request = new ProxyFeedbackRequest
					{
						control = id,
						node = node.Value,
						tooltipText = "Reset scale"
					};

					this.AddFeedbackRequest(request);
					m_ResetScaleFeedback.Add(request);
				}
			}

			if (m_Controls.TryGetValue("WorldReset", out ids))
			{
				foreach (var id in ids)
				{
					var request = new ProxyFeedbackRequest
					{
						control = id,
						node = node.Value,
						tooltipText = "Reset position rotation and scale"
					};

					this.AddFeedbackRequest(request);
					m_ResetScaleFeedback.Add(request);
				}
			}
		}

		void ShowSpeedFeedback()
		{
			List<VRInputDevice.VRControl> ids;
			if (m_Controls.TryGetValue("Speed", out ids))
			{
				foreach (var id in ids)
				{
					var request = new ProxyFeedbackRequest
					{
						node = node.Value,
						control = id,
						tooltipText = m_Preferences.blinkMode ? "Extra distance" : "Extra speed"
					};

					this.AddFeedbackRequest(request);
					m_SpeedFeedback.Add(request);
				}
			}
		}

		void HideMainButtonFeedback()
		{
			foreach (var request in m_MainButtonFeedback)
			{
				this.RemoveFeedbackRequest(request);
			}
			m_MainButtonFeedback.Clear();
		}

		void HideCrawlFeedback()
		{
			foreach (var request in m_CrawlFeedback)
			{
				this.RemoveFeedbackRequest(request);
			}
			m_CrawlFeedback.Clear();
		}

		void HideRotateFeedback()
		{
			foreach (var request in m_RotateFeedback)
			{
				this.RemoveFeedbackRequest(request);
			}
			m_RotateFeedback.Clear();
		}

		void HideScaleFeedback()
		{
			foreach (var request in m_ScaleFeedback)
			{
				this.RemoveFeedbackRequest(request);
			}
			m_ScaleFeedback.Clear();
		}

		void HideSpeedFeedback()
		{
			foreach (var request in m_SpeedFeedback)
			{
				this.RemoveFeedbackRequest(request);
			}
			m_SpeedFeedback.Clear();
		}

		void HideResetScaleFeedback()
		{
			foreach (var request in m_ResetScaleFeedback)
			{
				this.RemoveFeedbackRequest(request);
			}
			m_ResetScaleFeedback.Clear();
		}

		public object OnSerializePreferences()
		{
			if (this.IsSharedUpdater(this))
			{
				// Share one preferences object across all instances
				foreach (var linkedObject in linkedObjects)
				{
					((LocomotionTool)linkedObject).m_Preferences = m_Preferences;
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
				foreach (var linkedObject in linkedObjects)
				{
					((LocomotionTool)linkedObject).m_Preferences = m_Preferences;
					m_BlinkToggle.isOn = m_Preferences.blinkMode;
					m_FlyToggle.isOn = !m_Preferences.blinkMode;
				}
			}
		}
	}
}
#endif
