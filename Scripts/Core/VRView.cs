using System;
using System.Linq;
using System.Reflection;
using Unity.Labs.Utils;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SpatialTracking;
using UnityEngine.XR;
using InputTracking = UnityEngine.XR.InputTracking;
using TrackingSpaceType = UnityEngine.XR.TrackingSpaceType;

namespace UnityEditor.Experimental.EditorVR.Core
{
    sealed class VRView
#if UNITY_EDITOR
        : EditorWindow
#endif
    {
        public const float HeadHeight = 1.7f;
        const string k_ShowDeviceView = "VRView.ShowDeviceView";
        const string k_UseCustomPreviewCamera = "VRView.UseCustomPreviewCamera";
        const string k_CameraName = "VRCamera";
        const HideFlags k_HideFlags = HideFlags.None;// HideFlags.HideInHierarchy | HideFlags.DontSaveInEditor;

        static Camera s_ExistingSceneMainCamera;
        static bool s_ExistingSceneMainCameraEnabledState;

#if UNITY_EDITOR
        DrawCameraMode m_RenderMode = DrawCameraMode.Textured;

        // To allow for alternate previews (e.g. smoothing)
        public static Camera customPreviewCamera
        {
            get { return s_ActiveView != null && s_ActiveView.m_UseCustomPreviewCamera ? s_ActiveView.m_CustomPreviewCamera : null; }
            set
            {
                if (s_ActiveView != null)
                {

                    if (s_ExistingSceneMainCamera && !s_ActiveView.m_CustomPreviewCamera && EditingContextManager.defaultContext.copyMainCameraImageEffectsToPresentationCamera)
                        CopyImagesEffectsToCamera(value);

                    s_ActiveView.m_CustomPreviewCamera = value;
                }
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

        Rect m_ToggleDeviceViewRect = new Rect(5, 0, 140, 20); // Width will be set based on window size
        Rect m_PresentationCameraRect = new Rect(0, 0, 165, 20); // Y position and width will be set based on window size

        public static Transform cameraRig
        {
            get
            {
                if (s_ActiveView != null)
                    return s_ActiveView.m_CameraRig;

                return null;
            }
        }

        public static Camera viewerCamera
        {
            get
            {
                if (s_ActiveView != null)
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
            get { return s_ActiveView != null && s_ActiveView.m_ShowDeviceView; }
        }

        public static LayerMask cullingMask
        {
            set
            {
                if (s_ActiveView != null)
                    s_ActiveView.m_CullingMask = value;
            }
        }
#endif

        public static Vector3 headCenteredOrigin
        {
            get
            {
                return XRDevice.GetTrackingSpaceType() == TrackingSpaceType.Stationary ? Vector3.up * HeadHeight : Vector3.zero;
            }
        }

#if UNITY_EDITOR
        public static event Action viewEnabled;
        public static event Action viewDisabled;
        public static event Action<VRView> beforeOnGUI;
        public static event Action<VRView> afterOnGUI;
        public static event Action<bool> hmdStatusChange;
#endif

        public Rect guiRect { get; private set; }

        public static Vector2 MouseDelta;
        public static Vector2 MouseScrollDelta;
        public static bool LeftMouseButtonHeld;
        public static bool MiddleMouseButtonHeld;
        public static bool RightMouseButtonHeld;

        public static void CreateCameraRig(ref Camera camera, ref Transform cameraRig)
        {
            var hideFlags = Application.isPlaying ? HideFlags.None : k_HideFlags;

            const float nearClipPlane = 0.01f;
            const float farClipPlane = 1000f;

            // Redundant assignment for player builds
            // ReSharper disable once RedundantAssignment
            GameObject rigGO = null;

            if (Application.isPlaying)
            {
                camera.nearClipPlane = nearClipPlane;
                camera.farClipPlane = farClipPlane;

                rigGO = new GameObject("VRCameraRig");
            }
#if UNITY_EDITOR
            else
            {
                s_ExistingSceneMainCamera = Camera.main;

                // TODO: Copy camera settings when changing contexts
                var defaultContext = EditingContextManager.defaultContext;
                if (defaultContext.copyMainCameraSettings && s_ExistingSceneMainCamera && s_ExistingSceneMainCamera.enabled)
                {
                    GameObject cameraGO = EditorUtility.CreateGameObjectWithHideFlags(k_CameraName, hideFlags);
                    camera = EditorXRUtils.CopyComponent(s_ExistingSceneMainCamera, cameraGO);

                    if (camera.nearClipPlane > nearClipPlane)
                    {
                        Debug.LogWarning("Copying settings from scene camera that is tagged 'MainCamera'." + Environment.NewLine +
                            " Clipping issues may occur with NearClipPlane values is greater than " + nearClipPlane);

                        camera.nearClipPlane = nearClipPlane;
                    }

                    // TODO: Support multiple cameras
                    if (camera.clearFlags == CameraClearFlags.Nothing)
                        camera.clearFlags = CameraClearFlags.SolidColor;

                    camera.stereoTargetEye = StereoTargetEyeMask.Both;

                    // Force HDR on because of a bug in the mirror view
                    camera.allowHDR = true;
                }
                else
                {
                    GameObject cameraGO = EditorUtility.CreateGameObjectWithHideFlags(k_CameraName, hideFlags, typeof(Camera));
                    camera = cameraGO.GetComponent<Camera>();

                    camera.nearClipPlane = nearClipPlane;
                    camera.farClipPlane = farClipPlane;
                }

                camera.enabled = false;
                camera.cameraType = CameraType.VR;
                camera.useOcclusionCulling = false;

                if (s_ExistingSceneMainCamera && defaultContext.copyMainCameraImageEffectsToHMD)
                {
                    CopyImagesEffectsToCamera(viewerCamera);

                    s_ExistingSceneMainCameraEnabledState = s_ExistingSceneMainCamera.enabled;
                    s_ExistingSceneMainCamera.enabled = false; // Disable existing MainCamera in the scene
                }

                rigGO = EditorUtility.CreateGameObjectWithHideFlags("VRCameraRig", hideFlags, typeof(EditorMonoBehaviour));
            }
#endif

            cameraRig = rigGO.transform;
            camera.transform.parent = cameraRig;

            if (Application.isPlaying)
            {
                var tpd = camera.GetComponent<TrackedPoseDriver>();
                if (!tpd)
                    tpd = camera.gameObject.AddComponent<TrackedPoseDriver>();

                tpd.UseRelativeTransform = false;
            }
            else
            {
                cameraRig.rotation = Quaternion.identity;
                cameraRig.position = headCenteredOrigin;
            }
        }

#if UNITY_EDITOR
        public void OnEnable()
        {
            Assert.IsNull(s_ActiveView, "Only one EditorXR should be active");

            autoRepaintOnSceneChange = true;
            s_ActiveView = this;
            CreateCameraRig(ref m_Camera, ref m_CameraRig);

            m_ShowDeviceView = EditorPrefs.GetBool(k_ShowDeviceView, false);
            m_UseCustomPreviewCamera = EditorPrefs.GetBool(k_UseCustomPreviewCamera, false);

            // Disable other views to increase rendering performance for EditorVR
            SetOtherViewsEnabled(false);

            // VRSettings.enabled latches the reference pose for the current camera
            var currentCamera = Camera.current;
            Camera.SetupCurrent(m_Camera);
            XRSettings.enabled = true;
            Camera.SetupCurrent(currentCamera);

            if (viewEnabled != null)
                viewEnabled();
        }

        static void CopyImagesEffectsToCamera(Camera targetCamera)
        {
            var targetCameraGO = targetCamera.gameObject;
            var potentialImageEffects = s_ExistingSceneMainCamera.GetComponents<MonoBehaviour>();
            var enabledPotentialImageEffects = potentialImageEffects.Where(x => x != null && x.enabled);
            var targetMethodNames = new[] { "OnRenderImage", "OnPreRender", "OnPostRender" };
            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            foreach (var potentialImageEffect in enabledPotentialImageEffects)
            {
                var componentInstanceType = potentialImageEffect.GetType();
                var targetMethodFound = false;
                for (int i = 0; i < targetMethodNames.Length; ++i)
                {
                    targetMethodFound = componentInstanceType.GetMethodRecursively(targetMethodNames[i], bindingFlags) != null;

                    if (targetMethodFound)
                        break;
                }

                // Copying of certain image effects can cause Unity to crash when copied
                if (targetMethodFound)
                    EditorXRUtils.CopyComponent(potentialImageEffect, targetCameraGO);
            }
        }

        public void OnDisable()
        {
            if (viewDisabled != null)
                viewDisabled();

            XRSettings.enabled = false;

            EditorPrefs.SetBool(k_ShowDeviceView, m_ShowDeviceView);
            EditorPrefs.SetBool(k_UseCustomPreviewCamera, m_UseCustomPreviewCamera);

            SetOtherViewsEnabled(true);

            if (m_CameraRig)
                DestroyImmediate(m_CameraRig.gameObject, true);

            Assert.IsNotNull(s_ActiveView, "EditorXR should have an active view");
            s_ActiveView = null;

            if (s_ExistingSceneMainCamera)
                s_ExistingSceneMainCamera.enabled = s_ExistingSceneMainCameraEnabledState;
        }

        void UpdateCameraTransform()
        {
            if (!m_Camera)
                return;

            var cameraTransform = m_Camera.transform;
            cameraTransform.localPosition = InputTracking.GetLocalPosition(XRNode.Head);
            cameraTransform.localRotation = InputTracking.GetLocalRotation(XRNode.Head);
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
                renderTexture.hideFlags = k_HideFlags;
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
            m_Camera.targetTexture = m_TargetTexture;
            XRSettings.showDeviceView = !customPreviewCamera && m_ShowDeviceView;
        }

        void OnGUI()
        {
            if (beforeOnGUI != null)
                beforeOnGUI(this);

            var height = position.height;
            var width = position.width;
            var rect = guiRect;
            rect.x = 0;
            rect.y = 0;
            rect.width = width;
            rect.height = height;
            guiRect = rect;
            var cameraRect = EditorGUIUtility.PointsToPixels(guiRect);
            PrepareCameraTargetTexture(cameraRect);

            m_Camera.cullingMask = m_CullingMask.HasValue ? m_CullingMask.Value.value : UnityEditor.Tools.visibleLayers;

            DoDrawCamera(guiRect);

            MouseScrollDelta = Vector2.zero;
            var e = Event.current;
            MouseDelta = e.delta;
            switch (e.type)
            {
                case EventType.ScrollWheel:
                    MouseScrollDelta = e.delta;
                    break;
                case EventType.MouseDown:
                    switch (e.button)
                    {
                        case 0:
                            LeftMouseButtonHeld = true;
                            break;
                        case 1:
                            RightMouseButtonHeld = true;
                            break;
                        case 2:
                            MiddleMouseButtonHeld = true;
                            break;
                    }

                    break;
                case EventType.MouseUp:
                    switch (e.button)
                    {
                        case 0:
                            LeftMouseButtonHeld = false;
                            break;
                        case 1:
                            RightMouseButtonHeld = false;
                            break;
                        case 2:
                            MiddleMouseButtonHeld = false;
                            break;
                    }

                    break;
            }

            if (m_ShowDeviceView)
            {
                if (e.type == EventType.Repaint)
                {
                    var renderTexture = customPreviewCamera && customPreviewCamera.targetTexture ? customPreviewCamera.targetTexture : m_TargetTexture;
                    GUI.DrawTexture(guiRect, renderTexture, ScaleMode.StretchToFill, false);
                }
            }

            m_ToggleDeviceViewRect.y = height - m_ToggleDeviceViewRect.height;
            m_PresentationCameraRect.x = width - m_PresentationCameraRect.width;
            m_PresentationCameraRect.y = height - m_PresentationCameraRect.height;

            const string deviceViewEnabled = "Device View Enabled";
            const string deviceViewDisabled = "Device View Disabled";
            m_ShowDeviceView = GUI.Toggle(m_ToggleDeviceViewRect, m_ShowDeviceView, m_ShowDeviceView ? deviceViewEnabled : deviceViewDisabled);

            if (m_CustomPreviewCamera)
                m_UseCustomPreviewCamera = GUI.Toggle(m_PresentationCameraRect, m_UseCustomPreviewCamera, "Use Presentation Camera");

            if (afterOnGUI != null)
                afterOnGUI(this);
        }

        void DoDrawCamera(Rect rect)
        {
            if (!m_Camera.gameObject.activeInHierarchy)
                return;

            if (Event.current.type == EventType.Repaint)
            {
                if (XRDevice.isPresent)
                    UnityEditor.Handles.DrawCamera(rect, m_Camera, m_RenderMode);
                else
                    m_Camera.Render();

                GUI.matrix = Matrix4x4.identity; // Need to push GUI matrix back to GPU after camera rendering
                RenderTexture.active = null; // Clean up after DrawCamera
            }
        }

        private void Update()
        {
            // If code is compiling, then we need to clean up the window resources before classes get re-initialized
            if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Close();
                return;
            }

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

        internal static bool GetIsUserPresent()
        {
            return XRDevice.userPresence == UserPresenceState.Present;
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
#endif
    }
}
