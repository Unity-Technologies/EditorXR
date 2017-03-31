#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.Experimental.EditorVR.Helpers;
using UnityEditor.Experimental.EditorVR.Menus;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputNew;
using UnityEngine.VR;

namespace UnityEditor.Experimental.EditorVR.Core
{
	[InitializeOnLoad]
#if UNITY_EDITORVR
	[RequiresTag(k_VRPlayerTag)]
	sealed partial class EditorVR : MonoBehaviour
	{
		const string k_ShowGameObjects = "EditorVR.ShowGameObjects";
		const string k_PreserveLayout = "EditorVR.PreserveLayout";
		const string k_SerializedPreferences = "EditorVR.SerializedPreferences";
		const string k_VRPlayerTag = "VRPlayer";

		[SerializeField]
		GameObject m_PlayerModelPrefab;

		[SerializeField]
		ProxyExtras m_ProxyExtras;

		Dictionary<Type, MonoBehaviour> m_Modules = new Dictionary<Type, MonoBehaviour>();

		Interfaces m_Interfaces;

		Dictionary<Type, Nested> m_NestedModules = new Dictionary<Type, Nested>();

		event Action m_SelectionChanged;

		readonly List<DeviceData> m_DeviceData = new List<DeviceData>();

		// Local method use only -- caching here to prevent frequent lookups in Update
		Rays m_Rays;
		DirectSelection m_DirectSelection;
		Menus m_Menus;
		UI m_UI;
		KeyboardModule m_KeyboardModule;
		DeviceInputModule m_DeviceInputModule;
		Viewer m_Viewer;

		static HideFlags defaultHideFlags
		{
			get { return showGameObjects ? HideFlags.DontSave : HideFlags.HideAndDontSave; }
		}

		static bool showGameObjects
		{
			get { return EditorPrefs.GetBool(k_ShowGameObjects, false); }
			set { EditorPrefs.SetBool(k_ShowGameObjects, value); }
		}

		static bool preserveLayout
		{
			get { return EditorPrefs.GetBool(k_PreserveLayout, true); }
			set { EditorPrefs.SetBool(k_PreserveLayout, value); }
		}

		static string serializedPreferences
		{
			get { return EditorPrefs.GetString(k_SerializedPreferences, string.Empty); }
			set { EditorPrefs.SetString(k_SerializedPreferences, value); }
		}

		class DeviceData
		{
			public IProxy proxy;
			public InputDevice inputDevice;
			public Node node;
			public Transform rayOrigin;
			public readonly Stack<Tools.ToolData> toolData = new Stack<Tools.ToolData>();
			public ActionMapInput uiInput;
			public MainMenuActivator mainMenuActivator;
			public ActionMapInput directSelectInput;
			public IMainMenu mainMenu;
			public ActionMapInput mainMenuInput;
			public IAlternateMenu alternateMenu;
			public ActionMapInput alternateMenuInput;
			public ITool currentTool;
			public IMenu customMenu;
			public PinnedToolButton previousToolButton;
			public readonly Dictionary<IMenu, Menus.MenuHideFlags> menuHideFlags = new Dictionary<IMenu, Menus.MenuHideFlags>();
			public readonly Dictionary<IMenu, float> menuSizes = new Dictionary<IMenu, float>();
		}

		class Nested
		{
			public static EditorVR evr { protected get; set; }

			internal virtual void OnDestroy() { }
		}

