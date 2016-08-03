#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.VR;
using UnityEngine.Assertions;
using System.Collections;
using UnityEditor.VR.Helpers;
using System.Reflection;
using Object = UnityEngine.Object;
using UnityEngine.VR.Utilities;

namespace UnityEditor.VR
{
	[InitializeOnLoad]
	public class VRView : EditorWindow
	{
		public static VRView GetWindow()
		{
			return EditorWindow.GetWindow<VRView>(true);
		}

		public static Coroutine StartCoroutine(IEnumerator routine)
		{
			if (s_ActiveView && s_ActiveView.m_CameraPivot)
			{
				EditorMonoBehaviour mb = s_ActiveView.m_CameraPivot.GetComponent<EditorMonoBehaviour>();
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
		
		public static Transform viewerPivot
		{
			get
			{
				if (s_ActiveView)
				{
					return s_ActiveView.m_CameraPivot;
				}
				else
				{
					return null;
				}
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
				else
				{
					return null;
				}
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
				else
				{
					return new Rect();
				}
			}
		}

		public static VRView activeView
		{
			get
			{
				return s_ActiveView;
			}
		}

		public static event System.Action onEnable = delegate {};
		public static event System.Action onDisable = delegate {};
		public static event System.Action<EditorWindow> onGUIDelegate = delegate { };

		public DrawCameraMode m_RenderMode = DrawCameraMode.Textured;
		
		[NonSerialized]
		private Camera m_Camera;

		private RenderTexture m_SceneTargetTexture;

		private static VRView s_ActiveView = null;

		private Transform m_CameraPivot = null;
		private Quaternion m_LastHeadRotation = Quaternion.identity;
		private float m_TimeSinceLastHMDChange = 0f;
		
		private const string kLaunchOnExitPlaymode = "EditorVR.LaunchOnExitPlaymode";
		private const float kHMDActivityTimeout = 3f; // in seconds

		public void OnEnable()
		{
			EditorApplication.playmodeStateChanged += OnPlaymodeStateChanged;

			Assert.IsNull(s_ActiveView, "Only one EditorVR should be active");

			autoRepaintOnSceneChange = true;
			wantsMouseMove = true;
			s_ActiveView = this;

			GameObject cameraGO = EditorUtility.CreateGameObjectWithHideFlags("EditorVRCamera", EditorVR.kDefaultHideFlags, typeof(Camera));
			m_Camera = cameraGO.GetComponent<Camera>();
			m_Camera.enabled = false;
			m_Camera.cameraType = CameraType.VR;

			U.Object.AddComponent<VivePoseUpdater>(cameraGO);

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

			// Disable other views to increase rendering performance for EditorVR
			SetOtherViewsEnabled(false);

			VRSettings.StartRenderingToDevice();
			InputTracking.Recenter();

			onEnable();
		}

		public void OnDisable()
		{
			onDisable();

			EditorApplication.playmodeStateChanged -= OnPlaymodeStateChanged;

			VRSettings.StopRenderingToDevice();

			SetOtherViewsEnabled(true);

			if (m_CameraPivot)
				DestroyImmediate(m_CameraPivot.gameObject, true);

			Assert.IsNotNull(s_ActiveView, "EditorVR should have an active view");
			s_ActiveView = null;
		}

		protected void SetupCamera()
		{
			// Transfer original camera position and rotation to pivot, since it will get overridden by tracking
			//m_CameraPivot.position = m_Camera.transform.position;
			//m_CameraPivot.rotation = m_Camera.transform.rotation;
			//m_Camera.ResetFieldOfView(); // Use FOV from HMD

			// Latch HMD values initially
			m_Camera.transform.localPosition = InputTracking.GetLocalPosition(VRNode.Head);
			Quaternion headRotation = InputTracking.GetLocalRotation(VRNode.Head);
			if (Quaternion.Angle(headRotation, m_LastHeadRotation) > 0.1f)
			{
				if (Time.realtimeSinceStartup <= m_TimeSinceLastHMDChange + kHMDActivityTimeout)
					SetSceneViewsEnabled(false);

				// Keep track of HMD activity by tracking head rotations
				m_TimeSinceLastHMDChange = Time.realtimeSinceStartup;
			}
			m_Camera.transform.localRotation = headRotation;
			m_LastHeadRotation = headRotation;
		}

		// TODO: Share this between SceneView/EditorVR in SceneViewUtilies
		private void CreateCameraTargetTexture(Rect cameraRect, bool hdr)
		{
			bool useSRGBTarget = QualitySettings.activeColorSpace == ColorSpace.Linear;

			int msaa = Mathf.Max(1, QualitySettings.antiAliasing);
			
			RenderTextureFormat format = hdr ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32;
			if (m_SceneTargetTexture != null)
			{
				bool matchingSRGB = m_SceneTargetTexture != null && useSRGBTarget == m_SceneTargetTexture.sRGB;

				if (m_SceneTargetTexture.format != format || m_SceneTargetTexture.antiAliasing != msaa || !matchingSRGB)
				{
					Object.DestroyImmediate(m_SceneTargetTexture);
					m_SceneTargetTexture = null;
				}
			}

			Rect actualCameraRect = cameraRect; // Handles.GetCameraRect(cameraRect);
			int width = (int)actualCameraRect.width;
			int height = (int)actualCameraRect.height;

			if (m_SceneTargetTexture == null)
			{
				m_SceneTargetTexture = new RenderTexture(0, 0, 24, format);
				m_SceneTargetTexture.name = "SceneView RT";
				m_SceneTargetTexture.antiAliasing = msaa;
				m_SceneTargetTexture.hideFlags = HideFlags.HideAndDontSave;
			}
			if (m_SceneTargetTexture.width != width || m_SceneTargetTexture.height != height)
			{
				m_SceneTargetTexture.Release();
				m_SceneTargetTexture.width = width;
				m_SceneTargetTexture.height = height;
			}
			m_SceneTargetTexture.Create();
		}


		private void PrepareCameraTargetTexture(Rect cameraRect)
		{
			// Always render camera into a RT
			bool hdr = false; // SceneViewIsRenderingHDR();
			CreateCameraTargetTexture(cameraRect, hdr);
			m_Camera.targetTexture = m_SceneTargetTexture;
		}     

		private void OnGUI()
		{
			onGUIDelegate(this);
			SceneViewUtilities.ResetOnGUIState();

			SetupCamera();

			Rect guiRect = new Rect(0, 0, position.width, position.height);
			Rect cameraRect = EditorGUIUtility.PointsToPixels(guiRect);
			PrepareCameraTargetTexture(cameraRect);
			Handles.ClearCamera(cameraRect, m_Camera);
			
			m_Camera.cullingMask = Tools.visibleLayers;

			// Draw camera
			bool pushedGUIClip;
			DoDrawCamera(guiRect, out pushedGUIClip);

			SceneViewUtilities.BlitRT(m_SceneTargetTexture, guiRect, pushedGUIClip);
		}

		private void DoDrawCamera(Rect cameraRect, out bool pushedGUIClip)
		{
			pushedGUIClip = false;
			if (!m_Camera.gameObject.activeInHierarchy)
				return;
			//DrawGridParameters gridParam = grid.PrepareGridRender(camera, pivot, m_Rotation.target, m_Size.value, m_Ortho.target, AnnotationUtility.showGrid);

			SceneViewUtilities.DrawCamera(m_Camera, cameraRect, position, m_RenderMode, true, out pushedGUIClip);			
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
				Close();

			// Force the window to repaint every tick, since we need live updating
			// This also allows scripts with [ExecuteInEditMode] to run
			SceneViewUtilities.SetSceneRepaintDirty();

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
			SceneViewUtilities.SetViewsEnabled(typeof(SceneView), enabled);
		}

		private void SetOtherViewsEnabled(bool enabled)
		{
			SetGameViewsEnabled(enabled);
			SetSceneViewsEnabled(enabled);
		}
	}
} // namespace
#endif