#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEditor.Experimental.EditorVR.Helpers;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.XR;

#if ENABLE_STEAMVR_INPUT
using Valve.VR;
#endif

namespace UnityEditor.Experimental.EditorVR.Core
{
    sealed class VRView : EditorWindow
    {
        public const float HeadHeight = 1.7f;
        const string k_ShowDeviceView = "VRView.ShowDeviceView";
        const string k_UseCustomPreviewCamera = "VRView.UseCustomPreviewCamera";
        const string k_CameraName = "VRCamera";

        static Camera s_ExistingSceneMainCamera;
        static bool s_ExistingSceneMainCameraEnabledState;

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

        Rect m_ToggleDeviceViewRect = new Rect(0, 0, 0, 20); // Width will be set based on window size
        Rect m_PresentationCameraRect = new Rect(0, 0, 160, 20); // Y position and width will be set based on window size

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

        public static Vector3 headCenteredOrigin
        {
            get
            {
#if UNITY_2017_2_OR_NEWER
                return XRDevice.GetTrackingSpaceType() == TrackingSpaceType.Stationary ? Vector3.up * HeadHeight : Vector3.zero;
#else
                return Vector3.zero;
#endif
            }
        }

        public static event Action viewEnabled;
        public static event Action viewDisabled;
        public static event Action<VRView> beforeOnGUI;
        public static event Action<VRView> afterOnGUI;
        public static event Action<bool> hmdStatusChange;

        public Rect guiRect { get; private set; }

        public static Coroutine StartCoroutine(IEnumerator routine)
        {
            if (s_ActiveView && s_ActiveView.m_CameraRig)
            {
                var mb = s_ActiveView.m_CameraRig.GetComponent<EditorMonoBehaviour>();
                return mb.StartCoroutine(routine);
            }

            return null;
        }

        public void OnEnable()
        {
            Assert.IsNull(s_ActiveView, "Only one EditorXR should be active");

            autoRepaintOnSceneChange = true;
            s_ActiveView = this;
            const float nearClipPlane = 0.01f;
            const float farClipPlane = 1000f;

            s_ExistingSceneMainCamera = Camera.main;
            // TODO: Copy camera settings when changing contexts
            if (EditingContextManager.defaultContext.copyExistingCameraSettings && s_ExistingSceneMainCamera && s_ExistingSceneMainCamera.enabled)
            {
                GameObject cameraGO = EditorUtility.CreateGameObjectWithHideFlags(k_CameraName, HideFlags.HideAndDontSave);
                m_Camera = ObjectUtils.CopyComponent(s_ExistingSceneMainCamera, cameraGO);

                if (m_Camera.nearClipPlane > nearClipPlane)
                {
                    Debug.LogWarning("Copying settings from scene camera that is tagged 'MainCamera'." + Environment.NewLine +
                        " Clipping issues may occur with NearClipPlane values is greater than " + nearClipPlane);

                    m_Camera.nearClipPlane = nearClipPlane;
                }

                // TODO: Support multiple cameras
                if (m_Camera.clearFlags == CameraClearFlags.Nothing)
                    m_Camera.clearFlags = CameraClearFlags.SolidColor;

                m_Camera.stereoTargetEye = StereoTargetEyeMask.Both;
                // Force HDR on because of a bug in the mirror view
                m_Camera.allowHDR = true;
            }
            else
            {
                GameObject cameraGO = EditorUtility.CreateGameObjectWithHideFlags(k_CameraName, HideFlags.HideAndDontSave, typeof(Camera));
                m_Camera = cameraGO.GetComponent<Camera>();

                m_Camera.nearClipPlane = nearClipPlane;
                m_Camera.farClipPlane = farClipPlane;
            }

            m_Camera.enabled = false;
            m_Camera.cameraType = CameraType.VR;
            m_Camera.useOcclusionCulling = false;
            GameObject rigGO = EditorUtility.CreateGameObjectWithHideFlags("VRCameraRig", HideFlags.HideAndDontSave, typeof(EditorMonoBehaviour));
            m_CameraRig = rigGO.transform;
            m_Camera.transform.parent = m_CameraRig;
            m_CameraRig.position = headCenteredOrigin;
            m_CameraRig.rotation = Quaternion.identity;

            if (s_ExistingSceneMainCamera)
            {
                var cameraGameObject = m_Camera.gameObject;
                var potentialImageEffects = s_ExistingSceneMainCamera.GetComponents<MonoBehaviour>();
                var enabledPotentialImageEffects = potentialImageEffects.Where(x => x != null && x.enabled);
                var targetMethodNames = new [] {"OnRenderImage", "OnPreRender", "OnPostRender"};
                var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
                foreach (var potentialImageEffect in enabledPotentialImageEffects)
                {
                    var componentInstanceType = potentialImageEffect.GetType();
                    var targetMethodFound = false;
                    for (int i = 0; i < targetMethodNames.Length; ++i)
                    {
                        // Each of the three checks is performed to catch the various image effect variants I've tested against
                        // Each check catches a different case that was encountered during testing
                        // Check isntanced type for target methods
                        targetMethodFound = componentInstanceType.GetMethod(targetMethodNames[i], bindingFlags) != null;

                        // Check base type for target methods
                        if (!targetMethodFound)
                        {
                            var componentBaseType = componentInstanceType.BaseType;
                            if (componentBaseType != null)
                                targetMethodFound = componentBaseType.GetMethod(targetMethodNames[i], bindingFlags) != null;
                        }

                        // Check nested types for target methods
                        if (!targetMethodFound)
                        {
                            var nestedTypes = componentInstanceType.GetNestedTypes();
                            foreach (var nestedType in nestedTypes)
                            {
                                if (nestedType == null)
                                    continue;

                                targetMethodFound = nestedType.GetMethod(targetMethodNames[i], bindingFlags) != null;
                                if (targetMethodFound)
                                    break;
                            }
                        }

                        if (targetMethodFound)
                            break;
                    }

                    // Copying of certain image effects can cause Unity to crash when copied
                    if (targetMethodFound)
                        ObjectUtils.CopyComponent(potentialImageEffect, cameraGameObject);
                }

                s_ExistingSceneMainCameraEnabledState = s_ExistingSceneMainCamera.enabled;
                s_ExistingSceneMainCamera.enabled = false; // Disable existing MainCamera in the scene
            }

            m_ShowDeviceView = EditorPrefs.GetBool(k_ShowDeviceView, false);
            m_UseCustomPreviewCamera = EditorPrefs.GetBool(k_UseCustomPreviewCamera, false);

            // Disable other views to increase rendering performance for EditorVR
            SetOtherViewsEnabled(false);

            // VRSettings.enabled latches the reference pose for the current camera
            var currentCamera = Camera.current;
            Camera.SetupCurrent(m_Camera);
#if UNITY_2017_2_OR_NEWER
            XRSettings.enabled = true;
#endif
            Camera.SetupCurrent(currentCamera);

            if (viewEnabled != null)
                viewEnabled();
        }

