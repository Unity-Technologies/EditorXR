#if UNITY_EDITOR && UNITY_EDITORVR
using System;
using UnityEngine;
using UnityEngine.Assertions;
using System.Collections;
using UnityEditor.Experimental.EditorVR.Helpers;
using System.Reflection;
using UnityEngine.VR;
#if ENABLE_STEAMVR_INPUT
using Valve.VR;
#endif
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental.EditorVR
{
	[InitializeOnLoad]
	public class VRView : EditorWindow
	{
		const string kShowDeviceView = "VRView.ShowDeviceView";
		const string kUseCustomPreviewCamera = "VRView.UseCustomPreviewCamera";
		const string kLaunchOnExitPlaymode = "VRView.LaunchOnExitPlaymode";
		const float kHMDActivityTimeout = 3f; // in seconds

		DrawCameraMode m_RenderMode = DrawCameraMode.Textured;

		// To allow for alternate previews (e.g. smoothing)
		public static Camera customPreviewCamera
		{
			set
			{
				if (s_ActiveView)
					s_ActiveView.m_CustomPreviewCamera = value;
			}
			get
			{
				return s_ActiveView && s_ActiveView.m_UseCustomPreviewCamera ?
					s_ActiveView.m_CustomPreviewCamera : null;
			}
		}
		Camera m_CustomPreviewCamera;

		[NonSerialized]
		private Camera m_Camera;

		LayerMask? m_CullingMask;
		private RenderTexture m_SceneTargetTexture;
		private bool m_ShowDeviceView;
		private bool m_SceneViewsEnabled;

		private static VRView s_ActiveView;

		private Transform m_CameraPivot;
		private Quaternion m_LastHeadRotation = Quaternion.identity;
		private float m_TimeSinceLastHMDChange;
		private bool m_LatchHMDValues;

		bool m_HMDReady;
		bool m_VRInitialized;
		bool m_UseCustomPreviewCamera;

		public static Transform viewerPivot
		{
			get
			{
				if (s_ActiveView)
				{
					return s_ActiveView.m_CameraPivot;
				}

				return null;
			}
		}

		public static Camera viewerCamera
		{
			get
			{
				if (s_ActiveView)
				{
					return s_ActiveView.m_Camera;
				}

				return null;
			}
		}

		public static Rect rect
		{
			get
			{
				if (s_ActiveView)
				{
					return s_ActiveView.position;
				}

				return new Rect();
			}
		}

		public static VRView activeView
		{
			get
			{
				return s_ActiveView;
			}
		}

		public static bool showDeviceView
		{
			get { return s_ActiveView && s_ActiveView.m_ShowDeviceView; }
		}

		public static LayerMask cullingMask
		{
			set
			{
				if (s_ActiveView)
					s_ActiveView.m_CullingMask = value;
			}
		}

		public static event Action onEnable = delegate {};
		public static event Action onDisable = delegate {};
		public static event Action<EditorWindow> onGUIDelegate = delegate {};
		public static event Action onHMDReady = delegate {};

		public static VRView GetWindow()
		{
			return EditorWindow.GetWindow<VRView>(true);
		}

		public static Coroutine StartCoroutine(IEnumerator routine)
		{
			if (s_ActiveView && s_ActiveView.m_CameraPivot)
			{
				var mb = s_ActiveView.m_CameraPivot.GetComponent<EditorMonoBehaviour>();
				return mb.StartCoroutine(routine);
			}

			return null;
		}

		// Life cycle management across playmode switches is an odd beast indeed, and there is a need to reliably relaunch
		// EditorVR after we switch back out of playmode (assuming the view was visible before a playmode switch). So,
		// we watch until playmode is done and then relaunch.  
		static VRView()
		{
			EditorApplication.update += ReopenOnExitPlaymode;
		}

		private static void ReopenOnExitPlaymode()
		{
			bool launch = EditorPrefs.GetBool(kLaunchOnExitPlaymode, false);
			if (!launch || !EditorApplication.isPlaying)
			{
				EditorPrefs.DeleteKey(kLaunchOnExitPlaymode);
				EditorApplication.update -= ReopenOnExitPlaymode;
				if (launch)
					GetWindow();
			}
		}

		public void OnEnable()
		{
			EditorApplication.playmodeStateChanged += OnPlaymodeStateChanged;

			Assert.IsNull(s_ActiveView, "Only one EditorVR should be active");

			autoRepaintOnSceneChange = true;
			s_ActiveView = this;

			GameObject cameraGO = EditorUtility.CreateGameObjectWithHideFlags("EditorVRCamera", EditorVR.kDefaultHideFlags, typeof(Camera));
			m_Camera = cameraGO.GetComponent<Camera>();
			m_Camera.enabled = false;
			m_Camera.cameraType = CameraType.VR;

			GameObject pivotGO = EditorUtility.CreateGameObjectWithHideFlags("EditorVRCameraPivot", EditorVR.kDefaultHideFlags, typeof(EditorMonoBehaviour));
			m_CameraPivot = pivotGO.transform;
			m_Camera.transform.parent = m_CameraPivot;
			m_Camera.nearClipPlane = 0.01f;
			m_Camera.farClipPlane = 1000f;

			// Generally, we want to be at a standing height, so default to that
			const float kHeadHeight = 1.7f;
			Vector3 position = m_CameraPivot.position;
			position.y = kHeadHeight;
			m_CameraPivot.position = position;
			m_CameraPivot.rotation = Quaternion.identity;

			m_ShowDeviceView = EditorPrefs.GetBool(kShowDeviceView, false);
			m_UseCustomPreviewCamera = EditorPrefs.GetBool(kUseCustomPreviewCamera, false);

			// Disable other views to increase rendering performance for EditorVR
			SetOtherViewsEnabled(false);

			VRSettings.StartRenderingToDevice();
			InputTracking.Recenter();
			// HACK: Fix VRSettings.enabled or some other API to check for missing HMD
			m_VRInitialized = false;
#if ENABLE_OVR_INPUT
			m_VRInitialized |= OVRPlugin.initialized;
#endif

#if ENABLE_STEAMVR_INPUT
			m_VRInitialized |= (OpenVR.IsHmdPresent() && OpenVR.Compositor != null);
#endif

			onEnable();
		}

		public void OnDisable()
		{
			onDisable();

			EditorApplication.playmodeStateChanged -= OnPlaymodeStateChanged;

			VRSettings.StopRenderingToDevice();

			EditorPrefs.SetBool(kShowDeviceView, m_ShowDeviceView);
			EditorPrefs.SetBool(kUseCustomPreviewCamera, m_UseCustomPreviewCamera);

			SetOtherViewsEnabled(true);

			if (m_CameraPivot)
				DestroyImmediate(m_CameraPivot.gameObject, true);

			Assert.IsNotNull(s_ActiveView, "EditorVR should have an active view");
			s_ActiveView = null;
		}

		private void UpdateCamera()
		{
			// Latch HMD values early in case it is used in other scripts
			Vector3 headPosition = InputTracking.GetLocalPosition(VRNode.Head);
			Quaternion headRotation = InputTracking.GetLocalRotation(VRNode.Head);

			// HACK: Until an actual fix is found, this is a workaround
			// Delay until the VR subsystem has set the initial tracking position, then we can start latching values for
			// the HMD for the camera transform. Otherwise, we will bork the original centering of the HMD.
			var cameraTransform = m_Camera.transform;
			if (!Mathf.Approximately(Quaternion.Angle(cameraTransform.localRotation, Quaternion.identity), 0f))
				m_LatchHMDValues = true;

			if (Quaternion.Angle(headRotation, m_LastHeadRotation) > 0.1f)
			{
				if (Time.realtimeSinceStartup <= m_TimeSinceLastHMDChange + kHMDActivityTimeout)
					SetSceneViewsEnabled(false);

				// Keep track of HMD activity by tracking head rotations
				m_TimeSinceLastHMDChange = Time.realtimeSinceStartup;
			}

			if (m_LatchHMDValues)
			{
				cameraTransform.localPosition = headPosition;
				cameraTransform.localRotation = headRotation;
				if (!m_HMDReady)
				{
					m_HMDReady = true;
					onHMDReady();
				}
			}

			m_LastHeadRotation = headRotation;
		}

		// TODO: Share this between SceneView/EditorVR in SceneViewUtilies
		public void CreateCameraTargetTexture(ref RenderTexture renderTexture, Rect cameraRect, bool hdr)
		{
			bool useSRGBTarget = QualitySettings.activeColorSpace == ColorSpace.Linear;

			int msaa = Mathf.Max(1, QualitySettings.antiAliasing);
			
			RenderTextureFormat format = hdr ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32;
			if (renderTexture != null)
			{
				bool matchingSRGB = renderTexture != null && useSRGBTarget == renderTexture.sRGB;

				if (renderTexture.format != format || renderTexture.antiAliasing != msaa || !matchingSRGB)
				{
					Object.DestroyImmediate(renderTexture);
					renderTexture = null;
				}
			}

			Rect actualCameraRect = cameraRect;
			int width = (int)actualCameraRect.width;
			int height = (int)actualCameraRect.height;

			if (renderTexture == null)
			{
				renderTexture = new RenderTexture(0, 0, 24, format);
				renderTexture.name = "Scene RT";
				renderTexture.antiAliasing = msaa;
				renderTexture.hideFlags = HideFlags.HideAndDontSave;
			}
			if (renderTexture.width != width || renderTexture.height != height)
			{
				renderTexture.Release();
				renderTexture.width = width;
				renderTexture.height = height;
			}
			renderTexture.Create();
		}

		private void PrepareCameraTargetTexture(Rect cameraRect)
		{
			// Always render camera into a RT
			CreateCameraTargetTexture(ref m_SceneTargetTexture, cameraRect, false);
			m_Camera.targetTexture = m_ShowDeviceView ? m_SceneTargetTexture : null;
			VRSettings.showDeviceView = !customPreviewCamera && m_ShowDeviceView;
		}

		private void OnGUI()
		{
			onGUIDelegate(this);
			var e = Event.current;
			if (e.type != EventType.ExecuteCommand && e.type != EventType.used)
			{
				SceneViewUtilities.ResetOnGUIState();

				var guiRect = new Rect(0, 0, position.width, position.height);
				var cameraRect = EditorGUIUtility.PointsToPixels(guiRect);
				PrepareCameraTargetTexture(cameraRect);

				m_Camera.cullingMask = m_CullingMask.HasValue ? m_CullingMask.Value.value : Tools.visibleLayers;

				// Draw camera
				bool pushedGUIClip;
				DoDrawCamera(guiRect, out pushedGUIClip);

				if (m_ShowDeviceView)
					SceneViewUtilities.DrawTexture(customPreviewCamera && customPreviewCamera.targetTexture ? customPreviewCamera.targetTexture : m_SceneTargetTexture, guiRect, pushedGUIClip);

				GUILayout.BeginArea(guiRect);
				{
					if (GUILayout.Button("Toggle Device View", EditorStyles.toolbarButton))
						m_ShowDeviceView = !m_ShowDeviceView;

					if (m_CustomPreviewCamera)
					{
						GUILayout.FlexibleSpace();
						GUILayout.BeginHorizontal();
						{
							GUILayout.FlexibleSpace();
							m_UseCustomPreviewCamera = GUILayout.Toggle(m_UseCustomPreviewCamera, "Use Presentation Camera");
						}
						GUILayout.EndHorizontal();
					}
				}
				GUILayout.EndArea();
			}
		}

		private void DoDrawCamera(Rect cameraRect, out bool pushedGUIClip)
		{
			pushedGUIClip = false;
			if (!m_Camera.gameObject.activeInHierarchy)
				return;

			if (!m_VRInitialized)
				return;

			SceneViewUtilities.DrawCamera(m_Camera, cameraRect, position, m_RenderMode, out pushedGUIClip);
		}

		private void OnPlaymodeStateChanged()
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode)
			{
				EditorPrefs.SetBool(kLaunchOnExitPlaymode, true);
				Close();
			}
		}

		private void Update()
		{
			// If code is compiling, then we need to clean up the window resources before classes get re-initialized
			if (EditorApplication.isCompiling)
			{
				Close();
				return;
			}

			// Force the window to repaint every tick, since we need live updating
			// This also allows scripts with [ExecuteInEditMode] to run
			SceneViewUtilities.SetSceneRepaintDirty();

			UpdateCamera();

			// Re-enable the other scene views if there has been no activity from the HMD (allows editing in SceneView)
			if (Time.realtimeSinceStartup >= m_TimeSinceLastHMDChange + kHMDActivityTimeout)
				SetSceneViewsEnabled(true);
		}

		private void SetGameViewsEnabled(bool enabled)
		{
			Assembly asm = Assembly.GetAssembly(typeof(UnityEditor.EditorWindow));
			Type type = asm.GetType("UnityEditor.GameView");
			SceneViewUtilities.SetViewsEnabled(type, enabled);
		}

		private void SetSceneViewsEnabled(bool enabled)
		{
			// It's costly to call through to SetViewsEnabled, so only call when the value has changed
			if (m_SceneViewsEnabled != enabled)
			{
				SceneViewUtilities.SetViewsEnabled(typeof(SceneView), enabled);
				m_SceneViewsEnabled = enabled;
			}
		}

		private void SetOtherViewsEnabled(bool enabled)
		{
			SetGameViewsEnabled(enabled);
			SetSceneViewsEnabled(enabled);
		}
	}
}
#endif