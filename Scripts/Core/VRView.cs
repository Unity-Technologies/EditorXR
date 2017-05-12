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
using UnityObject = UnityEngine.Object;

namespace UnityEditor.Experimental.EditorVR.Core
{
	[InitializeOnLoad]
	sealed class VRView : EditorWindow
	{
		const string k_ShowDeviceView = "VRView.ShowDeviceView";
		const string k_UseCustomPreviewCamera = "VRView.UseCustomPreviewCamera";
		const string k_LaunchOnExitPlaymode = "VRView.LaunchOnExitPlaymode";

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
		Camera m_Camera;

		LayerMask? m_CullingMask;
		RenderTexture m_TargetTexture;
		bool m_ShowDeviceView;
		EditorWindow[] m_EditorWindows;

		static VRView s_ActiveView;

		Transform m_CameraRig;

		bool m_HMDReady;
		bool m_UseCustomPreviewCamera;

		public static Transform cameraRig
		{
			get
			{
				if (s_ActiveView)
					return s_ActiveView.m_CameraRig;

				return null;
			}
		}

		public static Camera viewerCamera
		{
			get
			{
				if (s_ActiveView)
					return s_ActiveView.m_Camera;

				return null;
			}
		}

		public static VRView activeView
		{
			get { return s_ActiveView; }
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

		public static event Action viewEnabled;
		public static event Action viewDisabled;
		public static event Action<EditorWindow> beforeOnGUI;
		public static event Action<EditorWindow> afterOnGUI;
		public static event Action<bool> hmdStatusChange;

		public Rect guiRect { get; private set; }

		static VRView GetWindow()
		{
			return GetWindow<VRView>(true);
		}

		public static Coroutine StartCoroutine(IEnumerator routine)
		{
			if (s_ActiveView && s_ActiveView.m_CameraRig)
			{
				var mb = s_ActiveView.m_CameraRig.GetComponent<EditorMonoBehaviour>();
				return mb.StartCoroutine(routine);
			}

			return null;
		}

		// Life cycle management across playmode switches is an odd beast indeed, and there is a need to reliably relaunch
		// EditorVR after we switch back out of playmode (assuming the view was visible before a playmode switch). So,
		// we watch until playmode is done and then relaunch.  
		static void ReopenOnExitPlaymode()
		{
			bool launch = EditorPrefs.GetBool(k_LaunchOnExitPlaymode, false);
			if (!launch || !EditorApplication.isPlaying)
			{
				EditorPrefs.DeleteKey(k_LaunchOnExitPlaymode);
				EditorApplication.update -= ReopenOnExitPlaymode;
				if (launch)
					GetWindow<VRView>();
			}
		}

		public void OnEnable()
		{
			EditorApplication.playmodeStateChanged += OnPlaymodeStateChanged;

			Assert.IsNull(s_ActiveView, "Only one EditorVR should be active");

			autoRepaintOnSceneChange = true;
			s_ActiveView = this;

			GameObject cameraGO = EditorUtility.CreateGameObjectWithHideFlags("VRCamera", HideFlags.HideAndDontSave, typeof(Camera));
			m_Camera = cameraGO.GetComponent<Camera>();
			m_Camera.useOcclusionCulling = false;
			m_Camera.enabled = false;
			m_Camera.cameraType = CameraType.VR;

			GameObject rigGO = EditorUtility.CreateGameObjectWithHideFlags("VRCameraRig", HideFlags.HideAndDontSave, typeof(EditorMonoBehaviour));
			m_CameraRig = rigGO.transform;
			m_Camera.transform.parent = m_CameraRig;
			m_Camera.nearClipPlane = 0.01f;
			m_Camera.farClipPlane = 1000f;

			// Generally, we want to be at a standing height, so default to that
			const float kHeadHeight = 1.7f;
			Vector3 position = m_CameraRig.position;
			position.y = kHeadHeight;
			m_CameraRig.position = position;
			m_CameraRig.rotation = Quaternion.identity;

			m_ShowDeviceView = EditorPrefs.GetBool(k_ShowDeviceView, false);
			m_UseCustomPreviewCamera = EditorPrefs.GetBool(k_UseCustomPreviewCamera, false);

			// Disable other views to increase rendering performance for EditorVR
			SetOtherViewsEnabled(false);

			// VRSettings.enabled latches the reference pose for the current camera
			var currentCamera = Camera.current;
			Camera.SetupCurrent(m_Camera);
			VRSettings.enabled = true;
			InputTracking.Recenter();
			Camera.SetupCurrent(currentCamera);

			if (viewEnabled != null)
				viewEnabled();
		}

		public void OnDisable()
		{
			if (viewDisabled != null)
				viewDisabled();

			EditorApplication.playmodeStateChanged -= OnPlaymodeStateChanged;

			VRSettings.enabled = false;

			EditorPrefs.SetBool(k_ShowDeviceView, m_ShowDeviceView);
			EditorPrefs.SetBool(k_UseCustomPreviewCamera, m_UseCustomPreviewCamera);

			SetOtherViewsEnabled(true);

			if (m_CameraRig)
				DestroyImmediate(m_CameraRig.gameObject, true);

			Assert.IsNotNull(s_ActiveView, "EditorVR should have an active view");
			s_ActiveView = null;
		}

		void UpdateCameraTransform()
		{
			var cameraTransform = m_Camera.transform;
			cameraTransform.localPosition = InputTracking.GetLocalPosition(VRNode.Head);
			cameraTransform.localRotation = InputTracking.GetLocalRotation(VRNode.Head);
		}

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
					DestroyImmediate(renderTexture);
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

		void PrepareCameraTargetTexture(Rect cameraRect)
		{
			// Always render camera into a RT
			CreateCameraTargetTexture(ref m_TargetTexture, cameraRect, false);
			m_Camera.targetTexture = m_ShowDeviceView ? m_TargetTexture : null;
			VRSettings.showDeviceView = !customPreviewCamera && m_ShowDeviceView;
		}

		void OnGUI()
		{
			if (beforeOnGUI != null)
				beforeOnGUI(this);

			var rect = guiRect;
			rect.x = 0;
			rect.y = 0;
			rect.width = position.width;
			rect.height = position.height;
			guiRect = rect;
			var cameraRect = EditorGUIUtility.PointsToPixels(guiRect);
			PrepareCameraTargetTexture(cameraRect);

			m_Camera.cullingMask = m_CullingMask.HasValue ? m_CullingMask.Value.value : UnityEditor.Tools.visibleLayers;

			DoDrawCamera(guiRect);

			Event e = Event.current;
			if (m_ShowDeviceView)
			{
				if (e.type == EventType.Repaint)
				{
					GL.sRGBWrite = (QualitySettings.activeColorSpace == ColorSpace.Linear);
					var renderTexture = customPreviewCamera && customPreviewCamera.targetTexture ? customPreviewCamera.targetTexture : m_TargetTexture;
					GUI.BeginGroup(guiRect);
					GUI.DrawTexture(guiRect, renderTexture, ScaleMode.StretchToFill, false);
					GUI.EndGroup();
					GL.sRGBWrite = false;
				}
			}

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
			
			if (afterOnGUI != null)
				afterOnGUI(this);
		}

		void DoDrawCamera(Rect rect)
		{
			if (!m_Camera.gameObject.activeInHierarchy)
				return;

			if (!VRDevice.isPresent)
				return;

			UnityEditor.Handles.DrawCamera(rect, m_Camera, m_RenderMode);
			if (Event.current.type == EventType.Repaint)
			{
				GUI.matrix = Matrix4x4.identity; // Need to push GUI matrix back to GPU after camera rendering
				RenderTexture.active = null; // Clean up after DrawCamera
			}
		}

		private void OnPlaymodeStateChanged()
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode)
			{
				EditorPrefs.SetBool(k_LaunchOnExitPlaymode, true);
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
			EditorApplication.SetSceneRepaintDirty();

			// Our camera is disabled, so it doesn't get automatically updated to HMD values until it renders
			UpdateCameraTransform();

			UpdateHMDStatus();

			SetSceneViewsAutoRepaint(false);
		}

