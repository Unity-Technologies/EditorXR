
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputNew;
using System.IO;
using UnityObject = UnityEngine.Object;

namespace UnityEditor.Experimental.EditorVR.Core
{
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    sealed class EditingContextManager : MonoBehaviour
    {
        [SerializeField]
        UnityObject m_DefaultContext;

        internal const string k_SettingsPath = "ProjectSettings/EditingContextManagerSettings.asset";
        internal const string k_UserSettingsPath = "Library/EditingContextManagerSettings.asset";

        const string k_LaunchOnExitPlaymode = "EditingContextManager.LaunchOnExitPlaymode";

        IEditingContext m_CurrentContext;

        internal static EditingContextManager s_Instance;
        static InputManager s_InputManager;
        static List<IEditingContext> s_AvailableContexts;
        static EditingContextManagerSettings s_Settings;
        static UnityObject s_DefaultContext;

        string[] m_ContextNames;
        int m_SelectedContextIndex;

        Rect m_EditingContextPopupRect = new Rect(0, 0, 150, 20); // Y and X position will be set based on window size

        readonly List<IEditingContext> m_PreviousContexts = new List<IEditingContext>();

        internal static IEditingContext defaultContext
        {
            get
            {
                var availableContexts = GetAvailableEditingContexts();
                var context = availableContexts.Find(c => c.Equals(s_DefaultContext)) ?? availableContexts.First();

                var defaultContextName = settings.defaultContextName;
                if (!string.IsNullOrEmpty(defaultContextName))
                {
                    var foundContext = availableContexts.Find(c => c.name == defaultContextName);
                    if (foundContext != null)
                        context = foundContext;
                }

                return context;
            }
            set
            {
                settings.defaultContextName = value.name;
            }
        }

        internal IEditingContext currentContext
        {
            get { return m_CurrentContext; }
        }

        static EditingContextManagerSettings settings
        {
            get
            {
                if (!s_Settings)
                    s_Settings = LoadUserSettings();

                return s_Settings;
            }
        }

#if UNITY_EDITOR
        static EditingContextManager()
        {
            VRView.viewEnabled += OnVRViewEnabled;
            VRView.viewDisabled += OnVRViewDisabled;
            EditorApplication.update += ReopenOnExitPlaymode;
        }
#endif

        static void OnVRViewEnabled()
        {
            Resources.UnloadUnusedAssets();
            InitializeInputManager();
            if (!Application.isPlaying)
                s_Instance = ObjectUtils.CreateGameObjectWithComponent<EditingContextManager>();
        }

        static void OnVRViewDisabled()
        {
            ObjectUtils.Destroy(s_Instance.gameObject);
            if (s_InputManager)
                ObjectUtils.Destroy(s_InputManager.gameObject);
        }

#if UNITY_EDITOR
        [MenuItem("Window/EditorXR %e", false)]
        internal static void ShowEditorVR()
        {
            // Using a utility window improves performance by saving from the overhead of DockArea.OnGUI()
            EditorWindow.GetWindow<VRView>(true, "EditorXR", true);
        }

        [MenuItem("Window/EditorXR %e", true)]
        static bool ShouldShowEditorVR()
        {
            return PlayerSettings.virtualRealitySupported;
        }

        [MenuItem("Edit/Project Settings/EditorXR/Default Editing Context")]
        static void EditProjectSettings()
        {
            var settings = LoadProjectSettings();
            settings.name = "Default Editing Context";
            Selection.activeObject = settings;
        }

        // Life cycle management across playmode switches is an odd beast indeed, and there is a need to reliably relaunch
        // EditorVR after we switch back out of playmode (assuming the view was visible before a playmode switch). So,
        // we watch until playmode is done and then relaunch.
        static void ReopenOnExitPlaymode()
        {
            var launch = EditorPrefs.GetBool(k_LaunchOnExitPlaymode, false);
            if (!launch || !EditorApplication.isPlaying)
            {
                EditorPrefs.DeleteKey(k_LaunchOnExitPlaymode);
                EditorApplication.update -= ReopenOnExitPlaymode;
                if (launch)
                    EditorApplication.delayCall += ShowEditorVR;
            }
        }
#endif

#if UNITY_EDITOR && UNITY_2017_2_OR_NEWER
        static void OnPlayModeStateChanged(PlayModeStateChange stateChange)
        {
            if (stateChange == PlayModeStateChange.ExitingEditMode)
            {
                EditorPrefs.SetBool(k_LaunchOnExitPlaymode, true);
                var view = VRView.activeView;
                if (view)
                    view.Close();
            }
        }
#endif

        void OnEnable()
        {
            Debug.Log("OnEnable");
            ISetEditingContextMethods.getAvailableEditingContexts = GetAvailableEditingContexts;
            ISetEditingContextMethods.getPreviousEditingContexts = GetPreviousEditingContexts;
            ISetEditingContextMethods.setEditingContext = SetEditingContext;
            ISetEditingContextMethods.restorePreviousEditingContext = RestorePreviousContext;

#if UNITY_EDITOR
            if (runInEditMode)
            {
                // Force the window to repaint every tick, since we need live updating
                // This also allows scripts with [ExecuteInEditMode] to run
                EditorApplication.update += EditorApplication.QueuePlayerLoopUpdate;

                EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

                SetEditingContext(defaultContext);
            }
#endif
        }

        void OnApplicationQuit()
        {
            Debug.Log("Quitting");
        }

        void OnDisable()
        {
            if (Application.isPlaying)
            {
                OnVRViewDisabled();
            }
#if UNITY_EDITOR
            else
            {
                EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

                EditorApplication.update -= EditorApplication.QueuePlayerLoopUpdate;

                VRView.afterOnGUI -= OnVRViewGUI;
            }
#endif

            if (m_CurrentContext != null)
            {
                defaultContext = m_CurrentContext;
                m_CurrentContext.Dispose();
            }

            s_AvailableContexts = null;

            SetEditingContext(null);

            ISetEditingContextMethods.getAvailableEditingContexts = null;
            ISetEditingContextMethods.getPreviousEditingContexts = null;
            ISetEditingContextMethods.setEditingContext = null;
            ISetEditingContextMethods.restorePreviousEditingContext = null;

            SaveUserSettings(settings);
        }

        void Awake()
        {
            Debug.Log("Awake");
            s_DefaultContext = m_DefaultContext;

            var availableContexts = GetAvailableEditingContexts();
            m_ContextNames = availableContexts.Select(c => c.name).ToArray();

            if (s_AvailableContexts.Count == 0)
                throw new Exception("You can't start EditorXR without at least one context. Try re-importing the package or use version control to restore the default context asset");

#if UNITY_EDITOR
            if (s_AvailableContexts.Count > 1)
                VRView.afterOnGUI += OnVRViewGUI;
#endif

            if (Application.isPlaying)
            {
                OnVRViewEnabled();
                s_Instance = this;

                SetEditingContext(defaultContext);
            }
        }


#if UNITY_EDITOR
        void OnVRViewGUI(VRView view)
        {
            var position = view.position;
            m_EditingContextPopupRect.y = position.height - m_EditingContextPopupRect.height;
            m_EditingContextPopupRect.x = position.width - m_EditingContextPopupRect.width;

            m_SelectedContextIndex = EditorGUI.Popup(m_EditingContextPopupRect, string.Empty, m_SelectedContextIndex, m_ContextNames);
            if (GUI.changed)
            {
                SetEditingContext(s_AvailableContexts[m_SelectedContextIndex]);
                GUIUtility.ExitGUI();
            }
        }
#endif

        internal void SetEditingContext(IEditingContext context)
        {
            if (context == null)
                return;

            if (m_CurrentContext != null)
            {
                m_PreviousContexts.Insert(0, m_CurrentContext);

                if (m_CurrentContext.instanceExists)
                    m_CurrentContext.Dispose();
            }

            context.Setup();
            m_CurrentContext = context;

            m_SelectedContextIndex = s_AvailableContexts.IndexOf(context);
        }

        internal void RestorePreviousContext()
        {
            if (m_PreviousContexts.Count > 0)
                SetEditingContext(m_PreviousContexts.First());
        }

        public static List<IEditingContext> GetEditingContextAssets()
        {
#if UNITY_EDITOR
            var availableContexts = new List<IEditingContext>();
            var types = ObjectUtils.GetImplementationsOfInterface(typeof(IEditingContext));
            var searchString = "t: " + string.Join(" t: ", types.Select(t => t.FullName).ToArray());
            var assets = AssetDatabase.FindAssets(searchString);

            foreach (var asset in assets)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(asset);
                var context = AssetDatabase.LoadMainAssetAtPath(assetPath) as IEditingContext;
                availableContexts.Add(context);
            }
#else
            var availableContexts = DefaultScriptReferences.GetEditingContexts();
#endif

            return availableContexts;
        }

