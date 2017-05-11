#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Handles;
using UnityEditor.Experimental.EditorVR.Helpers;
using UnityEditor.Experimental.EditorVR.Menus;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR.Modules
{
	[MainMenuItem("Snapping", "Settings", "Select snapping modes")]
	sealed class SnappingModule : MonoBehaviour, IUsesViewerScale, ISettingsMenuProvider, ISerializePreferences
	{
		public delegate bool RaycastDelegate(Ray ray, out RaycastHit hit, out GameObject go, float maxDistance = Mathf.Infinity, List<GameObject> ignoreList = null);

		const float k_GroundSnappingMaxRayLength = 25f;
		const float k_SurfaceSnappingMaxRayLength = 100f;

		const float k_GroundHeight = 0f;

		const float k_ManipulatorGroundSnapMin = 0.05f;
		const float k_ManipulatorGroundSnapMax = 0.15f;
		const float k_ManipulatorSurfaceSnapBreakDist = 0.1f;

		const float k_DirectSurfaceSearchScale = 1.1f;
		const float k_DirectSurfaceSnapBreakDist = 0.03f;
		const float k_DirectGroundSnapMin = 0.03f;
		const float k_DirectGroundSnapMax = 0.07f;

		const float k_WidgetScale = 0.03f;

		const string k_MaterialColorLeftProperty = "_ColorLeft";
		const string k_MaterialColorRightProperty = "_ColorRight";

		[SerializeField]
		GameObject m_GroundPlane;

		[SerializeField]
		GameObject m_Widget;

		[SerializeField]
		GameObject m_SettingsMenuPrefab;

		[SerializeField]
		Material m_ButtonHighlightMaterial;

		class SnappingState
		{
			public Vector3 currentPosition { get; set; }
			public bool groundSnapping { get; set; }
			public bool surfaceSnapping { get; set; }

			public Quaternion startRotation { get; private set; }
			public Bounds identityBounds { get; private set; }
			public Bounds rotatedBounds { get; private set; }

			public SnappingState(Transform[] transforms, Vector3 position, Quaternion rotation)
			{
				currentPosition = position;
				startRotation = rotation;

				Bounds rotatedBounds;
				Bounds identityBounds;

				if (transforms.Length == 1)
				{
					var transform = transforms[0];
					var objRotation = transform.rotation;

					rotatedBounds = ObjectUtils.GetBounds(transform);
					transform.rotation = Quaternion.identity;
					identityBounds = ObjectUtils.GetBounds(transform);
					transform.rotation = objRotation;
				}
				else
				{
					rotatedBounds = ObjectUtils.GetBounds(transforms);

					float angle;
					Vector3 axis;
					rotation.ToAngleAxis(out angle, out axis);
					foreach (var transform in transforms)
					{
						transform.transform.RotateAround(position, axis, -angle);
					}

					identityBounds = ObjectUtils.GetBounds(transforms);

					foreach (var transform in transforms)
					{
						transform.transform.RotateAround(position, axis, angle);
					}
				}

				rotatedBounds.center -= position;
				this.rotatedBounds = rotatedBounds;
				identityBounds.center -= position;
				this.identityBounds = identityBounds;
			}
		}

		struct SnappingDirection
		{
			public Vector3 direction;
			public Vector3 upVector;
			public Quaternion rotationOffset;
		}

		static readonly SnappingDirection[] k_Directions =
		{
			new SnappingDirection
			{
				direction = Vector3.down,
				upVector = Vector3.back,
				rotationOffset = Quaternion.AngleAxis(90, Vector3.right)
			},
			new SnappingDirection
			{
				direction = Vector3.left,
				upVector = Vector3.up,
				rotationOffset = Quaternion.AngleAxis(90, Vector3.down)
			},
			new SnappingDirection
			{
				direction = Vector3.back,
				upVector = Vector3.up,
				rotationOffset = Quaternion.identity
			},
			new SnappingDirection
			{
				direction = Vector3.right,
				upVector = Vector3.up,
				rotationOffset = Quaternion.AngleAxis(90, Vector3.up)
			},
			new SnappingDirection
			{
				direction = Vector3.forward,
				upVector = Vector3.up,
				rotationOffset = Quaternion.AngleAxis(180, Vector3.up)
			},
			new SnappingDirection
			{
				direction = Vector3.up,
				upVector = Vector3.forward,
				rotationOffset = Quaternion.AngleAxis(90, Vector3.left)
			}
		};

		[Serializable]
		class Preferences
		{
			[SerializeField]
			bool m_DisableAll;

			// Snapping Modes
			[SerializeField]
			bool m_GroundSnappingEnabled = true;
			[SerializeField]
			bool m_SurfaceSnappingEnabled = true;

			// Modifiers (do not require reset on value change)
			[SerializeField]
			bool m_PivotSnappingEnabled;
			[SerializeField]
			bool m_RotationSnappingEnabled;
			[SerializeField]
			bool m_LocalOnly;

			// Sources
			[SerializeField]
			bool m_ManipulatorSnappingEnabled = true;
			[SerializeField]
			bool m_DirectSnappingEnabled = true;

			public bool disableAll
			{
				get { return m_DisableAll; }
				set { m_DisableAll = value; }
			}

			public bool groundSnappingEnabled
			{
				get { return m_GroundSnappingEnabled; }
				set { m_GroundSnappingEnabled = value; }
			}

			public bool surfaceSnappingEnabled
			{
				get { return m_SurfaceSnappingEnabled; }
				set { m_SurfaceSnappingEnabled = value; }
			}

			public bool pivotSnappingEnabled
			{
				get { return m_PivotSnappingEnabled; }
				set { m_PivotSnappingEnabled = value; }
			}

			public bool rotationSnappingEnabled
			{
				get { return m_RotationSnappingEnabled; }
				set { m_RotationSnappingEnabled = value; }
			}

			public bool localOnly
			{
				get { return m_LocalOnly; }
				set { m_LocalOnly = value; }
			}

			public bool manipulatorSnappingEnabled
			{
				get { return m_ManipulatorSnappingEnabled; }
				set { m_ManipulatorSnappingEnabled = value; }
			}

			public bool directSnappingEnabled
			{
				get { return m_DirectSnappingEnabled; }
				set { m_DirectSnappingEnabled = value; }
			}
		}

		Preferences m_Preferences = new Preferences();

		SnappingModuleSettingsUI m_SnappingModuleSettingsUI;
		Material m_ButtonHighlightMaterialClone;

		readonly Dictionary<Transform, Dictionary<Transform, SnappingState>> m_SnappingStates = new Dictionary<Transform, Dictionary<Transform, SnappingState>>();
		Vector3 m_CurrentSurfaceSnappingHit;
		Vector3 m_CurrentSurfaceSnappingPosition;
		Quaternion m_CurrentSurfaceSnappingRotation;

		public bool widgetEnabled { get; set; }

		public RaycastDelegate raycast { private get; set; }
		public Renderer[] ignoreList { private get; set; }

		public GameObject settingsMenuPrefab { get { return m_SettingsMenuPrefab; } }

		public GameObject settingsMenuInstance
		{
			set
			{
				if (value == null)
				{
					m_SnappingModuleSettingsUI = null;
					return;
				}

				m_SnappingModuleSettingsUI = value.GetComponent<SnappingModuleSettingsUI>();
				SetupUI();
			}
		}

		public bool snappingEnabled
		{
			get { return !m_Preferences.disableAll && (groundSnappingEnabled || surfaceSnappingEnabled); }
			set
			{
				Reset();
				m_Preferences.disableAll = !value;

				if (m_SnappingModuleSettingsUI)
					m_SnappingModuleSettingsUI.snappingEnabled.isOn = value;
			}
		}

		public bool groundSnappingEnabled
		{
			get { return m_Preferences.groundSnappingEnabled; }
			set
			{
				if (value == m_Preferences.groundSnappingEnabled)
					return;

				Reset();
				m_Preferences.groundSnappingEnabled = value;

				if (m_SnappingModuleSettingsUI)
					m_SnappingModuleSettingsUI.groundSnappingEnabled.isOn = value;
			}
		}

		public bool surfaceSnappingEnabled
		{
			get { return m_Preferences.surfaceSnappingEnabled; }
			set
			{
				if (value == m_Preferences.surfaceSnappingEnabled)
					return;

				Reset();
				m_Preferences.surfaceSnappingEnabled = value;

				if (m_SnappingModuleSettingsUI)
					m_SnappingModuleSettingsUI.surfaceSnappingEnabled.isOn = value;
			}
		}

		public bool pivotSnappingEnabled
		{
			get { return m_Preferences.pivotSnappingEnabled; }
			set
			{
				m_Preferences.pivotSnappingEnabled = value;

				if (m_SnappingModuleSettingsUI)
					m_SnappingModuleSettingsUI.pivotSnappingEnabled.isOn = value;
			}
		}

		public bool rotationSnappingEnabled
		{
			get { return m_Preferences.rotationSnappingEnabled; }
			set
			{
				m_Preferences.rotationSnappingEnabled = value;

				if (m_SnappingModuleSettingsUI)
					m_SnappingModuleSettingsUI.rotationSnappingEnabled.isOn = value;
			}
		}

		public bool localOnly
		{
			get { return m_Preferences.localOnly; }
			set
			{
				m_Preferences.localOnly = value;

				if (m_SnappingModuleSettingsUI)
					m_SnappingModuleSettingsUI.localOnly.isOn = value;
			}
		}

		public bool manipulatorSnappingEnabled
		{
			get { return m_Preferences.manipulatorSnappingEnabled; }
			set
			{
				m_Preferences.manipulatorSnappingEnabled = value;

				if (m_SnappingModuleSettingsUI)
					m_SnappingModuleSettingsUI.manipulatorSnappingEnabled.isOn = value;
			}
		}

		public bool directSnappingEnabled
		{
			get
			{
				return m_Preferences.directSnappingEnabled;
			}
			set
			{
				m_Preferences.directSnappingEnabled = value;

				if (m_SnappingModuleSettingsUI)
					m_SnappingModuleSettingsUI.directSnappingEnabled.isOn = value;
			}
		}

		// Local method use only -- created here to reduce garbage collection
		readonly List<GameObject> m_CombinedIgnoreList = new List<GameObject>();
		Transform[] m_SingleTransformArray = new Transform[1];

		void Awake()
		{
			m_GroundPlane = ObjectUtils.Instantiate(m_GroundPlane, transform);
			m_GroundPlane.SetActive(false);

			m_Widget = ObjectUtils.Instantiate(m_Widget, transform);
			m_Widget.SetActive(false);

			m_ButtonHighlightMaterialClone = Instantiate(m_ButtonHighlightMaterial);
		}

		public object OnSerializePreferences()
		{
			return m_Preferences;
		}

		public void OnDeserializePreferences(object obj)
		{
			m_Preferences = (Preferences)obj;
		}

		void Update()
		{
			if (snappingEnabled)
			{
				SnappingState surfaceSnapping = null;
				var shouldActivateGroundPlane = false;
				foreach (var statesForRay in m_SnappingStates.Values)
				{
					foreach (var state in statesForRay.Values)
					{
						if (state.groundSnapping)
							shouldActivateGroundPlane = true;

						if (state.surfaceSnapping)
							surfaceSnapping = state;
					}
				}
				m_GroundPlane.SetActive(shouldActivateGroundPlane);

				if (widgetEnabled)
				{
					var shouldActivateWidget = surfaceSnapping != null;
					m_Widget.SetActive(shouldActivateWidget);
					if (shouldActivateWidget)
					{
						var camera = CameraUtils.GetMainCamera();
						var distanceToCamera = Vector3.Distance(camera.transform.position, m_CurrentSurfaceSnappingPosition);
						m_Widget.transform.position = m_CurrentSurfaceSnappingHit;
						m_Widget.transform.rotation = m_CurrentSurfaceSnappingRotation;
						m_Widget.transform.localScale = Vector3.one * k_WidgetScale * distanceToCamera;
					}
				}
			}
			else
			{
				m_GroundPlane.SetActive(false);
				m_Widget.SetActive(false);
			}
		}

		public bool ManipulatorSnap(Transform rayOrigin, Transform[] transforms, ref Vector3 position, ref Quaternion rotation, Vector3 delta)
		{
			if (transforms.Length == 0)
				return false;

			if (snappingEnabled && manipulatorSnappingEnabled)
			{
				var state = GetSnappingState(rayOrigin, transforms, position, rotation);

				state.currentPosition += delta;
				var targetPosition = state.currentPosition;
				var targetRotation = state.startRotation;

				var camera = CameraUtils.GetMainCamera();
				var breakScale = Vector3.Distance(camera.transform.position, targetPosition);

				AddToIgnoreList(transforms);
				if (surfaceSnappingEnabled && ManipulatorSnapToSurface(rayOrigin, ref position, ref rotation, targetPosition, state, targetRotation, breakScale * k_ManipulatorSurfaceSnapBreakDist))
					return true;

				if (localOnly)
				{
					if (groundSnappingEnabled && SnapToGround(ref position, ref rotation, targetPosition, targetRotation, state, breakScale * k_ManipulatorGroundSnapMin, breakScale * k_ManipulatorGroundSnapMax))
						return true;
				}
				else
				{
					var groundPlane = new Plane(Vector3.up, k_GroundHeight);
					var origin = rayOrigin.position;
					var direction = rayOrigin.forward;
					var pointerRay = new Ray(origin, direction);
					var raycastDistance = k_GroundSnappingMaxRayLength * this.GetViewerScale();
					float distance;
					if (groundPlane.Raycast(pointerRay, out distance) && distance <= raycastDistance)
					{
						state.groundSnapping = true;

						position = origin + direction * distance;

						if (rotationSnappingEnabled)
							rotation = Quaternion.LookRotation(Vector3.up, targetRotation * Vector3.back) * Quaternion.AngleAxis(90, Vector3.right);

						return true;
					}

					state.groundSnapping = false;
					position = targetPosition;
					rotation = targetRotation;
				}
			}

			position += delta;

			return false;
		}

		public bool DirectSnap(Transform rayOrigin, Transform transform, ref Vector3 position, ref Quaternion rotation, Vector3 targetPosition, Quaternion targetRotation)
		{
			if (snappingEnabled && directSnappingEnabled)
			{
				var state = GetSnappingState(rayOrigin, transform, position, rotation);

				state.currentPosition = targetPosition;

				var viewerScale = this.GetViewerScale();
				var breakScale = viewerScale;
				var breakDistance = breakScale * k_DirectSurfaceSnapBreakDist;

				AddToIgnoreList(transform);
				if (surfaceSnappingEnabled && DirectSnapToSurface(ref position, ref rotation, targetPosition, state, targetRotation, breakDistance))
					return true;

				if (groundSnappingEnabled && SnapToGround(ref position, ref rotation, targetPosition, targetRotation, state, breakScale * k_DirectGroundSnapMin, breakScale * k_DirectGroundSnapMax))
					return true;
			}

			position = targetPosition;
			rotation = targetRotation;

			return false;
		}

		bool ManipulatorSnapToSurface(Transform rayOrigin, ref Vector3 position, ref Quaternion rotation, Vector3 targetPosition, SnappingState state, Quaternion targetRotation, float breakDistance)
		{
			var bounds = state.identityBounds;
			var boundsExtents = bounds.extents;
			var projectedExtents = Vector3.Project(boundsExtents, Vector3.down);
			var offset = projectedExtents - bounds.center;
			var rotationOffset = Quaternion.AngleAxis(90, Vector3.right);
			var startRotation = state.startRotation;
			var upVector = startRotation * Vector3.back;
			var maxRayLength = k_SurfaceSnappingMaxRayLength * this.GetViewerScale();

			var pointerRay = new Ray(rayOrigin.position, rayOrigin.forward);
			return SnapToSurface(pointerRay, ref position, ref rotation, state, offset, targetPosition, targetRotation , rotationOffset, upVector, breakDistance, maxRayLength)
				|| TryBreakSurfaceSnap(ref position, ref rotation, targetPosition, startRotation, state, breakDistance);
		}

		bool DirectSnapToSurface(ref Vector3 position, ref Quaternion rotation, Vector3 targetPosition, SnappingState state, Quaternion targetRotation, float breakDistance)
		{
			var bounds = state.identityBounds;
			var boundsCenter = bounds.center;
			for (int i = 0; i < k_Directions.Length; i++)
			{
				var direction = k_Directions[i];
				var upVector = targetRotation * direction.upVector;
				var directionVector = direction.direction;
				var rotationOffset = direction.rotationOffset;
				var boundsRay = new Ray(targetPosition + targetRotation * boundsCenter, targetRotation * directionVector);

				var boundsExtents = bounds.extents;
				var projectedExtents = Vector3.Project(boundsExtents, directionVector);
				var raycastDistance = projectedExtents.magnitude * k_DirectSurfaceSearchScale;
				var offset = -boundsCenter;
				if (i > 2)
					offset -= projectedExtents;
				else
					offset += projectedExtents;

				if (SnapToSurface(boundsRay, ref position, ref rotation, state, offset, targetPosition, targetRotation, rotationOffset, upVector, breakDistance, raycastDistance))
					return true;
			}

			if (TryBreakSurfaceSnap(ref position, ref rotation, targetPosition, targetRotation, state, breakDistance))
				return true;

			return false;
		}

		static bool TryBreakSurfaceSnap(ref Vector3 position, ref Quaternion rotation, Vector3 targetPosition, Quaternion targetRotation, SnappingState state, float breakDistance)
		{
			if (state.surfaceSnapping)
			{
				if (Vector3.Distance(position, targetPosition) > breakDistance)
				{
					position = targetPosition;
					rotation = targetRotation;
					state.surfaceSnapping = false;
				}

				return true;
			}
			return false;
		}

		void AddToIgnoreList(Transform transform)
		{
			m_SingleTransformArray[0] = transform;
			AddToIgnoreList(m_SingleTransformArray);
		}

		void AddToIgnoreList(Transform[] transforms)
		{
			m_CombinedIgnoreList.Clear();

			for (int i = 0; i < transforms.Length; i++)
			{
				var renderers = transforms[i].GetComponentsInChildren<Renderer>();
				for (var j = 0; j < renderers.Length; j++)
				{
					m_CombinedIgnoreList.Add(renderers[j].gameObject);
				}
			}

			for (int i = 0; i < ignoreList.Length; i++)
			{
				m_CombinedIgnoreList.Add(ignoreList[i].gameObject);
			}
		}

		bool SnapToSurface(Ray ray, ref Vector3 position, ref Quaternion rotation, SnappingState state, Vector3 boundsOffset, Vector3 targetPosition, Quaternion targetRotation, Quaternion rotationOffset, Vector3 upVector, float breakDistance, float raycastDistance)
		{
			RaycastHit hit;
			GameObject go;
			if (raycast(ray, out hit, out go, raycastDistance, m_CombinedIgnoreList))
			{
				var snappedRotation = Quaternion.LookRotation(hit.normal, upVector) * rotationOffset;

				var hitPoint = hit.point;
				m_CurrentSurfaceSnappingHit = hitPoint;
				var snappedPosition = pivotSnappingEnabled ? hitPoint : hitPoint + rotation * boundsOffset;

				if (localOnly && Vector3.Distance(snappedPosition, targetPosition) > breakDistance)
					return false;

				state.surfaceSnapping = true;
				state.groundSnapping = false;

				position = snappedPosition;
				rotation = rotationSnappingEnabled ? snappedRotation : targetRotation;

				m_CurrentSurfaceSnappingPosition = position;
				m_CurrentSurfaceSnappingRotation = snappedRotation;
				return true;
			}

			return false;
		}


		bool SnapToGround(ref Vector3 position, ref Quaternion rotation, Vector3 targetPosition, Quaternion targetRotation, SnappingState state, float groundSnapMin, float groundSnapMax)
		{
			if (groundSnappingEnabled)
			{
				var diffGround = Mathf.Abs(targetPosition.y - k_GroundHeight);

				var bounds = state.rotatedBounds;
				if (rotationSnappingEnabled)
					bounds = state.identityBounds;

				var offset = bounds.center.y - bounds.extents.y;

				if (!pivotSnappingEnabled)
					diffGround = Mathf.Abs(targetPosition.y + offset - k_GroundHeight);

				if (diffGround < groundSnapMin)
					state.groundSnapping = true;

				if (diffGround > groundSnapMax)
				{
					state.groundSnapping = false;
					position = targetPosition;
					rotation = targetRotation;
				}

				if (state.groundSnapping)
				{
					if (pivotSnappingEnabled)
						targetPosition.y = k_GroundHeight;
					else
						targetPosition.y = k_GroundHeight - offset;

					position = targetPosition;

					if (rotationSnappingEnabled)
						rotation = Quaternion.LookRotation(Vector3.up, targetRotation * Vector3.back) * Quaternion.AngleAxis(90, Vector3.right);

					return true;
				}
			}

			return false;
		}

		SnappingState GetSnappingState(Transform rayOrigin, Transform transform, Vector3 position, Quaternion rotation)
		{
			m_SingleTransformArray[0] = transform;
			return GetSnappingState(rayOrigin, m_SingleTransformArray, position, rotation);
		}

		SnappingState GetSnappingState(Transform rayOrigin, Transform[] transforms, Vector3 position, Quaternion rotation)
		{
			Dictionary<Transform, SnappingState> states;
			if (!m_SnappingStates.TryGetValue(rayOrigin, out states))
			{
				states = new Dictionary<Transform, SnappingState>();
				m_SnappingStates[rayOrigin] = states;
			}

			var firstObject = transforms[0];
			SnappingState state;
			if (!states.TryGetValue(firstObject, out state))
			{
				state = new SnappingState(transforms, position, rotation);
				states[firstObject] = state;
			}
			return state;
		}

		public void ClearSnappingState(Transform rayOrigin)
		{
			m_SnappingStates.Remove(rayOrigin);
		}

		void Reset()
		{
			m_SnappingStates.Clear();
		}

		void SetupUI()
		{
			var snappingEnabledUI = m_SnappingModuleSettingsUI.snappingEnabled;
			var text = snappingEnabledUI.GetComponentInChildren<Text>();
			snappingEnabledUI.isOn = !m_Preferences.disableAll;
			snappingEnabledUI.onValueChanged.AddListener(b =>
			{
				m_Preferences.disableAll = !snappingEnabledUI.isOn;
				text.text = m_Preferences.disableAll ? "Snapping disabled" : "Snapping enabled";
				Reset();
				SetDependentTogglesGhosted();
			});

			var handle = snappingEnabledUI.GetComponent<BaseHandle>();
			handle.hoverStarted += (baseHandle, data) => { text.text = m_Preferences.disableAll ? "Enable Snapping" : "Disable snapping"; };
			handle.hoverEnded += (baseHandle, data) => { text.text = m_Preferences.disableAll ? "Snapping disabled" : "Snapping enabled"; };

			var groundSnappingUI = m_SnappingModuleSettingsUI.groundSnappingEnabled;
			groundSnappingUI.isOn = m_Preferences.groundSnappingEnabled;
			groundSnappingUI.onValueChanged.AddListener(b =>
			{
				m_Preferences.groundSnappingEnabled = groundSnappingUI.isOn;
				Reset();
			});

			var surfaceSnappingUI = m_SnappingModuleSettingsUI.surfaceSnappingEnabled;
			surfaceSnappingUI.isOn = m_Preferences.surfaceSnappingEnabled;
			surfaceSnappingUI.onValueChanged.AddListener(b =>
			{
				m_Preferences.surfaceSnappingEnabled = surfaceSnappingUI.isOn;
				Reset();
			});

			var pivotSnappingUI = m_SnappingModuleSettingsUI.pivotSnappingEnabled;
			m_SnappingModuleSettingsUI.SetToggleValue(pivotSnappingUI, m_Preferences.pivotSnappingEnabled);
			pivotSnappingUI.onValueChanged.AddListener(b => { m_Preferences.pivotSnappingEnabled = pivotSnappingUI.isOn; });

			var snapRotationUI = m_SnappingModuleSettingsUI.rotationSnappingEnabled;
			snapRotationUI.isOn = m_Preferences.rotationSnappingEnabled;
			snapRotationUI.onValueChanged.AddListener(b => { m_Preferences.rotationSnappingEnabled = snapRotationUI.isOn; });

			var localOnlyUI = m_SnappingModuleSettingsUI.localOnly;
			localOnlyUI.isOn = m_Preferences.localOnly;
			localOnlyUI.onValueChanged.AddListener(b => { m_Preferences.localOnly = localOnlyUI.isOn; });

			var manipulatorSnappingUI = m_SnappingModuleSettingsUI.manipulatorSnappingEnabled;
			manipulatorSnappingUI.isOn =  m_Preferences.manipulatorSnappingEnabled;
			manipulatorSnappingUI.onValueChanged.AddListener(b => { m_Preferences.manipulatorSnappingEnabled = manipulatorSnappingUI.isOn; });

			var directSnappingUI = m_SnappingModuleSettingsUI.directSnappingEnabled;
			directSnappingUI.isOn = m_Preferences.directSnappingEnabled;
			directSnappingUI.onValueChanged.AddListener(b => { m_Preferences.directSnappingEnabled = directSnappingUI.isOn; });

			SetDependentTogglesGhosted();

			SetSessionGradientMaterial(m_SnappingModuleSettingsUI.GetComponent<SubmenuFace>().gradientPair);
		}

		void SetDependentTogglesGhosted()
		{
			var toggles = new List<Toggle>
			{
				m_SnappingModuleSettingsUI.groundSnappingEnabled,
				m_SnappingModuleSettingsUI.surfaceSnappingEnabled,
				m_SnappingModuleSettingsUI.rotationSnappingEnabled,
				m_SnappingModuleSettingsUI.localOnly,
				m_SnappingModuleSettingsUI.manipulatorSnappingEnabled,
				m_SnappingModuleSettingsUI.directSnappingEnabled
			};

			toggles.AddRange(m_SnappingModuleSettingsUI.pivotSnappingEnabled.group.GetComponentsInChildren<Toggle>(true));

			foreach (var toggle in toggles)
			{
				toggle.interactable = !m_Preferences.disableAll;
				if (toggle.isOn)
					toggle.graphic.gameObject.SetActive(!m_Preferences.disableAll);
			}

			foreach (var text in m_SnappingModuleSettingsUI.GetComponentsInChildren<Text>(true))
			{
				text.color = m_Preferences.disableAll ? Color.gray : Color.white;
			}
		}

		void SetSessionGradientMaterial(GradientPair gradientPair)
		{
			m_ButtonHighlightMaterialClone.SetColor(k_MaterialColorLeftProperty, gradientPair.a);
			m_ButtonHighlightMaterialClone.SetColor(k_MaterialColorRightProperty, gradientPair.b);
			foreach (var graphic in m_SnappingModuleSettingsUI.GetComponentsInChildren<Graphic>())
			{
				if (graphic.material == m_ButtonHighlightMaterial)
					graphic.material = m_ButtonHighlightMaterialClone;
			}
		}
	}
}
#endif