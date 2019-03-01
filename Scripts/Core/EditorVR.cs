using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.Experimental.EditorVR;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;

[assembly: OptionalDependency("PolyToolkit.PolyApi", "INCLUDE_POLY_TOOLKIT")]
[assembly: OptionalDependency("UnityEngine.DrivenRectTransformTracker+BlockUndoCCU", "UNDO_PATCH")]

namespace UnityEditor.Experimental.EditorVR.Core
{
#if UNITY_2017_2_OR_NEWER
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    [RequiresTag(k_VRPlayerTag)]
    sealed partial class EditorVR : MonoBehaviour, IEditor, IConnectInterfaces
    {
        const string k_ShowGameObjects = "EditorVR.ShowGameObjects";
        const string k_PreserveLayout = "EditorVR.PreserveLayout";
        const string k_IncludeInBuilds = "EditorVR.IncludeInBuilds";
        const string k_SerializedPreferences = "EditorVR.SerializedPreferences";
        const string k_VRPlayerTag = "VRPlayer";

        Dictionary<Type, Nested> m_NestedModules = new Dictionary<Type, Nested>();
        Dictionary<Type, MonoBehaviour> m_Modules = new Dictionary<Type, MonoBehaviour>();

        Interfaces m_Interfaces;
        Type[] m_DefaultTools;

        event Action selectionChanged;

        readonly List<DeviceData> m_DeviceData = new List<DeviceData>();

        bool m_HasDeserialized;

        static bool s_IsInitialized;

        internal static HideFlags defaultHideFlags
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

        internal static Type[] DefaultTools { private get; set; }
        internal static Type DefaultMenu { private get; set; }
        internal static Type DefaultAlternateMenu { private get; set; }
        internal static Type[] HiddenTypes { private get; set; }

        class DeviceData
        {
            public IProxy proxy;
            public InputDevice inputDevice;
            public Node node;
            public Transform rayOrigin;
            public readonly Stack<Tools.ToolData> toolData = new Stack<Tools.ToolData>();
            public IMainMenu mainMenu;
            public ITool currentTool;
            public IMenu customMenu;
            public IToolsMenu toolsMenu;
            public readonly List<IAlternateMenu> alternateMenus = new List<IAlternateMenu>();
            public IAlternateMenu alternateMenu;
            public readonly Dictionary<IMenu, Menus.MenuHideData> menuHideData = new Dictionary<IMenu, Menus.MenuHideData>();
        }

        class Nested
        {
            public static EditorVR evr { protected get; set; }

            internal virtual void OnDestroy() { }
        }

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

#if !ENABLE_OVR_INPUT && !ENABLE_STEAMVR_INPUT && !ENABLE_SIXENSE_INPUT
                Debug.Log("<color=orange>EditorVR requires at least one partner (e.g. Oculus, Vive) SDK to be installed for input. You can download these from the Asset Store or from the partner's website</color>");
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

        void Initialize()
        {
#if UNITY_EDITOR
#if UNITY_2018_2_OR_NEWER
            DrivenRectTransformTracker.StopRecordingUndo();
#elif UNDO_PATCH
            DrivenRectTransformTracker.BlockUndo = true;
#endif
#endif
            Nested.evr = this; // Set this once for the convenience of all nested classes
            m_DefaultTools = DefaultTools;
            SetHideFlags(defaultHideFlags);
#if UNITY_EDITOR
            if (!Application.isPlaying)
                ClearDeveloperConsoleIfNecessary();
#endif
            HandleInitialization();

            m_Interfaces = (Interfaces)AddNestedModule(typeof(Interfaces));
            AddModule<SerializedPreferencesModule>(); // Added here in case any nested modules have preference serialization
            AddNestedModule(typeof(SerializedPreferencesModuleConnector));

            var nestedClassTypes = ObjectUtils.GetExtensionsOfClass(typeof(Nested));
            foreach (var type in nestedClassTypes)
            {
                AddNestedModule(type);
            }

            LateBindNestedModules(nestedClassTypes);

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                AddModule<HierarchyModule>();
                AddModule<ProjectFolderModule>();
            }
#endif

