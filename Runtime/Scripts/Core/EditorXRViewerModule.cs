using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Labs.EditorXR.Helpers;
using Unity.Labs.EditorXR.Interfaces;
using Unity.Labs.EditorXR.Modules;
using Unity.Labs.EditorXR.Utilities;
using Unity.Labs.ModuleLoader;
using Unity.Labs.SpatialHash;
using Unity.Labs.Utils;
using UnityEngine;
using UnityEngine.XR;

namespace Unity.Labs.EditorXR.Core
{
    class EditorXRViewerModule : ScriptableSettings<EditorXRViewerModule>,
        IModuleDependency<EditorXRDirectSelectionModule>, IInterfaceConnector,
        ISerializePreferences, IUsesConnectInterfaces, IDelayedInitializationModule, IModuleBehaviorCallbacks,
        IUsesFunctionalityInjection, IProvidesViewerScale, IProvidesViewerBody, IProvidesMoveCameraRig,
        IProvidesGetVRPlayerObjects, IUsesSpatialHash
    {
        [Serializable]
        class Preferences
        {
            [SerializeField]
            Vector3 m_CameraPosition;

            [SerializeField]
            Quaternion m_CameraRotation;

            [SerializeField]
            float m_CameraRigScale = 1;

            public Vector3 cameraPosition
            {
                get { return m_CameraPosition; }
                set { m_CameraPosition = value; }
            }

            public Quaternion cameraRotation
            {
                get { return m_CameraRotation; }
                set { m_CameraRotation = value; }
            }

            public float cameraRigScale
            {
                get { return m_CameraRigScale; }
                set { m_CameraRigScale = value; }
            }
        }

        const float k_CameraRigTransitionTime = 0.25f;

        const string k_WorldScaleProperty = "_WorldScale";

        // Local method use only -- created here to reduce garbage collection
        const int k_MaxCollisionCheck = 32;
        static Collider[] s_CachedColliders = new Collider[k_MaxCollisionCheck];

#pragma warning disable 649
        [SerializeField]
        bool m_UsePlayerFloor = true;

        [SerializeField]
        bool m_UsePlayerModel = true;

        // TODO: Remove this option or provide warnings if it is used
        [SerializeField]
        bool m_InitializeCamera = true;

        [SerializeField]
        GameObject m_PlayerModelPrefab;

        [SerializeField]
        GameObject m_PlayerFloorPrefab;

        [SerializeField]
        GameObject m_PreviewCameraPrefab;
#pragma warning restore 649

        PlayerBody m_PlayerBody;
        GameObject m_PlayerFloor;

        bool m_CameraInitialized;
        float m_OriginalNearClipPlane;
        float m_OriginalFarClipPlane;
        readonly List<GameObject> m_VRPlayerObjects = new List<GameObject>();

        readonly Preferences m_Preferences = new Preferences();
        EditorXRDirectSelectionModule m_DirectSelectionModule;

        public int initializationOrder { get { return -2; } }
        public int shutdownOrder { get { return 2; } }
        public int connectInterfaceOrder { get { return 0; } }

        internal IPreviewCamera customPreviewCamera { get; private set; }

        public bool hmdReady { get; private set; }

#if !FI_AUTOFILL
        IProvidesFunctionalityInjection IFunctionalitySubscriber<IProvidesFunctionalityInjection>.provider { get; set; }
        IProvidesConnectInterfaces IFunctionalitySubscriber<IProvidesConnectInterfaces>.provider { get; set; }
        IProvidesSpatialHash IFunctionalitySubscriber<IProvidesSpatialHash>.provider { get; set; }
#endif

        public void ConnectDependency(EditorXRDirectSelectionModule dependency)
        {
            m_DirectSelectionModule = dependency;
        }

        public void LoadModule()
        {
#if UNITY_EDITOR
            VRView.hmdStatusChange += OnHMDStatusChange;
#endif

            Shader.SetGlobalFloat(k_WorldScaleProperty, 1);
        }

        public void UnloadModule()
        {
#if UNITY_EDITOR
            VRView.hmdStatusChange -= OnHMDStatusChange;
#endif
        }

        public void ConnectInterface(object target, object userData = null)
        {
            var usesCameraRig = target as IUsesCameraRig;
            if (usesCameraRig != null)
                usesCameraRig.cameraRig = CameraUtils.GetCameraRig();
        }

        public void DisconnectInterface(object target, object userData = null) { }

        public object OnSerializePreferences()
        {
            if (!EditorXR.preserveLayout)
                return null;

            if (hmdReady)
                SaveCameraState();

            return m_Preferences;
        }

        void OnHMDStatusChange(bool ready)
        {
            hmdReady = ready;
            if (!ready)
                SaveCameraState();
        }

        void SaveCameraState()
        {
            var camera = CameraUtils.GetMainCamera();
            var cameraRig = CameraUtils.GetCameraRig();
            if (!cameraRig || !camera)
                return;

            var cameraTransform = camera.transform;
            var cameraRigScale = cameraRig.localScale.x;
            m_Preferences.cameraRigScale = cameraRigScale;
            m_Preferences.cameraPosition = cameraTransform.position;
            m_Preferences.cameraRotation = cameraTransform.rotation.ConstrainYaw();
        }

        public void OnDeserializePreferences(object obj)
        {
            if (!EditorXR.preserveLayout)
                return;

            var preferences = (Preferences)obj;

            var camera = CameraUtils.GetMainCamera();
            var cameraRig = CameraUtils.GetCameraRig();
            var cameraTransform = camera.transform;
            var cameraRotation = cameraTransform.rotation.ConstrainYaw();
            var inverseRotation = Quaternion.Inverse(cameraRotation);
            cameraRig.position = Vector3.zero;
            cameraRig.rotation = inverseRotation * preferences.cameraRotation;
            SetViewerScale(preferences.cameraRigScale);
            cameraRig.position = preferences.cameraPosition - cameraTransform.position;
        }

        void InitializeCamera()
        {
            var cameraRig = CameraUtils.GetCameraRig();
            cameraRig.parent = ModuleLoaderCore.instance.GetModuleParent().transform;
            var viewerCamera = CameraUtils.GetMainCamera();
            m_OriginalNearClipPlane = viewerCamera.nearClipPlane;
            m_OriginalFarClipPlane = viewerCamera.farClipPlane;
            if (XRSettings.loadedDeviceName == "OpenVR")
            {
                // Steam's reference position should be at the feet and not at the head as we do with Oculus
                cameraRig.localPosition = Vector3.zero;
            }

#if UNITY_EDITOR
            var hmdOnlyLayerMask = 0;
#endif
            if (!Application.isPlaying && m_PreviewCameraPrefab)
            {
                var go = EditorXRUtils.Instantiate(m_PreviewCameraPrefab);
                go.transform.SetParent(CameraUtils.GetCameraRig(), false);

#if UNITY_EDITOR
                customPreviewCamera = go.GetComponentInChildren<IPreviewCamera>();
                if (customPreviewCamera != null)
                {
                    VRView.customPreviewCamera = customPreviewCamera.previewCamera;
                    customPreviewCamera.vrCamera = VRView.viewerCamera;
                    hmdOnlyLayerMask = customPreviewCamera.hmdOnlyLayerMask;
                    this.ConnectInterfaces(customPreviewCamera);
                }
#endif
            }

#if UNITY_EDITOR
            VRView.cullingMask = UnityEditor.Tools.visibleLayers | hmdOnlyLayerMask;
#endif

            m_CameraInitialized = true;
        }

        void UpdateCamera()
        {
#if UNITY_EDITOR
            if (customPreviewCamera != null && customPreviewCamera as MonoBehaviour != null)
                customPreviewCamera.enabled = VRView.showDeviceView && VRView.customPreviewCamera != null;
#endif
        }

        void AddPlayerFloor()
        {
            m_PlayerFloor = EditorXRUtils.Instantiate(m_PlayerFloorPrefab, CameraUtils.GetCameraRig().transform, false);
            this.InjectFunctionalitySingle(m_PlayerFloor.GetComponent<PlayerFloor>());
            m_VRPlayerObjects.Add(m_PlayerFloor);
        }

        void AddPlayerModel()
        {
            m_PlayerBody = EditorXRUtils.Instantiate(m_PlayerModelPrefab, CameraUtils.GetMainCamera().transform, false).GetComponent<PlayerBody>();
            this.InjectFunctionalitySingle(m_PlayerBody);
            if (this.HasProvider<IProvidesSpatialHash>())
                this.AddRendererToSpatialHash(m_PlayerBody.GetComponent<Renderer>());

            var playerObjects = m_PlayerBody.GetComponentsInChildren<Renderer>(true);
            foreach (var playerObject in playerObjects)
            {
                m_VRPlayerObjects.Add(playerObject.gameObject);
            }

            var selectionModule = ModuleLoaderCore.instance.GetModule<SelectionModule>();
            if (selectionModule != null)
                selectionModule.standardIgnoreList.AddRange(m_VRPlayerObjects);
        }

        public bool IsOverShoulder(Transform rayOrigin)
        {
            if (m_PlayerBody == null)
                return false;

            return Overlaps(rayOrigin, m_PlayerBody.overShoulderTrigger);
        }

        public bool IsAboveHead(Transform rayOrigin)
        {
            if (m_PlayerBody == null)
                return false;

            return Overlaps(rayOrigin, m_PlayerBody.aboveHeadTrigger);
        }

        bool Overlaps(Transform rayOrigin, Collider trigger)
        {
            var radius = m_DirectSelectionModule.GetPointerLength(rayOrigin);

            var totalColliders = Physics.OverlapSphereNonAlloc(rayOrigin.position, radius, s_CachedColliders, -1, QueryTriggerInteraction.Collide);

            for (var colliderIndex = 0; colliderIndex < totalColliders; colliderIndex++)
            {
                if (s_CachedColliders[colliderIndex] == trigger)
                    return true;
            }

            return false;
        }

        internal void DropPlayerHead(Transform playerHead)
        {
            var cameraRig = CameraUtils.GetCameraRig();
            var mainCamera = CameraUtils.GetMainCamera().transform;

            // Hide player head to avoid jarring impact
            var playerHeadRenderers = playerHead.GetComponentsInChildren<Renderer>();
            foreach (var renderer in playerHeadRenderers)
            {
                renderer.enabled = false;
            }

            var rotationDiff = (Quaternion.Inverse(mainCamera.rotation) * playerHead.rotation).ConstrainYaw();
            var cameraDiff = cameraRig.position - mainCamera.position;
            cameraDiff.y = 0;
            var rotationOffset = rotationDiff * cameraDiff - cameraDiff;

            var endPosition = cameraRig.position + (playerHead.position - mainCamera.position) + rotationOffset;
            var endRotation = cameraRig.rotation * rotationDiff;
            var viewDirection = endRotation * Vector3.forward;

            EditorMonoBehaviour.instance.StartCoroutine(UpdateCameraRig(endPosition, viewDirection, () =>
            {
                playerHead.parent = mainCamera;
                playerHead.localRotation = Quaternion.identity;
                playerHead.localPosition = Vector3.zero;

                foreach (var renderer in playerHeadRenderers)
                {
                    renderer.enabled = true;
                }
            }));
        }

        static IEnumerator UpdateCameraRig(Vector3 position, Vector3? viewDirection, Action onComplete = null)
        {
            var cameraRig = CameraUtils.GetCameraRig();

            var startPosition = cameraRig.position;
            var startRotation = cameraRig.rotation;

            var rotation = startRotation;
            if (viewDirection.HasValue)
            {
                var direction = viewDirection.Value;
                direction.y = 0;
                rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            }

            var diffTime = 0f;
            var startTime = Time.realtimeSinceStartup;
            while (diffTime < k_CameraRigTransitionTime)
            {
                var t = diffTime / k_CameraRigTransitionTime;

                // Use a Lerp instead of SmoothDamp for constant velocity (avoid motion sickness)
                cameraRig.position = Vector3.Lerp(startPosition, position, t);
                cameraRig.rotation = Quaternion.Lerp(startRotation, rotation, t);
                yield return null;
                diffTime = Time.realtimeSinceStartup - startTime;
            }

            cameraRig.position = position;
            cameraRig.rotation = rotation;

            if (onComplete != null)
                onComplete();
        }

        public void MoveCameraRig(Vector3 position, Vector3? viewDirection)
        {
            EditorMonoBehaviour.instance.StartCoroutine(UpdateCameraRig(position, viewDirection));
        }

        public float GetViewerScale()
        {
            var cameraRig = CameraUtils.GetCameraRig();
            if (!cameraRig)
                return 1;

            return cameraRig.localScale.x;
        }

        public void SetViewerScale(float scale)
        {
            var camera = CameraUtils.GetMainCamera();
            CameraUtils.GetCameraRig().localScale = Vector3.one * scale;
            Shader.SetGlobalFloat(k_WorldScaleProperty, 1f / scale);
            if (m_CameraInitialized)
            {
                camera.nearClipPlane = m_OriginalNearClipPlane * scale;
                camera.farClipPlane = m_OriginalFarClipPlane * scale;
            }
            else
            {
                Debug.LogWarning("Premature use of SetViewerScale");
            }
        }

        public List<GameObject> GetVRPlayerObjects() { return m_VRPlayerObjects; }

        public void Initialize()
        {
            m_VRPlayerObjects.Clear();

            if (m_InitializeCamera)
                InitializeCamera();

            if (m_UsePlayerFloor)
                AddPlayerFloor();

            if (m_UsePlayerModel)
                AddPlayerModel();
        }

        public void Shutdown()
        {
            m_CameraInitialized = false;
            m_OriginalNearClipPlane = 0;
            m_OriginalFarClipPlane = 0;
            hmdReady = false;

            foreach (var playerObject in m_VRPlayerObjects)
            {
                UnityObjectUtils.Destroy(playerObject);
            }

            m_VRPlayerObjects.Clear();

            var cameraRig = CameraUtils.GetCameraRig();
            if (cameraRig)
                cameraRig.transform.parent = null;

            if (customPreviewCamera != null && customPreviewCamera as MonoBehaviour != null)
                UnityObjectUtils.Destroy(((MonoBehaviour)customPreviewCamera).gameObject);
        }

        public void OnBehaviorAwake() { }

        public void OnBehaviorEnable() { }

        public void OnBehaviorStart() { }

        public void OnBehaviorUpdate()
        {
            UpdateCamera();
        }

        public void OnBehaviorDisable() { }

        public void OnBehaviorDestroy() { }

        public void LoadProvider() { }

        public void ConnectSubscriber(object obj)
        {
#if !FI_AUTOFILL
            var viewerScaleSubscriber = obj as IFunctionalitySubscriber<IProvidesViewerScale>;
            if (viewerScaleSubscriber != null)
                viewerScaleSubscriber.provider = this;

            var viewerBodySubscriber = obj as IFunctionalitySubscriber<IProvidesViewerBody>;
            if (viewerBodySubscriber != null)
                viewerBodySubscriber.provider = this;

            var moveCameraRigSubscriber = obj as IFunctionalitySubscriber<IProvidesMoveCameraRig>;
            if (moveCameraRigSubscriber != null)
                moveCameraRigSubscriber.provider = this;

            var getVRPlayerObjectsSubscriber = obj as IFunctionalitySubscriber<IProvidesGetVRPlayerObjects>;
            if (getVRPlayerObjectsSubscriber != null)
                getVRPlayerObjectsSubscriber.provider = this;
#endif
        }

        public void UnloadProvider() { }
    }
}