		void Awake()
		{
			Nested.evr = this; // Set this once for the convenience of all nested classes 

			ClearDeveloperConsoleIfNecessary();

			m_Interfaces = (Interfaces)AddNestedModule(typeof(Interfaces));
			AddModule<SerializedPreferencesModule>(); // Added here in case any nested modules have preference serialization

			var nestedClassTypes = ObjectUtils.GetExtensionsOfClass(typeof(Nested));
			foreach (var type in nestedClassTypes)
			{
				AddNestedModule(type);
			}
			LateBindNestedModules(nestedClassTypes);

			m_Rays = GetNestedModule<Rays>();
			m_DirectSelection = GetNestedModule<DirectSelection>();
			m_Menus = GetNestedModule<Menus>();
			m_UI = GetNestedModule<UI>();

			AddModule<HierarchyModule>();
			AddModule<ProjectFolderModule>();

			m_Viewer = GetNestedModule<Viewer>();
			m_Viewer.preserveCameraRig = preserveLayout;
			m_Viewer.InitializeCamera();

			var tools = GetNestedModule<Tools>();

			m_DeviceInputModule = AddModule<DeviceInputModule>();
			m_DeviceInputModule.InitializePlayerHandle();
			m_DeviceInputModule.CreateDefaultActionMapInputs();
			m_DeviceInputModule.processInput = ProcessInput;
			m_DeviceInputModule.updatePlayerHandleMaps = tools.UpdatePlayerHandleMaps;

			var ui = GetNestedModule<UI>();
			ui.Initialize();

			m_KeyboardModule = AddModule<KeyboardModule>();

			var dragAndDropModule = AddModule<DragAndDropModule>();
			var inputModule = GetModule<MultipleRayInputModule>();
			inputModule.rayEntered += dragAndDropModule.OnRayEntered;
			inputModule.rayExited += dragAndDropModule.OnRayExited;
			inputModule.dragStarted += dragAndDropModule.OnDragStarted;
			inputModule.dragEnded += dragAndDropModule.OnDragEnded;

			var tooltipModule = AddModule<TooltipModule>();
			m_Interfaces.ConnectInterfaces(tooltipModule);
			inputModule.rayEntered += tooltipModule.OnRayEntered;
			inputModule.rayExited += tooltipModule.OnRayExited;

			var pixelRaycastModule = AddModule<PixelRaycastModule>();
			pixelRaycastModule.ignoreRoot = transform;
			pixelRaycastModule.raycastCamera = ui.eventCamera;

			AddModule<ActionsModule>();
			AddModule<HighlightModule>();

			var menus = GetNestedModule<Menus>();

			var lockModule = AddModule<LockModule>();
			lockModule.updateAlternateMenu = (rayOrigin, o) => menus.SetAlternateMenuVisibility(rayOrigin, o != null);

			var rays = GetNestedModule<Rays>();

			var selectionModule = AddModule<SelectionModule>();
			selectionModule.selected += rays.SetLastSelectionRayOrigin; // when a selection occurs in the selection tool, call show in the alternate menu, allowing it to show/hide itself.
			selectionModule.getGroupRoot = GetGroupRoot;

			var spatialHashModule = AddModule<SpatialHashModule>();
			spatialHashModule.shouldExcludeObject = go => go.GetComponentInParent<EditorVR>();
			spatialHashModule.Setup();

			var intersectionModule = AddModule<IntersectionModule>();
			m_Interfaces.ConnectInterfaces(intersectionModule);
			intersectionModule.Setup(spatialHashModule.spatialHash);

			menus.mainMenuTools = tools.allTools.Where(t => !tools.IsPermanentTool(t)).ToList(); // Don't show tools that can't be selected/toggled

			var vacuumables = GetNestedModule<Vacuumables>();
			var miniWorlds = GetNestedModule<MiniWorlds>();

			var workspaceModule = AddModule<WorkspaceModule>();
			workspaceModule.preserveWorkspaces = preserveLayout;
			workspaceModule.workspaceCreated += vacuumables.OnWorkspaceCreated;
			workspaceModule.workspaceCreated += miniWorlds.OnWorkspaceCreated;
			workspaceModule.workspaceCreated += (workspace) => { m_DeviceInputModule.UpdatePlayerHandleMaps(); };
			workspaceModule.workspaceDestroyed += vacuumables.OnWorkspaceDestroyed;
			workspaceModule.workspaceDestroyed += (workspace) => { m_Interfaces.DisconnectInterfaces(workspace); };
			workspaceModule.workspaceDestroyed += miniWorlds.OnWorkspaceDestroyed;

			UnityBrandColorScheme.sessionGradient = UnityBrandColorScheme.GetRandomGradient();

			var sceneObjectModule = AddModule<SceneObjectModule>();
			sceneObjectModule.shouldPlaceObject = (obj, targetScale) =>
			{
				foreach (var miniWorld in miniWorlds.worlds)
				{
					if (!miniWorld.Contains(obj.position))
						continue;

					var referenceTransform = miniWorld.referenceTransform;
					obj.transform.parent = null;
					obj.position = referenceTransform.position + Vector3.Scale(miniWorld.miniWorldTransform.InverseTransformPoint(obj.position), miniWorld.referenceTransform.localScale);
					obj.rotation = referenceTransform.rotation * Quaternion.Inverse(miniWorld.miniWorldTransform.rotation) * obj.rotation;
					obj.localScale = Vector3.Scale(Vector3.Scale(obj.localScale, referenceTransform.localScale), miniWorld.miniWorldTransform.lossyScale);
					return false;
				}

				return true;
			};

			m_Viewer.AddPlayerModel();

			rays.CreateAllProxies();

			// In case we have anything selected at start, set up manipulators, inspector, etc.
			EditorApplication.delayCall += OnSelectionChanged;
		}