            AddModule<AdaptivePositionModule>();

            var viewer = GetNestedModule<Viewer>();
            viewer.preserveCameraRig = preserveLayout;
            viewer.InitializeCamera();

            var deviceInputModule = AddModule<DeviceInputModule>();
            deviceInputModule.InitializePlayerHandle();
            deviceInputModule.CreateDefaultActionMapInputs();
            deviceInputModule.processInput = ProcessInput;
            deviceInputModule.updatePlayerHandleMaps = Tools.UpdatePlayerHandleMaps;
            deviceInputModule.inputDeviceForRayOrigin = rayOrigin =>
                (from deviceData in m_DeviceData
                    where deviceData.rayOrigin == rayOrigin
                    select deviceData.inputDevice).FirstOrDefault();

            GetNestedModule<UI>().Initialize();

            AddModule<KeyboardModule>();

            var multipleRayInputModule = GetModule<MultipleRayInputModule>();

            var dragAndDropModule = AddModule<DragAndDropModule>();
            multipleRayInputModule.rayEntered += dragAndDropModule.OnRayEntered;
            multipleRayInputModule.rayExited += dragAndDropModule.OnRayExited;
            multipleRayInputModule.dragStarted += dragAndDropModule.OnDragStarted;
            multipleRayInputModule.dragEnded += dragAndDropModule.OnDragEnded;

            var tooltipModule = AddModule<TooltipModule>();
            this.ConnectInterfaces(tooltipModule);
            multipleRayInputModule.rayEntered += tooltipModule.OnRayEntered;
            multipleRayInputModule.rayHovering += tooltipModule.OnRayHovering;
            multipleRayInputModule.rayExited += tooltipModule.OnRayExited;

            AddModule<ActionsModule>();
            AddModule<HighlightModule>();

            var lockModule = AddModule<LockModule>();
            lockModule.updateAlternateMenu = (rayOrigin, o) => Menus.SetAlternateMenuVisibility(rayOrigin, o != null);

            AddModule<SelectionModule>();

            var spatialHashModule = AddModule<SpatialHashModule>();
            spatialHashModule.shouldExcludeObject = go => go.GetComponentInParent<EditorVR>();
            spatialHashModule.Setup();

            var intersectionModule = AddModule<IntersectionModule>();
            this.ConnectInterfaces(intersectionModule);
            intersectionModule.Setup(spatialHashModule.spatialHash);

            // TODO: Support module dependencies via ConnectInterfaces
            GetNestedModule<Rays>().ignoreList = intersectionModule.standardIgnoreList;

            AddModule<SnappingModule>();

            var vacuumables = GetNestedModule<Vacuumables>();

            var miniWorlds = GetNestedModule<MiniWorlds>();
            var workspaceModule = AddModule<WorkspaceModule>();
            workspaceModule.preserveWorkspaces = preserveLayout;
            workspaceModule.HiddenTypes = HiddenTypes;
            workspaceModule.workspaceCreated += vacuumables.OnWorkspaceCreated;
            workspaceModule.workspaceCreated += miniWorlds.OnWorkspaceCreated;
            workspaceModule.workspaceCreated += workspace => { deviceInputModule.UpdatePlayerHandleMaps(); };
            workspaceModule.workspaceDestroyed += vacuumables.OnWorkspaceDestroyed;
            workspaceModule.workspaceDestroyed += miniWorlds.OnWorkspaceDestroyed;

            UnityBrandColorScheme.sessionGradient = UnityBrandColorScheme.GetRandomCuratedLightGradient();
            UnityBrandColorScheme.saturatedSessionGradient = UnityBrandColorScheme.GetRandomCuratedGradient();

