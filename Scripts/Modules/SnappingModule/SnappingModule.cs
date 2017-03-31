#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Handles;
using UnityEditor.Experimental.EditorVR.Helpers;
using UnityEditor.Experimental.EditorVR.Menus;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR.Modules
{
	[MainMenuItem("Snapping", "Settings", "Select snapping modes")]
	sealed class SnappingModule : MonoBehaviour, IUsesViewerScale, ISettingsMenuProvider
	{
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

		const string k_SnappingEnabled = "EditorVR.Snapping.Enabled";
		const string k_GroundSnapping = "EditorVR.Snapping.Ground";
		const string k_SurfaceSnapping = "EditorVR.Snapping.Sufrace";
		const string k_PivotSnapping = "EditorVR.Snapping.Pivot";
		const string k_SnapRotation = "EditorVR.Snapping.Rotation";
		const string k_LocalOnly = "EditorVR.Snapping.LocalOnly";
		const string k_ManipulatorSnapping = "EditorVR.Snapping.Manipulator";
		const string k_DirectSnapping = "EditorVR.Snapping.Direct";

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

		SnappingModuleSettingsUI m_SnappingModuleSettingsUI;
		Material m_ButtonHighlightMaterialClone;

		class SnappingState
		{
			public Vector3 currentPosition { get; set; }
			public bool groundSnapping { get; set; }
			public bool surfaceSnapping { get; set; }

			public Quaternion startRotation { get; private set; }
			public Bounds identityBounds { get; private set; }
			public Bounds rotatedBounds { get; private set; }

			public SnappingState(GameObject go, Vector3 position, Quaternion rotation)
			{
				currentPosition = position;
				startRotation = rotation;

				var objTransform = go.transform;
				var objRotation = objTransform.rotation;

				var rotatedBounds = ObjectUtils.GetBounds(go);
				go.transform.rotation = Quaternion.identity;
				var identityBounds = ObjectUtils.GetBounds(go);
				go.transform.rotation = objRotation;

				rotatedBounds.center -= position;
				this.rotatedBounds = rotatedBounds;
				identityBounds.center -= position;
				this.identityBounds = identityBounds;
			}

			public SnappingState(GameObject[] objects, Vector3 position, Quaternion rotation)
			{
				currentPosition = position;
				startRotation = rotation;

				var rotatedBounds = ObjectUtils.GetBounds(objects);

				float angle;
				Vector3 axis;
				rotation.ToAngleAxis(out angle, out axis);
				foreach (var obj in objects)
				{
					obj.transform.RotateAround(position, axis, -angle);
				}

				var identityBounds = ObjectUtils.GetBounds(objects);

				foreach (var obj in objects)
				{
					obj.transform.RotateAround(position, axis, angle);
				}

				rotatedBounds.center -= position;
				this.rotatedBounds = rotatedBounds;
				identityBounds.center -= position;
				this.identityBounds = identityBounds;
			}
		}

		struct SnappingDirection
		{
			public Vector3 direction { get; set; }
			public Vector3 upVector { get; set; }
			public Quaternion rotationOffset { get; set; }
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

		bool m_DisableAll;

		// Snapping Modes
		bool m_GroundSnappingEnabled;
		bool m_SurfaceSnappingEnabled;

		// Modifiers (do not require reset on value change)
		bool m_PivotSnapping;
		bool m_SnapRotation;
		bool m_LocalOnly;

		// Sources
		bool m_ManipulatorSnappingEnabled;
		bool m_DirectSnappingEnabled;

		public bool widgetEnabled { get; set; }

		public RaycastDelegate raycast { private get; set; }
		public Renderer[] ignoreList { private get; set; }

		readonly Dictionary<Transform, Dictionary<GameObject, SnappingState>> m_SnappingStates = new Dictionary<Transform, Dictionary<GameObject, SnappingState>>();
		Vector3 m_CurrentSurfaceSnappingHit;
		Vector3 m_CurrentSurfaceSnappingPosition;
		Quaternion m_CurrentSurfaceSnappingRotation;

		// Local method use only -- created here to reduce garbage collection
		readonly List<GameObject> m_IgnoreList = new List<GameObject>();

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
			get { return !m_DisableAll && (groundSnappingEnabled || surfaceSnappingEnabled); }
			set
			{
				Reset();
				m_DisableAll = !value;

				if (m_SnappingModuleSettingsUI)
					m_SnappingModuleSettingsUI.snappingEnabled.isOn = value;
			}
		}

		public bool groundSnappingEnabled
		{
			get { return m_GroundSnappingEnabled; }
			set
			{
				if (value == m_GroundSnappingEnabled)
					return;

				Reset();
				m_GroundSnappingEnabled = value;

				if (m_SnappingModuleSettingsUI)
					m_SnappingModuleSettingsUI.groundSnapping.isOn = value;
			}
		}

		public bool surfaceSnappingEnabled
		{
			get { return m_SurfaceSnappingEnabled; }
			set
			{
				if (value == m_SurfaceSnappingEnabled)
					return;

				Reset();
				m_SurfaceSnappingEnabled = value;

				if (m_SnappingModuleSettingsUI)
					m_SnappingModuleSettingsUI.surfaceSnapping.isOn = value;
			}
		}

		public bool pivotSnapping
		{
			get { return m_PivotSnapping; }
			set
			{
				m_PivotSnapping = value;

				if (m_SnappingModuleSettingsUI)
					m_SnappingModuleSettingsUI.pivotSnapping.isOn = value;
			}
		}

		public bool snapRotation
		{
			get { return m_SnapRotation; }
			set
			{
				m_SnapRotation = value;

				if (m_SnappingModuleSettingsUI)
					m_SnappingModuleSettingsUI.snapRotation.isOn = value;
			}
		}

		public bool localOnly
		{
			get { return m_LocalOnly; }
			set
			{
				m_LocalOnly = value;

				if (m_SnappingModuleSettingsUI)
					m_SnappingModuleSettingsUI.localOnly.isOn = value;
			}
		}

		public bool manipulatorSnappingEnabled
		{
			get { return m_ManipulatorSnappingEnabled; }
			set
			{
				m_ManipulatorSnappingEnabled = value;

				if (m_SnappingModuleSettingsUI)
					m_SnappingModuleSettingsUI.manipulatorSnapping.isOn = value;
			}
		}

		public bool directSnappingEnabled
		{
			get
			{
				return m_DirectSnappingEnabled;
			}
			set
			{
				m_DirectSnappingEnabled = value;

				if (m_SnappingModuleSettingsUI)
					m_SnappingModuleSettingsUI.directSnapping.isOn = value;
			}
		}

		void Awake()
		{
			m_GroundPlane = ObjectUtils.Instantiate(m_GroundPlane, transform);
			m_GroundPlane.SetActive(false);

			m_Widget = ObjectUtils.Instantiate(m_Widget, transform);
			m_Widget.SetActive(false);

			snappingEnabled = EditorPrefs.GetBool(k_SnappingEnabled, true);

			groundSnappingEnabled = EditorPrefs.GetBool(k_GroundSnapping, true);
			surfaceSnappingEnabled = EditorPrefs.GetBool(k_SurfaceSnapping, true);

			pivotSnapping = EditorPrefs.GetBool(k_PivotSnapping, false);
			snapRotation = EditorPrefs.GetBool(k_SnapRotation, false);
			localOnly = EditorPrefs.GetBool(k_LocalOnly, false);

			manipulatorSnappingEnabled = EditorPrefs.GetBool(k_ManipulatorSnapping, true);
			directSnappingEnabled = EditorPrefs.GetBool(k_DirectSnapping, true);

			m_ButtonHighlightMaterialClone = Instantiate(m_ButtonHighlightMaterial);
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

		public bool ManipulatorSnapping(Transform rayOrigin, GameObject[] objects, ref Vector3 position, ref Quaternion rotation, Vector3 delta)
		{
			if (objects.Length == 0)
				return false;

			if (snappingEnabled && manipulatorSnappingEnabled)
			{
				var state = GetSnappingState(rayOrigin, objects, position, rotation);

				state.currentPosition += delta;
				var targetPosition = state.currentPosition;
				var targetRotation = state.startRotation;

				var camera = CameraUtils.GetMainCamera();
				var breakScale = Vector3.Distance(camera.transform.position, targetPosition);

				SetupIgnoreList(m_IgnoreList, ignoreList, objects);
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

						if (snapRotation)
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

		public bool DirectSnapping(Transform rayOrigin, GameObject go, ref Vector3 position, ref Quaternion rotation, Vector3 targetPosition, Quaternion targetRotation)
		{
			if (snappingEnabled && directSnappingEnabled)
			{
				var state = GetSnappingState(rayOrigin, go, position, rotation);

				state.currentPosition = targetPosition;

				var viewerScale = this.GetViewerScale();
				var breakScale = viewerScale;
				var breakDistance = breakScale * k_DirectSurfaceSnapBreakDist;

				SetupIgnoreList(m_IgnoreList, ignoreList, go);
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
			return SnapToSurface(pointerRay, ref position, ref rotation, state, offset, targetPosition, targetRotation , rotationOffset, upVector, m_IgnoreList, breakDistance, maxRayLength)
				|| TryBreakSurfaceSnapping(ref position, ref rotation, targetPosition, startRotation, state, breakDistance);
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

				if (SnapToSurface(boundsRay, ref position, ref rotation, state, offset, targetPosition, targetRotation, rotationOffset, upVector, m_IgnoreList, breakDistance, raycastDistance))
					return true;
			}

			if (TryBreakSurfaceSnapping(ref position, ref rotation, targetPosition, targetRotation, state, breakDistance))
				return true;

			return false;
		}

		static bool TryBreakSurfaceSnapping(ref Vector3 position, ref Quaternion rotation, Vector3 targetPosition, Quaternion targetRotation, SnappingState state, float breakDistance)
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

		static void SetupIgnoreList(List<GameObject> ignoreList, Renderer[] moduleIgnoreList, GameObject go)
		{
			ignoreList.Clear();
			ignoreList.Add(go);

			for (int i = 0; i < moduleIgnoreList.Length; i++)
			{
				ignoreList.Add(moduleIgnoreList[i].gameObject);
			}
		}

		static void SetupIgnoreList(List<GameObject> ignoreList, Renderer[] moduleIgnoreList, GameObject[] objects)
		{
			ignoreList.Clear();

			for (int i = 0; i < objects.Length; i++)
			{
				ignoreList.Add(objects[i]);
			}

			for (int i = 0; i < moduleIgnoreList.Length; i++)
			{
				ignoreList.Add(moduleIgnoreList[i].gameObject);
			}
		}

		bool SnapToSurface(Ray ray, ref Vector3 position, ref Quaternion rotation, SnappingState state, Vector3 boundsOffset, Vector3 targetPosition, Quaternion targetRotation, Quaternion rotationOffset, Vector3 upVector, List<GameObject> ignoreList, float breakDistance, float raycastDistance)
		{
			RaycastHit hit;
			GameObject go;
			if (raycast(ray, out hit, out go, raycastDistance, ignoreList))
			{
				var snappedRotation = Quaternion.LookRotation(hit.normal, upVector) * rotationOffset;

				var hitPoint = hit.point;
				m_CurrentSurfaceSnappingHit = hitPoint;
				var snappedPosition = pivotSnapping ? hitPoint : hitPoint + rotation * boundsOffset;

				if (localOnly && Vector3.Distance(snappedPosition, targetPosition) > breakDistance)
					return false;

				state.surfaceSnapping = true;
				state.groundSnapping = false;

				position = snappedPosition;
				rotation = snapRotation ? snappedRotation : targetRotation;

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
				if (snapRotation)
					bounds = state.identityBounds;

				var offset = bounds.center.y - bounds.extents.y;

				if (!pivotSnapping)
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
					if (pivotSnapping)
						targetPosition.y = k_GroundHeight;
					else
						targetPosition.y = k_GroundHeight - offset;

					position = targetPosition;

					if (snapRotation)
						rotation = Quaternion.LookRotation(Vector3.up, targetRotation * Vector3.back) * Quaternion.AngleAxis(90, Vector3.right);

					return true;
				}
			}

			return false;
		}

		SnappingState GetSnappingState(Transform rayOrigin, GameObject go, Vector3 position, Quaternion rotation)
		{
			Dictionary<GameObject, SnappingState> states;
			if (!m_SnappingStates.TryGetValue(rayOrigin, out states))
			{
				states = new Dictionary<GameObject, SnappingState>();
				m_SnappingStates[rayOrigin] = states;
			}

			SnappingState state;
			if (!states.TryGetValue(go, out state))
			{
				state = new SnappingState(go, position, rotation);
				states[go] = state;
			}
			return state;
		}

		SnappingState GetSnappingState(Transform rayOrigin, GameObject[] objects, Vector3 position, Quaternion rotation)
		{
			Dictionary<GameObject, SnappingState> states;
			if (!m_SnappingStates.TryGetValue(rayOrigin, out states))
			{
				states = new Dictionary<GameObject, SnappingState>();
				m_SnappingStates[rayOrigin] = states;
			}

			var firstObject = objects[0];
			SnappingState state;
			if (!states.TryGetValue(firstObject, out state))
			{
				state = new SnappingState(objects, position, rotation);
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
			snappingEnabledUI.isOn = !m_DisableAll;
			snappingEnabledUI.onValueChanged.AddListener(b =>
			{
				m_DisableAll = !snappingEnabledUI.isOn;
				text.text = m_DisableAll ? "Snapping disabled" : "Snapping enabled";
				Reset();
				SetDependentTogglesGhosted();
			});

			var handle = snappingEnabledUI.GetComponent<BaseHandle>();
			handle.hoverStarted += (baseHandle, data) => { text.text = m_DisableAll ? "Enable Snapping" : "Disable snapping"; };
			handle.hoverEnded += (baseHandle, data) => { text.text = m_DisableAll ? "Snapping disabled" : "Snapping enabled"; };

			var groundSnappingUI = m_SnappingModuleSettingsUI.groundSnapping;
			groundSnappingUI.isOn = m_GroundSnappingEnabled;
			groundSnappingUI.onValueChanged.AddListener(b =>
			{
				m_GroundSnappingEnabled = groundSnappingUI.isOn;
				Reset();
			});

			var surfaceSnappingUI = m_SnappingModuleSettingsUI.surfaceSnapping;
			surfaceSnappingUI.isOn = m_SurfaceSnappingEnabled;
			surfaceSnappingUI.onValueChanged.AddListener(b =>
			{
				m_SurfaceSnappingEnabled = surfaceSnappingUI.isOn;
				Reset();
			});

			var pivotSnappingUI = m_SnappingModuleSettingsUI.pivotSnapping;
			m_SnappingModuleSettingsUI.SetToggleValue(pivotSnappingUI, m_PivotSnapping);
			pivotSnappingUI.onValueChanged.AddListener(b => { m_PivotSnapping = pivotSnappingUI.isOn; });

			var snapRotationUI = m_SnappingModuleSettingsUI.snapRotation;
			snapRotationUI.isOn = m_SnapRotation;
			snapRotationUI.onValueChanged.AddListener(b => { m_SnapRotation = snapRotationUI.isOn; });

			var localOnlyUI = m_SnappingModuleSettingsUI.localOnly;
			localOnlyUI.isOn = m_LocalOnly;
			localOnlyUI.onValueChanged.AddListener(b => { m_LocalOnly = localOnlyUI.isOn; });

			var manipulatorSnappingUI = m_SnappingModuleSettingsUI.manipulatorSnapping;
			manipulatorSnappingUI.isOn =  m_ManipulatorSnappingEnabled;
			manipulatorSnappingUI.onValueChanged.AddListener(b => { m_ManipulatorSnappingEnabled = manipulatorSnappingUI.isOn; });

			var directSnappingUI = m_SnappingModuleSettingsUI.directSnapping;
			directSnappingUI.isOn =  m_DirectSnappingEnabled;
			directSnappingUI.onValueChanged.AddListener(b => { m_DirectSnappingEnabled = directSnappingUI.isOn; });

			SetDependentTogglesGhosted();

			SetSessionGradientMaterial(m_SnappingModuleSettingsUI.GetComponent<SubmenuFace>().gradientPair);
		}

		void SetDependentTogglesGhosted()
		{
			var toggles = new List<Toggle>
			{
				m_SnappingModuleSettingsUI.groundSnapping,
				m_SnappingModuleSettingsUI.surfaceSnapping,
				m_SnappingModuleSettingsUI.snapRotation,
				m_SnappingModuleSettingsUI.localOnly,
				m_SnappingModuleSettingsUI.manipulatorSnapping,
				m_SnappingModuleSettingsUI.directSnapping
			};

			toggles.AddRange(m_SnappingModuleSettingsUI.pivotSnapping.group.GetComponentsInChildren<Toggle>(true));

			foreach (var toggle in toggles)
			{
				toggle.interactable = !m_DisableAll;
				if (toggle.isOn)
					toggle.graphic.gameObject.SetActive(!m_DisableAll);
			}

			foreach (var text in m_SnappingModuleSettingsUI.GetComponentsInChildren<Text>(true))
			{
				text.color = m_DisableAll ? Color.gray : Color.white;
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

		void OnDisable()
		{
			EditorPrefs.SetBool(k_SnappingEnabled, snappingEnabled);
			EditorPrefs.SetBool(k_GroundSnapping, groundSnappingEnabled);
			EditorPrefs.SetBool(k_SurfaceSnapping, surfaceSnappingEnabled);
			EditorPrefs.SetBool(k_PivotSnapping, pivotSnapping);
			EditorPrefs.SetBool(k_SnapRotation, snapRotation);
			EditorPrefs.SetBool(k_LocalOnly, localOnly);
			EditorPrefs.SetBool(k_ManipulatorSnapping, manipulatorSnappingEnabled);
			EditorPrefs.SetBool(k_DirectSnapping, directSnappingEnabled);
		}
	}
}
#endif