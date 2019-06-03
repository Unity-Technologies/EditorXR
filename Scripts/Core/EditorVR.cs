using System;
using System.Collections.Generic;
using System.Linq;
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
    sealed class EditorVR : MonoBehaviour, IEditor, IConnectInterfaces,
        IModuleDependency<EditorXRMiniWorldModule>, IModuleDependency<SerializedPreferencesModule>, IInterfaceConnector
    {
        const HideFlags k_DefaultHideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
        internal const string VRPlayerTag = "VRPlayer";
        const string k_PreserveLayout = "EditorVR.PreserveLayout";
        const string k_IncludeInBuilds = "EditorVR.IncludeInBuilds";
        const string k_SerializedPreferences = "EditorVR.SerializedPreferences";

        event Action selectionChanged;

        internal readonly List<DeviceData> deviceData = new List<DeviceData>();

        bool m_HasDeserialized;

        static bool s_IsInitialized;
        EditorXRMiniWorldModule m_MiniWorldModule;
        SerializedPreferencesModule m_SerializedPreferencesModule;

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
            EditorPrefs.DeleteKey(k_PreserveLayout);
            EditorPrefs.DeleteKey(k_IncludeInBuilds);
            EditorPrefs.DeleteKey(k_SerializedPreferences);
            ModuleLoaderDebugSettings.instance.SetModuleHideFlags(k_DefaultHideFlags);
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
            EditorXRUtils.hideFlags = ModuleLoaderDebugSettings.instance.moduleHideFlags;
            if (!Application.isPlaying)
                enabled = false;

            // TODO: Find a way to reconcile callback owner
            ModuleLoaderCore.instance.OnBehaviorAwake();
        }

        internal void Initialize()
        {
            if (UpdateInputManager != null)
                UpdateInputManager();

#if UNITY_EDITOR
            DrivenRectTransformTracker.StopRecordingUndo();

            if (!Application.isPlaying)
                ClearDeveloperConsoleIfNecessary();
#endif
            HandleInitialization();

            UnityBrandColorScheme.sessionGradient = UnityBrandColorScheme.GetRandomCuratedLightGradient();
            UnityBrandColorScheme.saturatedSessionGradient = UnityBrandColorScheme.GetRandomCuratedGradient();

            // In case we have anything selected at start, set up manipulators, inspector, etc.
            EditorApplication.delayCall += OnSelectionChanged;

            var initializableModules = new List<IInitializableModule>();
            foreach (var module in ModuleLoaderCore.instance.modules)
            {
                var initializableModule = module as IInitializableModule;
                if (initializableModule != null)
                    initializableModules.Add(initializableModule);
            }

            initializableModules.Sort((a, b) => a.order.CompareTo(b.order));

            foreach (var module in initializableModules)
            {
                module.Initialize();
            }

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
        }

        void OnEnable()
        {
            deviceData.Clear();
            Selection.selectionChanged += OnSelectionChanged;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                var currentAssembly = typeof(EditorVR).Assembly;
                foreach (var module in ModuleLoaderCore.instance.modules)
                {
                    if (module.GetType().Assembly != currentAssembly)
                        continue;

                    var behavior = module as MonoBehaviour;
                    if (behavior != null)
                        behavior.StartRunInEditMode();
                }
            }
#endif

            if (!Application.isPlaying)
                Initialize();

            // TODO: Find a way to reconcile callback owner
            //ModuleLoaderCore.instance.OnBehaviorEnable();
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
                var mainMenu = device.mainMenu;
                this.DisconnectInterfaces(mainMenu);
                var behavior = mainMenu as MonoBehaviour;
                if (behavior)
                    UnityObjectUtils.Destroy(behavior);

                var alternateMenu = device.alternateMenu;
                this.DisconnectInterfaces(alternateMenu);
                behavior = alternateMenu as MonoBehaviour;
                if (behavior)
                    UnityObjectUtils.Destroy(behavior);

                var toolsMenu = device.toolsMenu;
                this.DisconnectInterfaces(toolsMenu);
                behavior = toolsMenu as MonoBehaviour;
                if (behavior)
                    UnityObjectUtils.Destroy(behavior);

                var customMenu = device.customMenu;
                this.DisconnectInterfaces(customMenu);
                behavior = customMenu as MonoBehaviour;
                if (behavior)
                    UnityObjectUtils.Destroy(behavior);

                var spatialMenu = device.spatialMenu;
                this.DisconnectInterfaces(spatialMenu);
                behavior = spatialMenu;
                if (behavior)
                    UnityObjectUtils.Destroy(behavior);

                foreach (var menu in device.alternateMenus.ToList())
                {
                    this.DisconnectInterfaces(menu);
                    behavior = menu as MonoBehaviour;
                    if (behavior)
                        UnityObjectUtils.Destroy(behavior);
                }

                foreach (var toolData in device.toolData.ToList())
                {
                    var tool = toolData.tool;
                    this.DisconnectInterfaces(tool);
                    behavior = tool as MonoBehaviour;
                    if (behavior)
                        UnityObjectUtils.Destroy(behavior);
                }
            }

            deviceData.Clear();

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                var currentAssembly = typeof(EditorVR).Assembly;
                foreach (var module in ModuleLoaderCore.instance.modules)
                {
                    if (module.GetType().Assembly != currentAssembly)
                        continue;

                    if (ReferenceEquals(module, this))
                        continue;

                    var behavior = module as MonoBehaviour;
                    if (behavior != null)
                        behavior.StopRunInEditMode();
                }
            }
#endif

            // TODO: Find a way to reconcile callback owner
            //ModuleLoaderCore.instance.OnBehaviorDisable();
        }

        internal void Shutdown()
        {
            foreach (var module in ModuleLoaderCore.instance.modules)
            {
                var initializable = module as IInitializableModule;
                if (initializable != null)
                    initializable.Shutdown();
            }

            if (m_HasDeserialized)
                serializedPreferences = m_SerializedPreferencesModule.SerializePreferences();
        }

        void Update()
        {
            // TODO: Find a way to reconcile callback owner
            ModuleLoaderCore.instance.OnBehaviorUpdate();
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

        public void ConnectDependency(EditorXRMiniWorldModule dependency)
        {
            m_MiniWorldModule = dependency;
        }

        public void ConnectDependency(SerializedPreferencesModule dependency)
        {
            m_SerializedPreferencesModule = dependency;
        }

        public void LoadModule() { }

        public void UnloadModule()
        {
            var activeView = VRView.activeView;
            if (activeView)
                activeView.Close();
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