            var sceneObjectModule = AddModule<SceneObjectModule>();
            sceneObjectModule.tryPlaceObject = (obj, targetScale) =>
            {
                foreach (var miniWorld in miniWorlds.worlds)
                {
                    if (!miniWorld.Contains(obj.position))
                        continue;

                    var referenceTransform = miniWorld.referenceTransform;
                    obj.transform.parent = null;
                    obj.position = referenceTransform.position + Vector3.Scale(miniWorld.miniWorldTransform.InverseTransformPoint(obj.position), miniWorld.referenceTransform.localScale);
                    obj.rotation = referenceTransform.rotation * Quaternion.Inverse(miniWorld.miniWorldTransform.rotation) * obj.rotation;
                    obj.localScale = Vector3.Scale(Vector3.Scale(obj.localScale, referenceTransform.localScale), miniWorld.miniWorldTransform.lossyScale.Inverse());

                    spatialHashModule.AddObject(obj.gameObject);
                    return true;
                }

                return false;
            };

            AddModule<HapticsModule>();
            AddModule<GazeDivergenceModule>();
            AddModule<SpatialHintModule>();
            AddModule<SpatialScrollModule>();

            AddModule<FeedbackModule>();

            AddModule<WebModule>();

            //TODO: External module support (removes need for CCU in this instance)
#if INCLUDE_POLY_TOOLKIT
            AddModule<PolyModule>();
#endif

            viewer.AddPlayerModel();
            viewer.AddPlayerFloor();
            GetNestedModule<Rays>().CreateAllProxies();

            // In case we have anything selected at start, set up manipulators, inspector, etc.
            EditorApplication.delayCall += OnSelectionChanged;
        }

        IEnumerator Start()
        {
            Initialize();

            var leftHandFound = false;
            var rightHandFound = false;

            // Some components depend on both hands existing (e.g. MiniWorldWorkspace), so make sure they exist before restoring
            while (!(leftHandFound && rightHandFound))
            {
                Rays.ForEachProxyDevice(deviceData =>
                {
                    if (deviceData.node == Node.LeftHand)
                        leftHandFound = true;

                    if (deviceData.node == Node.RightHand)
                        rightHandFound = true;
                });

                yield return null;
            }

            var viewer = GetNestedModule<Viewer>();
            while (!viewer.hmdReady)
                yield return null;

            GetModule<SerializedPreferencesModule>().SetupWithPreferences(serializedPreferences);

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

            Menus.UpdateAlternateMenuOnSelectionChanged(GetNestedModule<Rays>().lastSelectionRayOrigin);
        }

        void OnEnable()
        {
            Selection.selectionChanged += OnSelectionChanged;
        }