		IEnumerator Start()
		{
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

			GetModule<SerializedPreferencesModule>().DeserializePreferences(serializedPreferences);
		}

		void ClearDeveloperConsoleIfNecessary()
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

				var hasFlagMethod = consoleWindowType.GetMethod("HasFlag", BindingFlags.NonPublic | BindingFlags.Instance);
				var result = (bool)hasFlagMethod.Invoke(window, new[] { clearOnPlayFlag });

				if (result)
				{
					var logEntries = asm.GetType("UnityEditorInternal.LogEntries");
					var clearMethod = logEntries.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public);
					clearMethod.Invoke(null, null);
				}
			}
		}

		void OnSelectionChanged()
		{
			if (m_SelectionChanged != null)
				m_SelectionChanged();

			GetNestedModule<Menus>().UpdateAlternateMenuOnSelectionChanged(GetNestedModule<Rays>().lastSelectionRayOrigin);
		}

		void OnEnable()
		{
			Selection.selectionChanged += OnSelectionChanged;
		}

		void OnDisable()
		{
			Selection.selectionChanged -= OnSelectionChanged;
		}

		void Shutdown()
		{
			serializedPreferences = GetModule<SerializedPreferencesModule>().SerializePreferences();
		}

		void OnDestroy()
		{
			foreach (var nested in m_NestedModules.Values)
			{
				nested.OnDestroy();
			}
		}

		void Update()
		{
			m_Viewer.UpdateCamera();

			m_Rays.UpdateDefaultProxyRays();
			m_DirectSelection.UpdateDirectSelection();

			m_KeyboardModule.UpdateKeyboardMallets();

			m_DeviceInputModule.ProcessInput();

			m_Menus.UpdateMenuVisibilityNearWorkspaces();
			m_Menus.UpdateMenuVisibilities();

			m_UI.UpdateManipulatorVisibilites();
		}

		void ProcessInput(HashSet<IProcessInput> processedInputs, ConsumeControlDelegate consumeControl)
		{
			GetNestedModule<MiniWorlds>().UpdateMiniWorlds(consumeControl);

			GetModule<MultipleRayInputModule>().ProcessInput(null, consumeControl);

			foreach (var deviceData in m_DeviceData)
			{
				if (!deviceData.proxy.active)
					continue;

				var mainMenu = deviceData.mainMenu;
				var menuInput = mainMenu as IProcessInput;
				if (menuInput != null && mainMenu.visible)
					menuInput.ProcessInput(deviceData.mainMenuInput, consumeControl);

				var altMenu = deviceData.alternateMenu;
				var altMenuInput = altMenu as IProcessInput;
				if (altMenuInput != null && altMenu.visible)
					altMenuInput.ProcessInput(deviceData.alternateMenuInput, consumeControl);

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
			MonoBehaviour module = null;
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

				m_Interfaces.ConnectInterfaces(module);
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
			Nested nested = null;
			if (!m_NestedModules.TryGetValue(type, out nested))
			{
				nested = (Nested)Activator.CreateInstance(type);
				m_NestedModules.Add(type, nested);

				if (m_Interfaces != null)
				{
					m_Interfaces.ConnectInterfaces(nested);
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

						Nested dependency = null;
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

		static GameObject GetGroupRoot(GameObject hoveredObject)
		{
			if (!hoveredObject)
				return null;

			var groupRoot = PrefabUtility.FindPrefabRoot(hoveredObject);

			return groupRoot;
		}

		static EditorVR s_Instance;
		static InputManager s_InputManager;

		[MenuItem("Window/EditorVR %e", false)]
		public static void ShowEditorVR()
		{
			// Using a utility window improves performance by saving from the overhead of DockArea.OnGUI()
			VRView.GetWindow<VRView>(true, "EditorVR", true);
		}

		[MenuItem("Window/EditorVR %e", true)]
		public static bool ShouldShowEditorVR()
		{
			return PlayerSettings.virtualRealitySupported;
		}

		static EditorVR()
		{
			VRView.onEnable += OnVRViewEnabled;
			VRView.onDisable += OnVRViewDisabled;

			if (!PlayerSettings.virtualRealitySupported)
				Debug.Log("<color=orange>EditorVR requires VR support. Please check Virtual Reality Supported in Edit->Project Settings->Player->Other Settings</color>");

#if !ENABLE_OVR_INPUT && !ENABLE_STEAMVR_INPUT && !ENABLE_SIXENSE_INPUT
			Debug.Log("<color=orange>EditorVR requires at least one partner (e.g. Oculus, Vive) SDK to be installed for input. You can download these from the Asset Store or from the partner's website</color>");
#endif

			// Add EVR tags and layers if they don't exist
			var tags = TagManager.GetRequiredTags();
			var layers = TagManager.GetRequiredLayers();

			foreach (var tag in tags)
				TagManager.AddTag(tag);

			foreach (var layer in layers)
				TagManager.AddLayer(layer);
		}

		static void OnVRViewEnabled()
		{
			ObjectUtils.hideFlags = defaultHideFlags;
			InitializeInputManager();
			s_Instance = ObjectUtils.CreateGameObjectWithComponent<EditorVR>();
		}

		static void InitializeInputManager()
		{
			// HACK: InputSystem has a static constructor that is relied upon for initializing a bunch of other components, so
			// in edit mode we need to handle lifecycle explicitly
			InputManager[] managers = Resources.FindObjectsOfTypeAll<InputManager>();
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
			s_InputManager.gameObject.hideFlags = defaultHideFlags;
			ObjectUtils.SetRunInEditModeRecursively(s_InputManager.gameObject, true);

			// These components were allocating memory every frame and aren't currently used in EditorVR
			ObjectUtils.Destroy(s_InputManager.GetComponent<JoystickInputToEvents>());
			ObjectUtils.Destroy(s_InputManager.GetComponent<MouseInputToEvents>());
			ObjectUtils.Destroy(s_InputManager.GetComponent<KeyboardInputToEvents>());
			ObjectUtils.Destroy(s_InputManager.GetComponent<TouchInputToEvents>());
		}

		static void OnVRViewDisabled()
		{
			s_Instance.Shutdown(); // Give a chance for dependent systems (e.g. serialization) to shut-down before destroying
			ObjectUtils.Destroy(s_Instance.gameObject);
			ObjectUtils.Destroy(s_InputManager.gameObject);
		}

		[PreferenceItem("EditorVR")]
		static void PreferencesGUI()
		{
			EditorGUILayout.BeginVertical();
			EditorGUILayout.Space();

			// Show EditorVR GameObjects
			{
				string title = "Show EditorVR GameObjects";
				string tooltip = "Normally, EditorVR GameObjects are hidden in the Hierarchy. Would you like to show them?";
				showGameObjects = EditorGUILayout.Toggle(new GUIContent(title, tooltip), showGameObjects);
			}

			// Preserve Layout
			{
				string title = "Preserve Layout";
				string tooltip = "Check this to preserve your layout and location in EditorVR";
				preserveLayout = EditorGUILayout.Toggle(new GUIContent(title, tooltip), preserveLayout);
			}

			EditorGUILayout.EndVertical();
		}
	}
#else
	internal class NoEditorVR
	{
		const string k_ShowCustomEditorWarning = "EditorVR.ShowCustomEditorWarning";

		static NoEditorVR()
		{
			if (EditorPrefs.GetBool(k_ShowCustomEditorWarning, true))
			{
				var message = "EditorVR requires a custom editor build. Please see https://blogs.unity3d.com/2016/12/15/editorvr-experimental-build-available-today/";
				var result = EditorUtility.DisplayDialogComplex("Custom Editor Build Required", message, "Download", "Ignore", "Remind Me Again");
				switch (result)
				{
					case 0:
						Application.OpenURL("http://rebrand.ly/EditorVR-build");
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
#endif
