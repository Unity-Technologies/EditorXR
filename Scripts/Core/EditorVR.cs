using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;

#if UNITY_EDITOR
[assembly: OptionalDependency("PolyToolkit.PolyApi", "INCLUDE_POLY_TOOLKIT")]
#endif

namespace UnityEditor.Experimental.EditorVR.Core
{
    class DeviceData
    {
        public IProxy proxy;
        public InputDevice inputDevice;
        public Node node;
        public Transform rayOrigin;
        public readonly Stack<ToolData> toolData = new Stack<ToolData>();
        public IMainMenu mainMenu;
        public ITool currentTool;
        public IMenu customMenu;
        public IToolsMenu toolsMenu;
        public readonly List<IAlternateMenu> alternateMenus = new List<IAlternateMenu>();
        public IAlternateMenu alternateMenu;
        public SpatialMenu spatialMenu;
        public readonly Dictionary<IMenu, MenuHideData> menuHideData = new Dictionary<IMenu, MenuHideData>();
    }

#if UNITY_2018_3_OR_NEWER
#if UNITY_EDITOR
    [RequiresTag(VRPlayerTag)]
#endif
    [ModuleOrder(ModuleOrders.EditorVRLoadOrder)]
    sealed class EditorVR : MonoBehaviour, IEditor, IConnectInterfaces, IModuleDependency<EditorXRRayModule>,
        IModuleDependency<EditorXRViewerModule>, IModuleDependency<EditorXRMenuModule>,
        IModuleDependency<EditorXRDirectSelectionModule>, IModuleDependency<KeyboardModule>,
        IModuleDependency<DeviceInputModule>,IModuleDependency<EditorXRUIModule>,
        IModuleDependency<EditorXRMiniWorldModule>, IModuleDependency<SerializedPreferencesModule>,
        IModuleDependency<SpatialHintModule>, IModuleDependency<TooltipModule>,IModuleDependency<IntersectionModule>,
        IModuleDependency<SnappingModule>, IInterfaceConnector
    {
        internal const string VRPlayerTag = "VRPlayer";
        const string k_ShowGameObjects = "EditorVR.ShowGameObjects";
        const string k_PreserveLayout = "EditorVR.PreserveLayout";
        const string k_IncludeInBuilds = "EditorVR.IncludeInBuilds";
        const string k_SerializedPreferences = "EditorVR.SerializedPreferences";

        event Action selectionChanged;

        internal readonly List<DeviceData> deviceData = new List<DeviceData>();

        bool m_HasDeserialized;

        static bool s_IsInitialized;
        EditorXRRayModule m_RayModule;
        EditorXRViewerModule m_ViewerModule;
        EditorXRMenuModule m_MenuModule;
        EditorXRDirectSelectionModule m_DirectSelectionModule;
        KeyboardModule m_KeyboardModule;
        DeviceInputModule m_DeviceInputModule;
        EditorXRUIModule m_UIModule;
        EditorXRMiniWorldModule m_MiniWorldModule;
        SerializedPreferencesModule m_SerializedPreferencesModule;
        SpatialHintModule m_SpatialHintModule;
        TooltipModule m_TooltipModule;
        IntersectionModule m_IntersectionModule;
        SnappingModule m_SnappingModule;

        public static HideFlags defaultHideFlags
        {
            get
            {
                if (Application.isPlaying)
                    return HideFlags.None;

                return showGameObjects ? HideFlags.DontSaveInEditor : HideFlags.HideInHierarchy | HideFlags.DontSaveInEditor;
            }
        }

        internal static bool showGameObjects
        {
            get { return EditorPrefs.GetBool(k_ShowGameObjects, false); }
            set { EditorPrefs.SetBool(k_ShowGameObjects, value); }
        }

        internal static bool preserveLayout
        {
            get { return EditorPrefs.GetBool(k_PreserveLayout, true); }
            set { EditorPrefs.SetBool(k_PreserveLayout, value); }
        }

        internal static bool includeInBuilds
        {
            get { return EditorPrefs.GetBool(k_IncludeInBuilds, false); }
            set { EditorPrefs.SetBool(k_IncludeInBuilds, value); }
        }

        internal static string serializedPreferences
        {
            get { return EditorPrefs.GetString(k_SerializedPreferences, string.Empty); }
            set { EditorPrefs.SetString(k_SerializedPreferences, value); }
        }

        internal static Type[] DefaultTools { get; set; }
        internal static Type DefaultMenu { get; set; }
        internal static Type DefaultAlternateMenu { get; set; }
        internal static Type[] HiddenTypes { get; set; }
        internal static Action UpdateInputManager { private get; set; }

        internal static void ResetPreferences()
        {
#if UNITY_EDITOR
            EditorPrefs.DeleteKey(k_ShowGameObjects);
            EditorPrefs.DeleteKey(k_PreserveLayout);
            EditorPrefs.DeleteKey(k_IncludeInBuilds);
            EditorPrefs.DeleteKey(k_SerializedPreferences);
#endif
        }

        // Code from the previous static constructor moved here to allow for testability
        static void HandleInitialization()
        {
            if (!s_IsInitialized)
            {
                s_IsInitialized = true;

#if UNITY_EDITOR
                if (!PlayerSettings.virtualRealitySupported)
                    Debug.Log("<color=orange>EditorXR requires VR support. Please check Virtual Reality Supported in Edit->Project Settings->Player->XR Settings</color>");
#endif
            }

            // Add EVR tags and layers if they don't exist
#if UNITY_EDITOR
            var tags = TagManager.GetRequiredTags();
            var layers = TagManager.GetRequiredLayers();

            foreach (var tag in tags)
            {
                TagManager.AddTag(tag);
            }

            foreach (var layer in layers)
            {
                TagManager.AddLayer(layer);
            }
#endif
        }

        void Awake()
        {
            enabled = false;
        }

        void Initialize()
        {
            if (UpdateInputManager != null)
                UpdateInputManager();

#if UNITY_EDITOR
            DrivenRectTransformTracker.StopRecordingUndo();
#endif

            SetHideFlags(defaultHideFlags);
#if UNITY_EDITOR
            if (!Application.isPlaying)
                ClearDeveloperConsoleIfNecessary();
#endif
            HandleInitialization();

            UnityBrandColorScheme.sessionGradient = UnityBrandColorScheme.GetRandomCuratedLightGradient();
            UnityBrandColorScheme.saturatedSessionGradient = UnityBrandColorScheme.GetRandomCuratedGradient();

            // In case we have anything selected at start, set up manipulators, inspector, etc.
            EditorApplication.delayCall += OnSelectionChanged;

            // TODO: Better way to call init on everything
            m_SnappingModule.Initialize();
            m_IntersectionModule.Initialize();
            m_UIModule.Initialize();
            m_TooltipModule.Initialize();
            m_SpatialHintModule.Initialize();
            m_DeviceInputModule.Initialize();
            m_ViewerModule.Initialize();
            m_MenuModule.Initialize();
            m_RayModule.CreateAllProxies();
        }

        void Start()
        {
            Initialize();

            m_SerializedPreferencesModule.SetupWithPreferences(serializedPreferences);

            m_HasDeserialized = true;
        }

#if UNITY_EDITOR
        static void ClearDeveloperConsoleIfNecessary()
        {
            var asm = Assembly.GetAssembly(typeof(Editor));
            var consoleWindowType = asm.GetType("UnityEditor.ConsoleWindow");

            EditorWindow window = null;
            foreach (var w in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if (w.GetType() == consoleWindowType)
                {
                    window = w;
                    break;
                }
            }

            if (window)
            {
                var consoleFlagsType = consoleWindowType.GetNestedType("ConsoleFlags", BindingFlags.NonPublic);
                var names = Enum.GetNames(consoleFlagsType);
                var values = Enum.GetValues(consoleFlagsType);
                var clearOnPlayFlag = values.GetValue(Array.IndexOf(names, "ClearOnPlay"));

                var hasFlagMethod = consoleWindowType.GetMethod("HasFlag", BindingFlags.NonPublic | BindingFlags.Static);
                var result = (bool)hasFlagMethod.Invoke(window, new[] { clearOnPlayFlag });

                if (result)
                {
                    var logEntries = asm.GetType("UnityEditor.LogEntries");
                    var clearMethod = logEntries.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public);
                    clearMethod.Invoke(null, null);
                }
            }
        }
#endif

