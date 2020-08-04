using System.Collections.Generic;
using Unity.EditorXR.Core;
using Unity.XRTools.Utils;
using UnityEngine;

namespace Unity.EditorXR.Helpers
{
#if UNITY_EDITOR
    /// <summary>
    /// A preview camera that provides for smoothing of the position and look vector
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [RequiresLayer(k_HMDOnlyLayer)]
    sealed class VRSmoothCamera : MonoBehaviour, IPreviewCamera
    {
        static readonly List<bool> k_HiddenEnabled = new List<bool>();
        const string k_HMDOnlyLayer = "HMDOnly";
        static readonly Rect k_DefaultCameraRect = new Rect(0f, 0f, 1f, 1f);

#pragma warning disable 649
        [SerializeField]
        Camera m_VRCamera;

        [SerializeField]
        int m_TargetDisplay;

        [Range(1, 180)]
        [SerializeField]
        int m_FieldOfView = 40;

        [SerializeField]
        float m_PullBackDistance = 0.8f;

        [SerializeField]
        float m_SmoothingMultiplier = 3;
#pragma warning restore 649

        Camera m_SmoothCamera;

        RenderTexture m_RenderTexture;

        Vector3 m_Position;
        Quaternion m_Rotation;
        int m_HMDOnlyLayerMask;

        /// <summary>
        /// The camera drawing the preview
        /// </summary>
        public Camera previewCamera { get { return m_SmoothCamera; } }

        /// <summary>
        /// The actual HMD camera (will be provided by system)
        /// The VRView's camera, whose settings are copied by the SmoothCamera
        /// </summary>
        public Camera vrCamera
        {
            private get { return m_VRCamera; }
            set { m_VRCamera = value; }
        }

        /// <summary>
        /// A layer mask that controls what will always render in the HMD and not in the preview
        /// </summary>
        public int hmdOnlyLayerMask { get { return m_HMDOnlyLayerMask; } }

        // Local method use only -- created here to reduce garbage collection
        static readonly List<Renderer> k_Renderers = new List<Renderer>();

        void Awake()
        {
            m_SmoothCamera = GetComponent<Camera>();
            m_SmoothCamera.enabled = false;
            m_HMDOnlyLayerMask = LayerMask.GetMask(k_HMDOnlyLayer);
        }

        void Start()
        {
            transform.position = m_VRCamera.transform.localPosition;
            transform.localRotation = m_VRCamera.transform.localRotation;

            m_Position = transform.localPosition;
            m_Rotation = transform.localRotation;
        }

        void OnEnable()
        {
            // Snap camera to starting position
            if (m_VRCamera)
            {
                m_Rotation = m_VRCamera.transform.localRotation;
                m_Position = m_VRCamera.transform.localPosition;
            }
        }

        void LateUpdate()
        {
            m_SmoothCamera.CopyFrom(m_VRCamera); // This copies the transform as well
            var vrCameraTexture = m_VRCamera.targetTexture;
#if UNITY_EDITOR
            if (vrCameraTexture && (!m_RenderTexture || m_RenderTexture.width != vrCameraTexture.width || m_RenderTexture.height != vrCameraTexture.height))
            {
                var cameraRect = new Rect(0f, 0f, vrCameraTexture.width, vrCameraTexture.height);
                VRView.activeView.CreateCameraTargetTexture(ref m_RenderTexture, cameraRect, false);
                m_RenderTexture.name = "Smooth Camera RT";
            }
#endif

            m_SmoothCamera.targetTexture = m_RenderTexture;
            m_SmoothCamera.targetDisplay = m_TargetDisplay;
            m_SmoothCamera.cameraType = CameraType.Game;
            m_SmoothCamera.cullingMask &= ~hmdOnlyLayerMask;
            m_SmoothCamera.rect = k_DefaultCameraRect;
            m_SmoothCamera.stereoTargetEye = StereoTargetEyeMask.None;
            m_SmoothCamera.fieldOfView = m_FieldOfView;

            m_Position = Vector3.Lerp(m_Position, m_VRCamera.transform.localPosition, Time.deltaTime * m_SmoothingMultiplier);
            m_Rotation = Quaternion.Slerp(m_Rotation, m_VRCamera.transform.localRotation, Time.deltaTime * m_SmoothingMultiplier);

            transform.localRotation = Quaternion.LookRotation(m_Rotation * Vector3.forward, Vector3.up);
            transform.localPosition = m_Position - transform.localRotation * Vector3.forward * m_PullBackDistance;

            // Don't render any HMD-related visual proxies
            k_Renderers.Clear();
            m_VRCamera.GetComponentsInChildren(k_Renderers);
            var count = k_Renderers.Count;

            k_HiddenEnabled.Clear();
            for (var i = 0; i < count; i++)
            {
                var h = k_Renderers[i];
                k_HiddenEnabled.Add(h.enabled);
                h.enabled = false;
            }

            RenderTexture.active = m_SmoothCamera.targetTexture;
            m_SmoothCamera.Render();
            RenderTexture.active = null;

            for (var i = 0; i < count; i++)
            {
                k_Renderers[i].enabled = k_HiddenEnabled[i];
            }
        }
    }
#else
    sealed class VRSmoothCamera : MonoBehaviour
    {
#pragma warning disable 649
        [SerializeField]
        Camera m_VRCamera;

        [SerializeField]
        int m_TargetDisplay;

        [SerializeField]
        int m_FieldOfView;

        [SerializeField]
        float m_PullBackDistance;

        [SerializeField]
        float m_SmoothingMultiplier;
#pragma warning restore 649
    }
#endif
}
