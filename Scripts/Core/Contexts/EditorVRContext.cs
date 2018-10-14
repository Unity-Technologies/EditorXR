#if UNITY_EDITOR && UNITY_2017_2_OR_NEWER
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Menus;
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
        MonoScript m_DefaultMainMenu;

        [SerializeField]
        MonoScript m_DefaultAlternateMenu;

        [SerializeField]
        internal List<MonoScript> m_DefaultToolStack;

        [SerializeField]
        List<MonoScript> m_HiddenTypes;

        EditorVR m_Instance;
        static EditorVR s_Instance; // Used only by PreferencesGUI

        public bool copyMainCameraSettings { get { return m_CopyMainCameraSettings; } }

        public bool copyMainCameraImageEffectsToHMD { get { return m_CopyMainCameraImageEffectsToHmd; } }

        public bool copyMainCameraImageEffectsToPresentationCamera { get { return m_CopyMainCameraImageEffectsToPresentationCamera; } }

        public bool instanceExists { get { return m_Instance != null; } }

        public void Setup()
        {
            EditorVR.DefaultTools = m_DefaultToolStack.Select(ms => ms.GetClass()).ToArray();
            EditorVR.DefaultMenu = m_DefaultMainMenu ? m_DefaultMainMenu.GetClass() : null;
            EditorVR.DefaultAlternateMenu = m_DefaultAlternateMenu ? m_DefaultAlternateMenu.GetClass() : null;
            EditorVR.HiddenTypes = m_HiddenTypes.Select(ms => ms.GetClass()).ToArray();
            s_Instance = m_Instance = ObjectUtils.CreateGameObjectWithComponent<EditorVR>();
            XRSettings.eyeTextureResolutionScale = m_RenderScale;
        }

        public void Dispose()
        {
            m_Instance.Shutdown(); // Give a chance for dependent systems (e.g. serialization) to shut-down before destroying
            ObjectUtils.Destroy(m_Instance.gameObject);
            s_Instance = m_Instance = null;
        }

        static void PreferencesGUI()
        {
            EditorGUILayout.BeginVertical();

            // Show EditorVR GameObjects
            {
                string title = "Show EditorVR GameObjects";
                string tooltip = "Normally, EditorVR GameObjects are hidden in the Hierarchy. Would you like to show them?";

                EditorGUI.BeginChangeCheck();
                EditorVR.showGameObjects = EditorGUILayout.Toggle(new GUIContent(title, tooltip), EditorVR.showGameObjects);
                if (EditorGUI.EndChangeCheck() && s_Instance)
                    s_Instance.SetHideFlags(EditorVR.defaultHideFlags);
            }

            // Preserve Layout
            {
                string title = "Preserve Layout";
                string tooltip = "Check this to preserve your layout and location in EditorVR";
                EditorVR.preserveLayout = EditorGUILayout.Toggle(new GUIContent(title, tooltip), EditorVR.preserveLayout);
            }

            if (GUILayout.Button("Reset to Defaults", GUILayout.Width(140)))
                EditorVR.ResetPreferences();

            EditorGUILayout.EndVertical();
        }
    }
}
#endif
