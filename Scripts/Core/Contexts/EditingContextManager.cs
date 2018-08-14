#if UNITY_EDITOR
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
    [InitializeOnLoad]
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

        static EditingContextManager()
        {
            VRView.viewEnabled += OnVRViewEnabled;
            VRView.viewDisabled += OnVRViewDisabled;

            EditorApplication.update += ReopenOnExitPlaymode;
        }

        static void OnVRViewEnabled()
        {
            InitializeInputManager();
            s_Instance = ObjectUtils.CreateGameObjectWithComponent<EditingContextManager>();
        }

        static void OnVRViewDisabled()
        {
            ObjectUtils.Destroy(s_Instance.gameObject);
            ObjectUtils.Destroy(s_InputManager.gameObject);
        }

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

#if UNITY_2017_2_OR_NEWER
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

        void OnEnable()
        {
            ISetEditingContextMethods.getAvailableEditingContexts = GetAvailableEditingContexts;
            ISetEditingContextMethods.getPreviousEditingContexts = GetPreviousEditingContexts;
            ISetEditingContextMethods.setEditingContext = SetEditingContext;
            ISetEditingContextMethods.restorePreviousEditingContext = RestorePreviousContext;

            // Force the window to repaint every tick, since we need live updating
            // This also allows scripts with [ExecuteInEditMode] to run
            EditorApplication.update += EditorApplication.QueuePlayerLoopUpdate;

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            SetEditingContext(defaultContext);
        }

        void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

            EditorApplication.update -= EditorApplication.QueuePlayerLoopUpdate;

            VRView.afterOnGUI -= OnVRViewGUI;

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
#endif


        void Awake()
        {
            s_DefaultContext = m_DefaultContext;

            var availableContexts = GetAvailableEditingContexts();
            m_ContextNames = availableContexts.Select(c => c.name).ToArray();

            if (s_AvailableContexts.Count == 0)
                throw new Exception("You can't start EditorXR without at least one context. Try re-importing the package or use version control to restore the default context asset");

            if (s_AvailableContexts.Count > 1)
                VRView.afterOnGUI += OnVRViewGUI;
        }

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

        static List<IEditingContext> GetEditingContextAssets()
        {
            var types = ObjectUtils.GetImplementationsOfInterface(typeof(IEditingContext));
            var searchString = "t: " + string.Join(" t: ", types.Select(t => t.FullName).ToArray());
            var assets = AssetDatabase.FindAssets(searchString);

            var availableContexts = new List<IEditingContext>();
            foreach (var asset in assets)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(asset);
                var context = AssetDatabase.LoadMainAssetAtPath(assetPath) as IEditingContext;
                availableContexts.Add(context);
            }

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
            File.Delete(k_UserSettingsPath);

            if (EditorUtility.DisplayDialog("Delete Project Settings?", "Would you like to remove the project-wide settings, too?", "Yes", "No"))
                File.Delete(k_SettingsPath);
        }

        internal static void SaveProjectSettings(EditingContextManagerSettings settings)
        {
            File.WriteAllText(k_SettingsPath, JsonUtility.ToJson(settings, true));
        }

        internal static void SaveUserSettings(EditingContextManagerSettings settings)
        {
            File.WriteAllText(k_UserSettingsPath, JsonUtility.ToJson(settings, true));
        }

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
            go.hideFlags = ObjectUtils.hideFlags;
            ObjectUtils.SetRunInEditModeRecursively(go, true);

            // These components were allocating memory every frame and aren't currently used in EditorVR
            ObjectUtils.Destroy(s_InputManager.GetComponent<JoystickInputToEvents>());
            ObjectUtils.Destroy(s_InputManager.GetComponent<KeyboardInputToEvents>());
            ObjectUtils.Destroy(s_InputManager.GetComponent<TouchInputToEvents>());
        }
    }
}
#endif