        internal static string[] GetEditingContextNames()
        {
            var availableContexts = GetEditingContextAssets();
            return availableContexts.Select(c => c.name).ToArray();
        }

        static List<IEditingContext> GetAvailableEditingContexts()
        {
            if (s_AvailableContexts == null)
                s_AvailableContexts = GetEditingContextAssets();

            return s_AvailableContexts;
        }

        List<IEditingContext> GetPreviousEditingContexts()
        {
            return m_PreviousContexts;
        }

        internal static EditingContextManagerSettings LoadProjectSettings()
        {
            EditingContextManagerSettings settings = ScriptableObject.CreateInstance<EditingContextManagerSettings>();
            if (File.Exists(k_SettingsPath))
                JsonUtility.FromJsonOverwrite(File.ReadAllText(k_SettingsPath), settings);

            return settings;
        }

        internal static EditingContextManagerSettings LoadUserSettings()
        {
            EditingContextManagerSettings settings;
            if (File.Exists(k_UserSettingsPath)
                && File.GetLastWriteTime(k_UserSettingsPath) > File.GetLastWriteTime(k_SettingsPath))
            {
                settings = ScriptableObject.CreateInstance<EditingContextManagerSettings>();
                JsonUtility.FromJsonOverwrite(File.ReadAllText(k_UserSettingsPath), settings);
            }
            else
                settings = LoadProjectSettings();

            return settings;
        }