        void OnSelectionChanged()
        {
            if (selectionChanged != null)
                selectionChanged();

            m_MenuModule.UpdateAlternateMenuOnSelectionChanged(m_RayModule.lastSelectionRayOrigin);
        }

        void OnEnable()
        {
            Selection.selectionChanged += OnSelectionChanged;
        }

        void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;

            // Suppress MissingReferenceException in tests
            EditorApplication.delayCall -= OnSelectionChanged;

#if UNITY_EDITOR
            DrivenRectTransformTracker.StartRecordingUndo();
#endif

            foreach (var device in deviceData)
            {
                var behavior = device.mainMenu as MonoBehaviour;
                if (behavior)
                    UnityObjectUtils.Destroy(behavior);

                behavior = device.alternateMenu as MonoBehaviour;
                if (behavior)
                    UnityObjectUtils.Destroy(behavior);

                behavior = device.toolsMenu as MonoBehaviour;
                if (behavior)
                    UnityObjectUtils.Destroy(behavior);

                behavior = device.customMenu as MonoBehaviour;
                if (behavior)
                    UnityObjectUtils.Destroy(behavior);

                behavior = device.spatialMenu;
                if (behavior)
                    UnityObjectUtils.Destroy(behavior);

                foreach (var menu in device.alternateMenus)
                {
                    behavior = menu as MonoBehaviour;
                    if (behavior)
                        UnityObjectUtils.Destroy(behavior);
                }

                foreach (var tool in device.toolData)
                {
                    behavior = tool.tool as MonoBehaviour;
                    if (behavior)
                        UnityObjectUtils.Destroy(behavior);
                }
            }
        }

        internal void Shutdown()
        {
            m_SnappingModule.Shutdown();
            m_IntersectionModule.Shutdown();
            m_UIModule.Shutdown();
            m_DeviceInputModule.Shutdown();
            m_SpatialHintModule.Shutdown();
            m_TooltipModule.Shutdown();

            if (m_HasDeserialized)
                serializedPreferences = m_SerializedPreferencesModule.SerializePreferences();
        }

        void Update()
        {
            m_ViewerModule.UpdateCamera();

            m_RayModule.UpdateRaycasts();

            m_RayModule.UpdateDefaultProxyRays();

            m_DirectSelectionModule.UpdateDirectSelection();

            m_KeyboardModule.UpdateKeyboardMallets();

            m_DeviceInputModule.ProcessInput();

            m_MenuModule.UpdateMenuVisibilities();

            m_UIModule.UpdateManipulatorVisibilities();
        }

        internal void ProcessInput(HashSet<IProcessInput> processedInputs, ConsumeControlDelegate consumeControl)
        {
            m_MiniWorldModule.UpdateMiniWorlds();

            foreach (var device in deviceData)
            {
                if (!device.proxy.active)
                    continue;

                foreach (var toolData in device.toolData)
                {
                    var process = toolData.tool as IProcessInput;
                    if (process != null && ((MonoBehaviour)toolData.tool).enabled
                        && processedInputs.Add(process)) // Only process inputs for an instance of a tool once (e.g. two-handed tools)
                        process.ProcessInput(toolData.input, consumeControl);
                }
            }
        }

        internal void SetHideFlags(HideFlags hideFlags)
        {
            EditorXRUtils.hideFlags = hideFlags;

            foreach (var manager in Resources.FindObjectsOfTypeAll<InputManager>())
            {
                manager.gameObject.hideFlags = hideFlags;
            }

            EditingContextManager.instance.gameObject.hideFlags = hideFlags;

            foreach (var child in GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.hideFlags = hideFlags;
            }

#if UNITY_EDITOR
            EditorApplication.DirtyHierarchyWindowSorting(); // Otherwise objects aren't shown/hidden in hierarchy window
#endif
        }

        public void ConnectDependency(EditorXRRayModule dependency)
        {
            m_RayModule = dependency;
        }

        public void ConnectDependency(EditorXRViewerModule dependency)
        {
            m_ViewerModule = dependency;
        }

        public void ConnectDependency(EditorXRMenuModule dependency)
        {
            m_MenuModule = dependency;
        }

        public void ConnectDependency(EditorXRDirectSelectionModule dependency)
        {
            m_DirectSelectionModule = dependency;
        }

        public void ConnectDependency(KeyboardModule dependency)
        {
            m_KeyboardModule = dependency;
        }

        public void ConnectDependency(DeviceInputModule dependency)
        {
            m_DeviceInputModule = dependency;
        }

        public void ConnectDependency(EditorXRUIModule dependency)
        {
            m_UIModule = dependency;
        }

        public void ConnectDependency(EditorXRMiniWorldModule dependency)
        {
            m_MiniWorldModule = dependency;
        }

        public void ConnectDependency(SerializedPreferencesModule dependency)
        {
            m_SerializedPreferencesModule = dependency;
        }

        public void ConnectDependency(SpatialHintModule dependency)
        {
            m_SpatialHintModule = dependency;
        }

        public void ConnectDependency(TooltipModule dependency)
        {
            m_TooltipModule = dependency;
        }

        public void ConnectDependency(IntersectionModule dependency)
        {
            m_IntersectionModule = dependency;
        }

        public void ConnectDependency(SnappingModule dependency)
        {
            m_SnappingModule = dependency;
        }

        public void LoadModule()
        {

        }

        public void UnloadModule()
        {
        }

        public void ConnectInterface(object target, object userData = null)
        {
            var selectionChanged = target as ISelectionChanged;
            if (selectionChanged != null)
                this.selectionChanged += selectionChanged.OnSelectionChanged;
        }

        public void DisconnectInterface(object target, object userData = null)
        {
            var selectionChanged = target as ISelectionChanged;
            if (selectionChanged != null)
                this.selectionChanged -= selectionChanged.OnSelectionChanged;
        }
    }
#else
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    class NoEditorVR
    {
        const string k_ShowCustomEditorWarning = "EditorVR.ShowCustomEditorWarning";

        static NoEditorVR()
        {
            if (EditorPrefs.GetBool(k_ShowCustomEditorWarning, true))
            {
                var message = "EditorVR requires Unity 2018.3.12 or above.";
                var result = EditorUtility.DisplayDialogComplex("Update Unity", message, "Download", "Ignore", "Remind Me Again");
                switch (result)
                {
                    case 0:
                        Application.OpenURL("https://unity3d.com/get-unity/download");
                        break;
                    case 1:
                        EditorPrefs.SetBool(k_ShowCustomEditorWarning, false);
                        break;
                    case 2:
                        Debug.Log("<color=orange>" + message + "</color>");
                        break;
                }
            }
        }
    }
#endif
}
