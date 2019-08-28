#if UNITY_2018_3_OR_NEWER
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.XR;

#if UNITY_EDITOR
using UnityEditor.Compilation;
using UnityEditorInternal;
#endif

namespace UnityEditor.Experimental.EditorVR.Core
{
    [CreateAssetMenu(menuName = "EditorXR/Editing Context")]
    class EditorXRContext : ScriptableObject, IEditingContext
    {
        [Serializable]
        class AssemblyDefinition
        {
            [SerializeField]
            string name;

            [SerializeField]
            string[] references;

            [SerializeField]
            string[] optionalUnityReferences;

            [SerializeField]
            string[] includePlatforms;

            [SerializeField]
            string[] excludePlatforms;

            [SerializeField]
            bool allowUnsafeCode;

            public string Name { get { return name; } set { name = value; } }
            public string[] IncludePlatforms { get { return includePlatforms; } set { includePlatforms = value; } }
            public string[] ExcludePlatforms { get { return excludePlatforms; } set { excludePlatforms = value; } }
        }

        static readonly string[] k_AssemblyNames = { "Unity.Labs.EditorXR", "input-prototype", "VRLR" };
        static readonly string[] k_IncludePlatformsEditorOnly = { "Editor" };
        static readonly string[] k_ExcludePlatformsEditorOnly = { };
        static readonly string[] k_IncludePlatformsInPlayer = { };
        static readonly string[] k_ExcludePlatformsInPlayer = { };

        static bool s_IncludeInPlayer;

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

            EditorVR.DefaultMenu = GetTypeSafe(m_DefaultMainMenuName);
            EditorVR.DefaultAlternateMenu = GetTypeSafe(m_DefaultAlternateMenuName);

            if (m_DefaultToolStackNames != null)
                EditorVR.DefaultTools = m_DefaultToolStackNames.Select(GetTypeSafe).ToArray();

            if (m_HiddenTypeNames != null)
                EditorVR.HiddenTypes = m_HiddenTypeNames.Select(GetTypeSafe).ToArray();

            if (Application.isPlaying)
            {
                var camera = CameraUtils.GetMainCamera();
                VRView.CreateCameraRig(ref camera, out m_CameraRig);
            }

            m_Instance = ModuleLoaderCore.instance.GetModule<EditorVR>();
            if (m_Instance == null)
            {
                Debug.LogWarning("EditorVR Module not loaded");
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
        static EditorXRContext()
        {
            EditorApplication.delayCall += () =>
            {
                s_IncludeInPlayer = GetIncludeInPlayer();
            };
        }

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
                EditorVR.preserveLayout = EditorGUILayout.Toggle(new GUIContent(title, tooltip), EditorVR.preserveLayout);
            }

            // Include in Builds
            using (var changed = new EditorGUI.ChangeCheckScope())
            {
                const string title = "Include in Player Builds";
                const string tooltip = "Normally, EditorXR will only be available in the editor. Check this if you would like to modify its assembly definitions and include it in player builds";
                s_IncludeInPlayer = EditorGUILayout.Toggle(new GUIContent(title, tooltip), s_IncludeInPlayer);
                if (changed.changed)
                    SetIncludeInPlayer(s_IncludeInPlayer);
            }

            if (GUILayout.Button("Reset to Defaults", GUILayout.Width(140)))
                EditorVR.ResetPreferences();

            EditorGUILayout.EndVertical();
        }

        static bool GetIncludeInPlayer()
        {
            var editorOnly = false;
            ForEachAssembly(asmDef =>
            {
                editorOnly |= asmDef.IncludePlatforms.Contains("Editor");
            });

            return !editorOnly;
        }

        static void SetIncludeInPlayer(bool include)
        {
            ForEachAssembly(asmDef =>
            {
                asmDef.IncludePlatforms = include ? k_IncludePlatformsInPlayer : k_IncludePlatformsEditorOnly;
                asmDef.ExcludePlatforms = include ? k_ExcludePlatformsInPlayer : k_ExcludePlatformsEditorOnly;
            });

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        }

        static void ForEachAssembly(Action<AssemblyDefinition> callback)
        {
            foreach (var assembly in k_AssemblyNames)
            {
                var path = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(assembly);
                if (string.IsNullOrEmpty(path))
                {
                    Debug.LogWarningFormat("Error in EditorXR Pre-Build action: Cannot find asmdef for assembly: {0}", assembly);
                    continue;
                }

                var asmDefAsset = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(path);
                if (asmDefAsset == null)
                {
                    Debug.LogWarningFormat("Error in EditorXR Pre-Build action: Cannot load asmdef at: {0}", path);
                    continue;
                }

                var asmDef = JsonUtility.FromJson<AssemblyDefinition>(asmDefAsset.text);
                callback(asmDef);
                File.WriteAllText(path, JsonUtility.ToJson(asmDef, true));
            }
        }
#endif
    }
}
#endif