        internal static void ResetProjectSettings()
        {
#if UNITY_EDITOR
            File.Delete(k_UserSettingsPath);

            if (EditorUtility.DisplayDialog("Delete Project Settings?", "Would you like to remove the project-wide settings, too?", "Yes", "No"))
                File.Delete(k_SettingsPath);
#endif
        }

        internal static void SaveProjectSettings(EditingContextManagerSettings settings)
        {
#if UNITY_EDITOR
            File.WriteAllText(k_SettingsPath, JsonUtility.ToJson(settings, true));
#endif
        }

        internal static void SaveUserSettings(EditingContextManagerSettings settings)
        {
#if UNITY_EDITOR
            File.WriteAllText(k_UserSettingsPath, JsonUtility.ToJson(settings, true));
#endif
        }

#if UNITY_EDITOR
        [ContextMenu("Utility")]
        void Utility()
        {
            var objects = Resources.FindObjectsOfTypeAll<InputManager>();
            Debug.Log("InputManagers");
            foreach (var o in objects)
                Debug.Log(o);

            Resources.UnloadUnusedAssets();

            Debug.Log("GameObjects");
            var gameObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var g in gameObjects)
            {
                if ((g.hideFlags & (HideFlags.DontSave | HideFlags.DontUnloadUnusedAsset)) != 0)
                    Debug.Log(g.name, g);

                var im = g.GetComponent<InputManager>();
                if (im)
                    Debug.Log("Input Manager!!!! " + im);
            }
        }
#endif

        static void InitializeInputManager()
        {
            // HACK: InputSystem has a static constructor that is relied upon for initializing a bunch of other components, so
            // in edit mode we need to handle lifecycle explicitly
            var managers = Resources.FindObjectsOfTypeAll<InputManager>();
            foreach (var m in managers)
            {
                ObjectUtils.Destroy(m.gameObject);
            }

            managers = Resources.FindObjectsOfTypeAll<InputManager>();

//            var gameObjects = Resources.FindObjectsOfTypeAll<GameObject>();
//            foreach (var g in gameObjects)
//                Debug.Log(g.name);

            if (managers.Length == 0)
            {
                // Attempt creating object hierarchy via an implicit static constructor call by touching the class
                InputSystem.ExecuteEvents();
                managers = Resources.FindObjectsOfTypeAll<InputManager>();

                if (managers.Length == 0)
                {
                    typeof(InputSystem).TypeInitializer.Invoke(null, null);
                    managers = Resources.FindObjectsOfTypeAll<InputManager>();
                }
            }
            Assert.IsTrue(managers.Length == 1, "Only one InputManager should be active; Count: " + managers.Length);

            s_InputManager = managers[0];
            var go = s_InputManager.gameObject;
//            go.hideFlags = ObjectUtils.hideFlags;
            ObjectUtils.SetRunInEditModeRecursively(go, true);

            // These components were allocating memory every frame and aren't currently used in EditorVR
            ObjectUtils.Destroy(s_InputManager.GetComponent<JoystickInputToEvents>());
            ObjectUtils.Destroy(s_InputManager.GetComponent<MouseInputToEvents>());
            ObjectUtils.Destroy(s_InputManager.GetComponent<KeyboardInputToEvents>());
            ObjectUtils.Destroy(s_InputManager.GetComponent<TouchInputToEvents>());
        }
    }
}