        public void OnDisable()
        {
            if (viewDisabled != null)
                viewDisabled();

#if UNITY_2017_2_OR_NEWER
            XRSettings.enabled = false;
#endif

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
            var cameraTransform = m_Camera.transform;
#if UNITY_2017_2_OR_NEWER
            cameraTransform.localPosition = InputTracking.GetLocalPosition(XRNode.Head);
            cameraTransform.localRotation = InputTracking.GetLocalRotation(XRNode.Head);
#endif
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
#if UNITY_2017_2_OR_NEWER
            XRSettings.showDeviceView = !customPreviewCamera && m_ShowDeviceView;
#endif
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

            Event e = Event.current;
            if (m_ShowDeviceView)
            {
                if (e.type == EventType.Repaint)
                {
                    GL.sRGBWrite = (QualitySettings.activeColorSpace == ColorSpace.Linear);
                    var renderTexture = customPreviewCamera && customPreviewCamera.targetTexture ? customPreviewCamera.targetTexture : m_TargetTexture;
                    GUI.DrawTexture(guiRect, renderTexture, ScaleMode.StretchToFill, false);
                    GL.sRGBWrite = false;
                }
            }

            m_ToggleDeviceViewRect.width = width;
            m_PresentationCameraRect.y = height - m_PresentationCameraRect.height;

            if (GUI.Button(m_ToggleDeviceViewRect, "Toggle Device View", EditorStyles.toolbarButton))
                m_ShowDeviceView = !m_ShowDeviceView;

            if (m_CustomPreviewCamera)
                m_UseCustomPreviewCamera = GUI.Toggle(m_PresentationCameraRect, m_UseCustomPreviewCamera, "Use Presentation Camera");

            if (afterOnGUI != null)
                afterOnGUI(this);
        }

        void DoDrawCamera(Rect rect)
        {
            if (!m_Camera.gameObject.activeInHierarchy)
                return;

#if UNITY_2017_2_OR_NEWER
            if (!XRDevice.isPresent)
                return;
#endif

            UnityEditor.Handles.DrawCamera(rect, m_Camera, m_RenderMode);
            if (Event.current.type == EventType.Repaint)
            {
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

        static bool GetIsUserPresent()
        {
#if UNITY_2017_2_OR_NEWER
#if ENABLE_OVR_INPUT
            if (XRSettings.loadedDeviceName == "Oculus")
                return OVRPlugin.userPresent;
#endif
#if ENABLE_STEAMVR_INPUT
            if (XRSettings.loadedDeviceName == "OpenVR")
                return OpenVR.System.GetTrackedDeviceActivityLevel(0) == EDeviceActivityLevel.k_EDeviceActivityLevel_UserInteraction;
#endif
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
