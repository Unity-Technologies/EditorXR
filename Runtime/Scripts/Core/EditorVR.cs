using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Labs.EditorXR.Interfaces;
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
    sealed class EditorVR : IEditor, IUsesConnectInterfaces, IModuleDependency<EditorXRToolModule>,
        IInterfaceConnector
    {
        const HideFlags k_DefaultHideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
        internal const string VRPlayerTag = "VRPlayer";
        const string k_PreserveLayout = "EditorVR.PreserveLayout";
        const string k_IncludeInBuilds = "EditorVR.IncludeInBuilds";

        event Action selectionChanged;

        static bool s_IsInitialized;
        EditorXRMiniWorldModule m_MiniWorldModule;
        EditorXRToolModule m_ToolModule;

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

        internal static Type[] DefaultTools { get; set; }
        internal static Type DefaultMenu { get; set; }
        internal static Type DefaultAlternateMenu { get; set; }
        internal static Type[] HiddenTypes { get; set; }
        internal static Action UpdateInputManager { private get; set; }

        public int connectInterfaceOrder { get { return 0; } }

#if !FI_AUTOFILL
        IProvidesConnectInterfaces IFunctionalitySubscriber<IProvidesConnectInterfaces>.provider { get; set; }
#endif

        internal static void ResetPreferences()
        {
#if UNITY_EDITOR
            EditorPrefs.DeleteKey(k_PreserveLayout);
            EditorPrefs.DeleteKey(k_IncludeInBuilds);
            EditorPrefs.DeleteKey(SerializedPreferencesModule.SerializedPreferencesKey);
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

        internal void Initialize()
        {
            Selection.selectionChanged += OnSelectionChanged;

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

            initializableModules.Sort((a, b) => a.initializationOrder.CompareTo(b.initializationOrder));

            foreach (var module in initializableModules)
            {
                module.Initialize();
            }
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

        internal void Shutdown()
        {
            var initializableModules = new List<IInitializableModule>();
            foreach (var module in ModuleLoaderCore.instance.modules)
            {
                var initializableModule = module as IInitializableModule;
                if (initializableModule != null)
                    initializableModules.Add(initializableModule);
            }

            initializableModules.Sort((a, b) => a.shutdownOrder.CompareTo(b.shutdownOrder));

            foreach (var module in initializableModules)
            {
                module.Shutdown();
            }

            Selection.selectionChanged -= OnSelectionChanged;

            // Suppress MissingReferenceException in tests
            EditorApplication.delayCall -= OnSelectionChanged;

#if UNITY_EDITOR
            DrivenRectTransformTracker.StartRecordingUndo();
#endif
        }

        internal void ProcessInput(HashSet<IProcessInput> processedInputs, ConsumeControlDelegate consumeControl)
        {
            if (m_MiniWorldModule != null)
                m_MiniWorldModule.UpdateMiniWorlds();

            foreach (var device in m_ToolModule.deviceData)
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

        public void LoadModule() { m_MiniWorldModule = ModuleLoaderCore.instance.GetModule<EditorXRMiniWorldModule>(); }

        public void ConnectDependency(EditorXRToolModule dependency)
        {
            m_ToolModule = dependency;
        }

        public void UnloadModule()
        {
#if UNITY_EDITOR
            var activeView = VRView.activeView;
            if (activeView)
                activeView.Close();
#endif
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