        void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
        }

        internal void Shutdown()
        {
            if (m_HasDeserialized)
                serializedPreferences = GetModule<SerializedPreferencesModule>().SerializePreferences();
        }

        void OnDestroy()
        {
            foreach (var nested in m_NestedModules.Values)
            {
                nested.OnDestroy();
            }

#if UNITY_EDITOR
#if UNITY_2018_2_OR_NEWER
            DrivenRectTransformTracker.StartRecordingUndo();
#elif UNDO_PATCH
            DrivenRectTransformTracker.BlockUndo = false;
#endif
#endif
        }

        void Update()
        {
            GetNestedModule<Viewer>().UpdateCamera();

            Rays.UpdateRaycasts();

            GetNestedModule<Rays>().UpdateDefaultProxyRays();

            GetNestedModule<DirectSelection>().UpdateDirectSelection();

            GetModule<KeyboardModule>().UpdateKeyboardMallets();

            GetModule<DeviceInputModule>().ProcessInput();

            GetNestedModule<Menus>().UpdateMenuVisibilities();

            GetNestedModule<UI>().UpdateManipulatorVisibilities();
        }

        void ProcessInput(HashSet<IProcessInput> processedInputs, ConsumeControlDelegate consumeControl)
        {
            GetNestedModule<MiniWorlds>().UpdateMiniWorlds();

            foreach (var deviceData in m_DeviceData)
            {
                if (!deviceData.proxy.active)
                    continue;

                foreach (var toolData in deviceData.toolData)
                {
                    var process = toolData.tool as IProcessInput;
                    if (process != null && ((MonoBehaviour)toolData.tool).enabled
                        && processedInputs.Add(process)) // Only process inputs for an instance of a tool once (e.g. two-handed tools)
                        process.ProcessInput(toolData.input, consumeControl);
                }
            }
        }

        T GetModule<T>() where T : MonoBehaviour
        {
            MonoBehaviour module;
            m_Modules.TryGetValue(typeof(T), out module);
            return (T)module;
        }

        T AddModule<T>() where T : MonoBehaviour
        {
            MonoBehaviour module;
            var type = typeof(T);
            if (!m_Modules.TryGetValue(type, out module))
            {
                module = ObjectUtils.AddComponent<T>(gameObject);
                m_Modules.Add(type, module);

                foreach (var nested in m_NestedModules.Values)
                {
                    var lateBinding = nested as ILateBindInterfaceMethods<T>;
                    if (lateBinding != null)
                        lateBinding.LateBindInterfaceMethods((T)module);
                }

                this.ConnectInterfaces(module);
                m_Interfaces.AttachInterfaceConnectors(module);
            }

            return (T)module;
        }

        T GetNestedModule<T>() where T : Nested
        {
            return (T)m_NestedModules[typeof(T)];
        }

        Nested AddNestedModule(Type type)
        {
            Nested nested;
            if (!m_NestedModules.TryGetValue(type, out nested))
            {
                nested = (Nested)Activator.CreateInstance(type);
                m_NestedModules.Add(type, nested);

                if (m_Interfaces != null)
                {
                    this.ConnectInterfaces(nested);
                    m_Interfaces.AttachInterfaceConnectors(nested);
                }
            }

            return nested;
        }

        void LateBindNestedModules(IEnumerable<Type> types)
        {
            foreach (var type in types)
            {
                var lateBindings = type.GetInterfaces().Where(i =>
                    i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ILateBindInterfaceMethods<>));

                Nested nestedModule;
                if (m_NestedModules.TryGetValue(type, out nestedModule))
                {
                    foreach (var lateBinding in lateBindings)
                    {
                        var dependencyType = lateBinding.GetGenericArguments().First();

                        Nested dependency;
                        if (m_NestedModules.TryGetValue(dependencyType, out dependency))
                        {
                            var map = type.GetInterfaceMap(lateBinding);
                            if (map.InterfaceMethods.Length == 1)
                            {
                                var tm = map.TargetMethods[0];
                                tm.Invoke(nestedModule, new[] { dependency });
                            }
                        }
                    }
                }
            }
        }

        internal void SetHideFlags(HideFlags hideFlags)
        {
            ObjectUtils.hideFlags = hideFlags;

            foreach (var manager in Resources.FindObjectsOfTypeAll<InputManager>())
            {
                manager.gameObject.hideFlags = hideFlags;
            }

            foreach (var manager in Resources.FindObjectsOfTypeAll<EditingContextManager>())
            {
                manager.gameObject.hideFlags = hideFlags;
            }

            foreach (var child in GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.hideFlags = hideFlags;
            }

#if UNITY_EDITOR
            EditorApplication.DirtyHierarchyWindowSorting(); // Otherwise objects aren't shown/hidden in hierarchy window
#endif
        }

#if !INCLUDE_TEXT_MESH_PRO
        static EditorVR()
        {
            if (Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro") == null)
                Debug.LogWarning("EditorVR requires TextMesh Pro. Please open the package manager and install Text Mesh Pro");
        }
#endif
    }
#else
    internal class NoEditorVR
    {
        const string k_ShowCustomEditorWarning = "EditorVR.ShowCustomEditorWarning";

        static NoEditorVR()
        {
            if (EditorPrefs.GetBool(k_ShowCustomEditorWarning, true))
            {
                var message = "EditorVR requires Unity 2017.2 or above.";
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
