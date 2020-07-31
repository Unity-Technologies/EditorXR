using System;
using System.Collections.Generic;
using System.Linq;
using Unity.EditorXR.Utilities;
using Unity.XRTools.ModuleLoader;
using Unity.XRTools.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR;

namespace Unity.EditorXR.Core
{
    [CreateAssetMenu(menuName = "EditorXR/Editing Context")]
    class EditorXRContext : ScriptableObject, IEditingContext
    {
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

        EditorXR m_Instance;
        Transform m_CameraRig;

        public bool copyMainCameraSettings { get { return m_CopyMainCameraSettings; } }

        public bool copyMainCameraImageEffectsToHMD { get { return m_CopyMainCameraImageEffectsToHmd; } }

        public bool copyMainCameraImageEffectsToPresentationCamera { get { return m_CopyMainCameraImageEffectsToPresentationCamera; } }

        public bool instanceExists { get { return m_Instance != null; } }

        public void Setup()
        {
#if UNITY_EDITOR
            SetupMonoScriptTypeNames();
#endif

            EditorXR.DefaultMenu = GetTypeSafe(m_DefaultMainMenuName);
            EditorXR.DefaultAlternateMenu = GetTypeSafe(m_DefaultAlternateMenuName);

            if (m_DefaultToolStackNames != null)
                EditorXR.DefaultTools = m_DefaultToolStackNames.Select(GetTypeSafe).ToArray();

            if (m_HiddenTypeNames != null)
                EditorXR.HiddenTypes = m_HiddenTypeNames.Select(GetTypeSafe).ToArray();

            if (Application.isPlaying)
            {
                var camera = CameraUtils.GetMainCamera();
                VRView.CreateCameraRig(ref camera, out m_CameraRig);
            }

            m_Instance = ModuleLoaderCore.instance.GetModule<EditorXR>();
            if (m_Instance == null)
            {
                Debug.LogWarning("EditorXR Module not loaded");
                return;
            }

            m_Instance.Initialize();

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
            if (m_Instance == null)
                return;

            if (m_CameraRig && Application.isPlaying)
                UnityObjectUtils.Destroy(m_CameraRig.gameObject);

            m_Instance.Shutdown(); // Give a chance for dependent systems (e.g. serialization) to shut-down before destroying
            m_Instance = null;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            SetupMonoScriptTypeNames();
        }

        void SetupMonoScriptTypeNames()
        {
            const string warningString = "Could not get class for MonoScript: {0}";
            if (m_DefaultMainMenu)
            {
                var defaultMenuType = m_DefaultMainMenu.GetClass();
                if (defaultMenuType == null)
                    Debug.LogWarningFormat(warningString, AssetDatabase.GetAssetPath(m_DefaultMainMenu));
                else
                    m_DefaultMainMenuName = defaultMenuType.AssemblyQualifiedName;
            }

            if (m_DefaultAlternateMenu)
            {
                var defaultAlternateMenuType = m_DefaultAlternateMenu.GetClass();
                if (defaultAlternateMenuType == null)
                    Debug.LogWarningFormat(warningString, AssetDatabase.GetAssetPath(m_DefaultAlternateMenu));
                else
                    m_DefaultAlternateMenuName = defaultAlternateMenuType.AssemblyQualifiedName;
            }

            if (m_DefaultToolStack != null)
            {
                m_DefaultToolStackNames = new List<string>();
                foreach (var defaultToolType in m_DefaultToolStack)
                {
                    var defaultToolClass = defaultToolType.GetClass();
                    if (defaultToolClass == null)
                    {
                        Debug.LogWarningFormat(warningString, AssetDatabase.GetAssetPath(defaultToolType));
                        continue;
                    }

                    m_DefaultToolStackNames.Add(defaultToolClass.AssemblyQualifiedName);
                }
            }

            if (m_HiddenTypes != null)
            {
                m_HiddenTypeNames = new List<string>();
                foreach (var hiddenType in m_HiddenTypes)
                {
                    var hiddenTypeClass = hiddenType.GetClass();
                    if (hiddenTypeClass == null)
                    {
                        Debug.LogWarningFormat(warningString, AssetDatabase.GetAssetPath(hiddenType));
                        continue;
                    }

                    m_HiddenTypeNames.Add(hiddenTypeClass.AssemblyQualifiedName);
                }
            }
        }

        // ReSharper disable once UnusedMember.Local
        static void PreferencesGUI()
        {
            EditorGUILayout.BeginVertical();

            // Show EditorXR GameObjects
            using (var changed = new EditorGUI.ChangeCheckScope())
            {
                const string title = "Show EditorXR GameObjects";
                const string tooltip = "Normally, EditorXR GameObjects are hidden in the Hierarchy. Would you like to show them?";

                var debugSettings = ModuleLoaderDebugSettings.instance;
                var hideFlags = debugSettings.moduleHideFlags;
                var showGameObjects = (hideFlags & HideFlags.HideInHierarchy) == 0;
                showGameObjects = EditorGUILayout.Toggle(new GUIContent(title, tooltip), showGameObjects);
                if (changed.changed)
                    debugSettings.SetModuleHideFlags(showGameObjects ? hideFlags & ~HideFlags.HideInHierarchy : hideFlags | HideFlags.HideInHierarchy);
            }

            // Preserve Layout
            {
                const string title = "Preserve Layout";
                const string tooltip = "Check this to preserve your layout and location in EditorXR";
                EditorXR.preserveLayout = EditorGUILayout.Toggle(new GUIContent(title, tooltip), EditorXR.preserveLayout);
            }

            // Include in Builds
            {
                const string title = "Include in Player Builds";
                const string tooltip = "Normally, EditorXR will only be available in the editor. Check this if you would like to modify its assembly definitions and include it in player builds";
                EditorXR.includeInBuilds = EditorGUILayout.Toggle(new GUIContent(title, tooltip), EditorXR.includeInBuilds);
            }

            if (GUILayout.Button("Reset to Defaults", GUILayout.Width(140)))
                EditorXR.ResetPreferences();

            EditorGUILayout.EndVertical();
        }
#endif
    }
}
