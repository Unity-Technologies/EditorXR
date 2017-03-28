#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR.Modules
{
	[MainMenuItem("Snapping", "Settings", "Select snapping modes")]
	sealed class SnappingModule : MonoBehaviour, IUsesViewerScale, ISettingsMenuProvider
	{
		const float k_MaxRayLength = 100f;

		const float k_GroundHeight = 0f;

		const float k_ManipulatorGroundSnapMin = 0.3f;
		const float k_ManipulatorGroundSnapMax = 0.5f;
		const float k_ManipulatorSurfaceSnapBreakDist = 0.1f;

		const float k_DirectSurfaceSnapBreakDist = 0.1f;
		const float k_DirectGroundSnapMin = 0.05f;
		const float k_DirectGroundSnapMax = 0.15f;

		const float k_WidgetScale = 0.03f;

		const string k_SnappingEnabled = "EditorVR.SnappingEnabled";
		const string k_GroundSnapping = "EditorVR.GroundSnapping";
		const string k_SurfaceSnapping = "EditorVR.SufraceSnapping";
		const string k_PivotSnapping = "EditorVR.PivotSnapping";
		const string k_SnapRotation = "EditorVR.SnapRotation";
		const string k_ManipulatorSnapping = "EditorVR.ManipulatorSnapping";
		const string k_DirectSnapping = "EditorVR.DirectSnapping";

		[SerializeField]
		GameObject m_GroundPlane;

		[SerializeField]
		GameObject m_Widget;

		[SerializeField]
		GameObject m_SettingsMenuPrefab;

		SnappingModuleUI m_SnappingModuleUI;

		class SnappingState
		{
			public Vector3 currentPosition;
			public Bounds rotatedBounds;
			public Bounds identityBounds;
			public bool groundSnapping;
			public bool surfaceSnapping;
			public Vector3 surfaceSnappingStartPosition;
			public Quaternion surfaceSnappingRotation;
			public GameObject[] objects;
		}

		bool m_DisableAll;

		// Snapping Modes
		bool m_GroundSnapping;
		bool m_SurfaceSnapping;

		// Modifiers (do not require reset on value change)
		bool m_PivotSnapping;
		bool m_SnapRotation;
		bool m_ManipulatorSnapping;
		bool m_DirectSnapping;

		public RaycastDelegate raycast { private get; set; }
		public Renderer[] playerHeadObjects { private get; set; }

		readonly Dictionary<Transform, SnappingState> m_SnappingStates = new Dictionary<Transform, SnappingState>();

		// Local method use only -- created here to reduce garbage collection
		readonly List<GameObject> m_IgnoreList = new List<GameObject>();

		public GameObject settingsMenuPrefab { get { return m_SettingsMenuPrefab; } }

		public GameObject settingsMenuInstance
		{
			set
			{
				if (value == null)
				{
					m_SnappingModuleUI = null;
					return;
				}

				m_SnappingModuleUI = value.GetComponent<SnappingModuleUI>();
				SetupUI();
			}
		}

		public bool snappingEnabled
		{
			get { return !m_DisableAll && (groundSnapping || surfaceSnapping); }
			set
			{
				Reset();
				m_DisableAll = !value;

				if (m_SnappingModuleUI)
					m_SnappingModuleUI.snappingEnabled.isOn = value;
			}
		}

		public bool groundSnapping
		{
			get { return m_GroundSnapping; }
			set
			{
				if (value == m_GroundSnapping)
					return;

				Reset();
				m_GroundSnapping = value;

				if (m_SnappingModuleUI)
					m_SnappingModuleUI.groundSnapping.isOn = value;
			}
		}

		public bool surfaceSnapping
		{
			get { return m_SurfaceSnapping; }
			set
			{
				if (value == m_SurfaceSnapping)
					return;

				Reset();
				m_SurfaceSnapping = value;

				if (m_SnappingModuleUI)
					m_SnappingModuleUI.surfaceSnapping.isOn = value;
			}
		}

		public bool pivotSnapping
		{
			get { return m_PivotSnapping; }
			set
			{
				m_PivotSnapping = value;

				if (m_SnappingModuleUI)
					m_SnappingModuleUI.pivotSnapping.isOn = value;
			}
		}

		public bool snapRotation
		{
			get { return m_SnapRotation; }
			set
			{
				m_SnapRotation = value;

				if (m_SnappingModuleUI)
					m_SnappingModuleUI.snapRotation.isOn = value;
			}
		}

		public bool manipulatorSnapping
		{
			get { return m_ManipulatorSnapping; }
			set
			{
				m_ManipulatorSnapping = value;

				if (m_SnappingModuleUI)
					m_SnappingModuleUI.manipulatorSnapping.isOn = value;
			}
		}

		public bool directSnapping
		{
			get
			{
				return m_DirectSnapping;
			}
			set
			{
				m_DirectSnapping = value;

				if (m_SnappingModuleUI)
					m_SnappingModuleUI.directSnapping.isOn = value;
			}
		}

		void Awake()
		{
			m_GroundPlane = ObjectUtils.Instantiate(m_GroundPlane, transform);
			m_GroundPlane.SetActive(false);

			m_Widget = ObjectUtils.Instantiate(m_Widget, transform);
			m_Widget.SetActive(false);

			groundSnapping = true;
			surfaceSnapping = true;

			directSnapping = true;
			manipulatorSnapping = true;

			if (EditorPrefs.HasKey(k_SnappingEnabled))
				snappingEnabled = EditorPrefs.GetBool(k_SnappingEnabled);

			if (EditorPrefs.HasKey(k_GroundSnapping))
				groundSnapping = EditorPrefs.GetBool(k_GroundSnapping);

			if (EditorPrefs.HasKey(k_SurfaceSnapping))
				surfaceSnapping = EditorPrefs.GetBool(k_SurfaceSnapping);

			if (EditorPrefs.HasKey(k_PivotSnapping))
				pivotSnapping = EditorPrefs.GetBool(k_PivotSnapping);

			if (EditorPrefs.HasKey(k_SnapRotation))
				snapRotation = EditorPrefs.GetBool(k_SnapRotation);

			if (EditorPrefs.HasKey(k_ManipulatorSnapping))
				manipulatorSnapping = EditorPrefs.GetBool(k_ManipulatorSnapping);

			if (EditorPrefs.HasKey(k_DirectSnapping))
				directSnapping = EditorPrefs.GetBool(k_DirectSnapping);
		}

		void Update()
		{
			if (snappingEnabled)
			{
				SnappingState surfaceSnapping = null;
				var shouldActivateGroundPlane = false;
				foreach (var state in m_SnappingStates.Values)
				{
					if (state.groundSnapping)
						shouldActivateGroundPlane = true;

					if (state.surfaceSnapping)
						surfaceSnapping = state;
				}
				m_GroundPlane.SetActive(shouldActivateGroundPlane);

				var shouldActivateWidget = surfaceSnapping != null;
				m_Widget.SetActive(shouldActivateWidget);
				if (shouldActivateWidget)
				{
					var statePosition = surfaceSnapping.surfaceSnappingStartPosition;
					var camera = CameraUtils.GetMainCamera();
					var distToCamera = Vector3.Distance(camera.transform.position, statePosition);
					m_Widget.transform.position = statePosition;
					m_Widget.transform.rotation = surfaceSnapping.surfaceSnappingRotation;
					m_Widget.transform.localScale = Vector3.one * k_WidgetScale * distToCamera;
				}
			}
			else
			{
				m_GroundPlane.SetActive(false);
				m_Widget.SetActive(false);
			}
		}

		public bool ManipulatorSnapping(Transform rayOrigin, GameObject[] objects, ref Vector3 position, ref Quaternion rotation, Vector3 delta, bool constrained)
		{
			if (snappingEnabled && manipulatorSnapping)
			{
				var state = GetSnappingState(rayOrigin, objects, position, rotation);

				state.currentPosition += delta;
				var statePosition = state.currentPosition;

				var camera = CameraUtils.GetMainCamera();
				var breakScale = Vector3.Distance(camera.transform.position, statePosition);

				if (surfaceSnapping && !constrained)
				{
					var ray = new Ray(rayOrigin.position, rayOrigin.forward);
					if (PerformSurfaceSnapping(ray, ref position, ref rotation, statePosition, state, 0, rotation, breakScale * k_ManipulatorSurfaceSnapBreakDist, false))
						return true;
				}

				if (groundSnapping && PerformGroundSnapping(ref position, ref rotation, statePosition, state, breakScale * k_ManipulatorGroundSnapMin, breakScale * k_ManipulatorGroundSnapMax))
					return true;
			}

			position += delta;

			return false;
		}

		public bool DirectSnapping(Transform rayOrigin, GameObject[] objects, ref Vector3 position, ref Quaternion rotation, Vector3 targetPosition, Quaternion targetRotation)
		{
			if (snappingEnabled && directSnapping)
			{
				var state = GetSnappingState(rayOrigin, objects, position, rotation);

				state.currentPosition = targetPosition;

				var viewerScale = this.GetViewerScale();
				var breakScale = viewerScale;

				if (surfaceSnapping)
				{
					var bounds = state.identityBounds;
					var offset = bounds.center;
					for(int i = 0; i < 6; i++)
					{
						var ray = new Ray(targetPosition + targetRotation * offset, targetRotation * GetDirection(i));
						var raycastDistance = state.identityBounds.extents.y;
						if (PerformSurfaceSnapping(ray, ref position, ref rotation, targetPosition, state, i, targetRotation, breakScale * k_DirectSurfaceSnapBreakDist, true, raycastDistance))
							return true;
					}
				}

				if (groundSnapping && PerformGroundSnapping(ref position, ref rotation, targetPosition, state, breakScale * k_DirectGroundSnapMin, breakScale * k_DirectGroundSnapMax))
					return true;
			}

			position = targetPosition;
			rotation = targetRotation;

			return false;
		}

		static Vector3 GetDirection(int i)
		{
			switch (i)
			{
				default:
					return Vector3.down;
				case 1:
					return Vector3.left;
				case 2:
					return Vector3.forward;
				case 3:
					return Vector3.right;
				case 4:
					return Vector3.back;
				case 5:
					return Vector3.up;
			}
		}

		bool PerformSurfaceSnapping(Ray ray, ref Vector3 position, ref Quaternion rotation, Vector3 statePosition, SnappingState state, int direction, Quaternion targetRotation, float breakDistance, bool localOnly, float raycastDistance = k_MaxRayLength)
		{
			m_IgnoreList.Clear();
			var objects = state.objects;
			for (int i = 0; i < objects.Length; i++)
			{
				m_IgnoreList.Add(objects[i]);
			}

			for (int i = 0; i < playerHeadObjects.Length; i++)
			{
				m_IgnoreList.Add(playerHeadObjects[i].gameObject);
			}

			RaycastHit hit;
			GameObject go;
			if (raycast(ray, out hit, out go, raycastDistance, m_IgnoreList))
			{
				var snappedRotation = Quaternion.LookRotation(hit.normal) * Quaternion.AngleAxis(90, Vector3.right);

				var hitPoint = hit.point;
				var bounds = state.identityBounds;
				var directionVector = GetDirection(direction);
				switch (direction)
				{
					default:
						directionVector *= bounds.center.y - bounds.extents.y;
						break;
					case 1:
						directionVector *= bounds.center.x - bounds.extents.x;
						break;
					case 2:
						directionVector *= bounds.center.z - bounds.extents.z;
						break;
					case 3:
						directionVector *= bounds.extents.x - bounds.center.x;
						break;
					case 4:
						directionVector *= bounds.extents.z - bounds.center.z;
						break;
					case 5:
						directionVector *= bounds.extents.y - bounds.center.y;
						break;
				}
				var snappedPosition = pivotSnapping ? hitPoint : hitPoint + rotation * directionVector;

				if (localOnly && Vector3.Distance(snappedPosition, statePosition) > breakDistance)
					return false;

				state.surfaceSnapping = true;
				state.groundSnapping = false;

				position = snappedPosition;
				rotation = snapRotation ? snappedRotation : targetRotation;

				state.surfaceSnappingStartPosition = position;
				state.surfaceSnappingRotation = snappedRotation;
				return true;
			}

			if (state.surfaceSnapping)
			{
				if (Vector3.Distance(state.surfaceSnappingStartPosition, statePosition) > breakDistance)
				{
					position = statePosition;
					state.surfaceSnapping = false;
				}

				return true;
			}

			return false;
		}


		bool PerformGroundSnapping(ref Vector3 position, ref Quaternion rotation, Vector3 statePosition, SnappingState state, float groundSnapMin, float groundSnapMax)
		{
			if(groundSnapping)
			{
				var diffGround = Mathf.Abs(statePosition.y - k_GroundHeight);

				var bounds = state.rotatedBounds;
				var offset = bounds.center.y - bounds.extents.y;

				if (!pivotSnapping)
					diffGround = Mathf.Abs(statePosition.y + offset - k_GroundHeight);

				if (diffGround < groundSnapMin)
					state.groundSnapping = true;

				if (diffGround > groundSnapMax)
					state.groundSnapping = false;

				if (state.groundSnapping)
				{
					if (pivotSnapping)
						statePosition.y = k_GroundHeight;
					else
						statePosition.y = k_GroundHeight - offset;

					position = statePosition;
					
					if (snapRotation)
						rotation = Quaternion.identity;

					return true;
				}
			}

			return false;
		}

		SnappingState GetSnappingState(Transform rayOrigin, GameObject[] objects, Vector3 position, Quaternion rotation)
		{
			SnappingState state;
			if (!m_SnappingStates.TryGetValue(rayOrigin, out state))
			{
				float angle;
				Vector3 axis;
				rotation.ToAngleAxis(out angle, out axis);
				foreach (var go in objects)
				{
					go.transform.RotateAround(position, axis, -angle);
				}

				var identityBounds = ObjectUtils.GetBounds(objects);

				foreach (var go in objects)
				{
					go.transform.RotateAround(position, axis, angle);
				}

				var totalBounds = ObjectUtils.GetBounds(objects);
				totalBounds.center -= position;
				identityBounds.center -= position;
				state = new SnappingState
				{
					currentPosition = position,
					rotatedBounds = totalBounds,
					identityBounds = identityBounds,
					objects = objects
				};
				m_SnappingStates[rayOrigin] = state;
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
			var snappingEnabledUI = m_SnappingModuleUI.snappingEnabled;
			m_SnappingModuleUI.SetToggleValue(snappingEnabledUI, !m_DisableAll);
			snappingEnabledUI.onValueChanged.AddListener(b => { OnTogglePressed(snappingEnabledUI); });

			var groundSnappingUI = m_SnappingModuleUI.groundSnapping;
			m_SnappingModuleUI.SetToggleValue(groundSnappingUI, m_GroundSnapping);
			groundSnappingUI.onValueChanged.AddListener(b => { OnTogglePressed(groundSnappingUI); });

			var surfaceSnappingUI = m_SnappingModuleUI.surfaceSnapping;
			m_SnappingModuleUI.SetToggleValue(surfaceSnappingUI, m_SurfaceSnapping);
			surfaceSnappingUI.onValueChanged.AddListener(b => { OnTogglePressed(surfaceSnappingUI); });

			var pivotSnappingUI = m_SnappingModuleUI.pivotSnapping;
			m_SnappingModuleUI.SetToggleValue(pivotSnappingUI, m_PivotSnapping);
			pivotSnappingUI.onValueChanged.AddListener(b => { OnTogglePressed(pivotSnappingUI); });

			var snapRotationUI = m_SnappingModuleUI.snapRotation;
			m_SnappingModuleUI.SetToggleValue(snapRotationUI, m_SnapRotation);
			snapRotationUI.onValueChanged.AddListener(b => { OnTogglePressed(snapRotationUI); });

			var manipulatorSnappingUI = m_SnappingModuleUI.manipulatorSnapping;
			m_SnappingModuleUI.SetToggleValue(manipulatorSnappingUI, m_ManipulatorSnapping);
			manipulatorSnappingUI.onValueChanged.AddListener(b => { OnTogglePressed(manipulatorSnappingUI); });

			var directSnappingUI = m_SnappingModuleUI.directSnapping;
			m_SnappingModuleUI.SetToggleValue(directSnappingUI, m_DirectSnapping);
			directSnappingUI.onValueChanged.AddListener(b => { OnTogglePressed(directSnappingUI); });
		}

		void OnTogglePressed(Toggle toggle)
		{
			var snappingEnabledUI = m_SnappingModuleUI.snappingEnabled;
			if (toggle == snappingEnabledUI)
			{
				m_DisableAll = !snappingEnabledUI.isOn;
				Reset();
			}

			var groundSnappingUI = m_SnappingModuleUI.groundSnapping;
			if (toggle == groundSnappingUI)
			{
				m_GroundSnapping = groundSnappingUI.isOn;
				Reset();
			}

			var surfaceSnappingUI = m_SnappingModuleUI.surfaceSnapping;
			if (toggle == surfaceSnappingUI)
			{
				m_SurfaceSnapping = surfaceSnappingUI.isOn;
				Reset();
			}

			var pivotSnappingUI = m_SnappingModuleUI.pivotSnapping;
			if (toggle == pivotSnappingUI)
				m_PivotSnapping = pivotSnappingUI.isOn;

			var snapRotationUI = m_SnappingModuleUI.snapRotation;
			if (toggle == snapRotationUI)
				m_SnapRotation = snapRotationUI.isOn;

			var manipulatorSnappingUI = m_SnappingModuleUI.manipulatorSnapping;
			if (toggle == manipulatorSnappingUI)
				m_ManipulatorSnapping = manipulatorSnappingUI.isOn;

			var directSnappingUI = m_SnappingModuleUI.directSnapping;
			if (toggle == directSnappingUI)
				m_DirectSnapping = directSnappingUI.isOn;
		}

		void OnDisable()
		{
			EditorPrefs.SetBool(k_SnappingEnabled, snappingEnabled);
			EditorPrefs.SetBool(k_GroundSnapping, groundSnapping);
			EditorPrefs.SetBool(k_SurfaceSnapping, surfaceSnapping);
			EditorPrefs.SetBool(k_PivotSnapping, pivotSnapping);
			EditorPrefs.SetBool(k_SnapRotation, snapRotation);
			EditorPrefs.SetBool(k_ManipulatorSnapping, manipulatorSnapping);
			EditorPrefs.SetBool(k_DirectSnapping, directSnapping);
		}
	}
}
#endif