		void UpdateHMDStatus()
		{
			if (hmdStatusChange != null)
			{
				var ready = GetIsUserPresent();
				if (m_HMDReady != ready)
				{
					m_HMDReady = ready;
					hmdStatusChange(ready);
				}
			}
		}

		static bool GetIsUserPresent()
		{
#if ENABLE_OVR_INPUT
			if (VRSettings.loadedDeviceName == "Oculus")
				return OVRPlugin.userPresent;
#endif
#if ENABLE_STEAMVR_INPUT
			if (VRSettings.loadedDeviceName == "OpenVR")
				return OpenVR.System.GetTrackedDeviceActivityLevel(0) == EDeviceActivityLevel.k_EDeviceActivityLevel_UserInteraction;
#endif
			return true;
		}

		void SetGameViewsAutoRepaint(bool enabled)
		{
			var asm = Assembly.GetAssembly(typeof(UnityEditor.EditorWindow));
			var type = asm.GetType("UnityEditor.GameView");
			SetAutoRepaintOnSceneChanged(type, enabled);
		}

		void SetSceneViewsAutoRepaint(bool enabled)
		{
			SetAutoRepaintOnSceneChanged(typeof(SceneView), enabled);
		}

		void SetOtherViewsEnabled(bool enabled)
		{
			SetGameViewsAutoRepaint(enabled);
			SetSceneViewsAutoRepaint(enabled);
		}

		void SetAutoRepaintOnSceneChanged(Type viewType, bool enabled)
		{
			if (m_EditorWindows == null)
				m_EditorWindows = Resources.FindObjectsOfTypeAll<EditorWindow>();

			var windowCount = m_EditorWindows.Length;
			var mouseOverWindow = EditorWindow.mouseOverWindow;
			for (int i = 0; i < windowCount; i++)
			{
				var window = m_EditorWindows[i];
				if (window.GetType() == viewType)
					window.autoRepaintOnSceneChange = enabled || (window == mouseOverWindow);
			}

		}
	}
}
#endif