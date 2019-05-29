#if UNITY_2018_3_OR_NEWER
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Labs.Utils;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.XR;

namespace UnityEditor.Experimental.EditorVR.Core
{
    [CreateAssetMenu(menuName = "EditorXR/Editing Context")]
    class EditorXRContext : ScriptableObject, IEditingContext
    {
        static EditorVR s_Instance; // Used only by PreferencesGUI

#pragma warning disable 649
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
        MonoScript m_DefaultMainMenu;

        [SerializeField]
        MonoScript m_DefaultAlternateMenu;

        [SerializeField]
        internal List<MonoScript> m_DefaultToolStack;

        [SerializeField]
        List<MonoScript> m_HiddenTypes;
#endif

        [SerializeField]
        [HideInInspector]
        string m_DefaultMainMenuName;

        [SerializeField]
        [HideInInspector]
        string m_DefaultAlternateMenuName;

        [SerializeField]
        [HideInInspector]
        List<string> m_DefaultToolStackNames;

        [SerializeField]
        [HideInInspector]
        List<string> m_HiddenTypeNames;
#pragma warning restore 649

        EditorVR m_Instance;

        public bool copyMainCameraSettings { get { return m_CopyMainCameraSettings; } }

        public bool copyMainCameraImageEffectsToHMD { get { return m_CopyMainCameraImageEffectsToHmd; } }

        public bool copyMainCameraImageEffectsToPresentationCamera { get { return m_CopyMainCameraImageEffectsToPresentationCamera; } }

        public bool instanceExists { get { return m_Instance != null; } }

        public void Setup()
        {
#if UNITY_EDITOR
            SetupMonoScriptTypeNames();
#endif

            EditorVR.DefaultMenu = GetTypeSafe(m_DefaultMainMenuName);
            EditorVR.DefaultAlternateMenu = GetTypeSafe(m_DefaultAlternateMenuName);

            if (m_DefaultToolStackNames != null)
                EditorVR.DefaultTools = m_DefaultToolStackNames.Select(GetTypeSafe).ToArray();

            if (m_HiddenTypeNames != null)
                EditorVR.HiddenTypes = m_HiddenTypeNames.Select(GetTypeSafe).ToArray();

            var editorVRs = Resources.FindObjectsOfTypeAll<EditorVR>();
            if (editorVRs.Length == 0)
            {
                Debug.LogWarning("EditorVR Module not loaded");
                return;
            }

            s_Instance = m_Instance = editorVRs[0];
            m_Instance.enabled = true;

            if (Application.isPlaying)
            {
                var camera = CameraUtils.GetMainCamera();
                var cameraRig = m_Instance.transform;
                VRView.CreateCameraRig(ref camera, ref cameraRig);
            }

            XRSettings.eyeTextureResolutionScale = m_RenderScale;
        }

        static Type GetTypeSafe(string name)
        {
            if (!string.IsNullOrEmpty(name))
                return Type.GetType(name);

            return null;
        }

        public void Dispose()
        {
            m_Instance.Shutdown(); // Give a chance for dependent systems (e.g. serialization) to shut-down before destroying
            if (m_Instance)
                m_Instance.enabled = false;

            s_Instance = m_Instance = null;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            SetupMonoScriptTypeNames();
        }

        void SetupMonoScriptTypeNames()
        {
            if (m_DefaultMainMenu)
                m_DefaultMainMenuName = m_DefaultMainMenu.GetClass().AssemblyQualifiedName;

            if (m_DefaultAlternateMenu)
                m_DefaultAlternateMenuName = m_DefaultAlternateMenu.GetClass().AssemblyQualifiedName;

            if (m_DefaultToolStack != null)
                m_DefaultToolStackNames = m_DefaultToolStack.Select(ms => ms.GetClass().AssemblyQualifiedName).ToList();

            if (m_HiddenTypes != null)
                m_HiddenTypeNames = m_HiddenTypes.Select(ms => ms.GetClass().AssemblyQualifiedName).ToList();
        }

        static void PreferencesGUI()
        {
            EditorGUILayout.BeginVertical();

            // Show EditorXR GameObjects
            {
                const string title = "Show EditorXR GameObjects";
                const string tooltip = "Normally, EditorXR GameObjects are hidden in the Hierarchy. Would you like to show them?";

                EditorGUI.BeginChangeCheck();
                EditorVR.showGameObjects = EditorGUILayout.Toggle(new GUIContent(title, tooltip), EditorVR.showGameObjects);
                if (EditorGUI.EndChangeCheck() && s_Instance)
                    s_Instance.SetHideFlags(EditorVR.defaultHideFlags);
            }

            // Preserve Layout
            {
                const string title = "Preserve Layout";
                const string tooltip = "Check this to preserve your layout and location in EditorXR";
                EditorVR.preserveLayout = EditorGUILayout.Toggle(new GUIContent(title, tooltip), EditorVR.preserveLayout);
            }

            // Include in Builds
            {
                const string title = "Include in Player Builds";
                const string tooltip = "Normally, EditorXR will override its assembly definitions to keep its assemblies out of Player builds. Check this if you would like to skip this step and include EditorXR in Player builds";
                EditorVR.includeInBuilds = EditorGUILayout.Toggle(new GUIContent(title, tooltip), EditorVR.includeInBuilds);
            }

            if (GUILayout.Button("Reset to Defaults", GUILayout.Width(140)))
                EditorVR.ResetPreferences();

            EditorGUILayout.EndVertical();
        }
#endif
    }
}
#endif
