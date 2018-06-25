#if UNITY_EDITOR && UNITY_2017_2_OR_NEWER
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.XR;

namespace UnityEditor.Experimental.EditorVR.Core
{
    [CreateAssetMenu(menuName = "EditorVR/EditorVR Context")]
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

        [SerializeField]
        internal List<MonoScript> m_DefaultToolStack;

        EditorVR m_Instance;

        public bool copyMainCameraSettings { get { return m_CopyMainCameraSettings; } }

        public bool copyMainCameraImageEffectsToHMD { get { return m_CopyMainCameraImageEffectsToHmd; } }

        public bool copyMainCameraImageEffectsToPresentationCamera { get { return m_CopyMainCameraImageEffectsToPresentationCamera; } }

        public bool instanceExists { get { return m_Instance != null; } }

        public void Setup()
        {
            EditorVR.defaultTools = m_DefaultToolStack.Select(ms => ms.GetClass()).ToArray();
            m_Instance = ObjectUtils.CreateGameObjectWithComponent<EditorVR>();
            XRSettings.eyeTextureResolutionScale = m_RenderScale;
        }

        public void Dispose()
        {
            m_Instance.Shutdown(); // Give a chance for dependent systems (e.g. serialization) to shut-down before destroying
            ObjectUtils.Destroy(m_Instance.gameObject);
            m_Instance = null;
        }
    }
}
#endif
