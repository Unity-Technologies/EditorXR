using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.XR;

namespace UnityEditor.Experimental.EditorVR.Core
{
    [CreateAssetMenu(menuName = "EditorXR/Editing Context")]
    class EditorVRContext : ScriptableObject, IEditingContext
    {
        [SerializeField]
        float m_RenderScale = 1f;

        [SerializeField]
        bool m_CopyMainCameraSettings = true;

        [SerializeField]
        bool m_CopyMainCameraImageEffectsToHmd;

        [SerializeField]
        bool m_CopyMainCameraImageEffectsToPresentationCamera;

#if UNITY_EDITOR
        [SerializeField]
        internal List<MonoScript> m_DefaultToolStack;
#endif

        [SerializeField]
        [HideInInspector]
        List<string> m_DefaultToolStackNames;

        EditorVR m_Instance;

        public bool copyMainCameraSettings { get { return m_CopyMainCameraSettings; } }

        public bool copyMainCameraImageEffectsToHMD { get { return m_CopyMainCameraImageEffectsToHmd; } }

        public bool copyMainCameraImageEffectsToPresentationCamera { get { return m_CopyMainCameraImageEffectsToPresentationCamera; } }

        public bool instanceExists { get { return m_Instance != null; } }

        public void Setup()
        {
            EditorVR.defaultTools = m_DefaultToolStackNames.Select(t => Type.GetType(t)).ToArray();
            m_Instance = ObjectUtils.CreateGameObjectWithComponent<EditorVR>();
            if (Application.isPlaying)
            {
                var camera = CameraUtils.GetMainCamera();
                var cameraRig = m_Instance.transform;
                VRView.CreateCameraRig(ref camera, ref cameraRig);
            }

            XRSettings.eyeTextureResolutionScale = m_RenderScale;
        }

        public void Dispose()
        {
            m_Instance.Shutdown(); // Give a chance for dependent systems (e.g. serialization) to shut-down before destroying
            if (m_Instance)
                ObjectUtils.Destroy(m_Instance.gameObject);
            m_Instance = null;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            m_DefaultToolStackNames = m_DefaultToolStack.Select(ms => ms.GetClass().AssemblyQualifiedName).ToList();
        }
#endif
    }
}

