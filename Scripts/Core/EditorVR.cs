#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.Experimental.EditorVR.Menus;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;

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

		Dictionary<Type, Nested> m_NestedModules = new Dictionary<Type, Nested>();
		Dictionary<Type, MonoBehaviour> m_Modules = new Dictionary<Type, MonoBehaviour>();

		Interfaces m_Interfaces;
		Type[] m_DefaultTools;

		event Action selectionChanged;

		readonly List<DeviceData> m_DeviceData = new List<DeviceData>();

		bool m_HasDeserialized;

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

		internal static Type[] defaultTools { get; set; }

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

		static void ResetPreferences()
		{
			EditorPrefs.DeleteKey(k_ShowGameObjects);
			EditorPrefs.DeleteKey(k_PreserveLayout);
			EditorPrefs.DeleteKey(k_SerializedPreferences);
		}

		void Awake()
		{
			Nested.evr = this; // Set this once for the convenience of all nested classes 
			m_DefaultTools = defaultTools;

			ClearDeveloperConsoleIfNecessary();

			m_Interfaces = (Interfaces)AddNestedModule(typeof(Interfaces));
			AddModule<SerializedPreferencesModule>(); // Added here in case any nested modules have preference serialization

			var nestedClassTypes = ObjectUtils.GetExtensionsOfClass(typeof(Nested));
			foreach (var type in nestedClassTypes)
			{
				AddNestedModule(type);
			}
			LateBindNestedModules(nestedClassTypes);

			AddModule<HierarchyModule>();
			AddModule<ProjectFolderModule>();

			var viewer = GetNestedModule<Viewer>();
			viewer.preserveCameraRig = preserveLayout;
			viewer.InitializeCamera();

			var deviceInputModule = AddModule<DeviceInputModule>();
			deviceInputModule.InitializePlayerHandle();
			deviceInputModule.CreateDefaultActionMapInputs();
			deviceInputModule.processInput = ProcessInput;
			deviceInputModule.updatePlayerHandleMaps = Tools.UpdatePlayerHandleMaps;

			GetNestedModule<UI>().Initialize();

			AddModule<KeyboardModule>();

			var multipleRayInputModule = GetModule<MultipleRayInputModule>();
			var dragAndDropModule = AddModule<DragAndDropModule>();
			multipleRayInputModule.rayEntered += dragAndDropModule.OnRayEntered;
			multipleRayInputModule.rayExited += dragAndDropModule.OnRayExited;
			multipleRayInputModule.dragStarted += dragAndDropModule.OnDragStarted;
			multipleRayInputModule.dragEnded += dragAndDropModule.OnDragEnded;

			var tooltipModule = AddModule<TooltipModule>();
			m_Interfaces.ConnectInterfaces(tooltipModule);
			multipleRayInputModule.rayEntered += tooltipModule.OnRayEntered;
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
			m_Interfaces.ConnectInterfaces(intersectionModule);
			intersectionModule.Setup(spatialHashModule.spatialHash);

			var snappingModule = AddModule<SnappingModule>();
			snappingModule.raycast = intersectionModule.Raycast;

			var vacuumables = GetNestedModule<Vacuumables>();

			var miniWorlds = GetNestedModule<MiniWorlds>();
			var workspaceModule = AddModule<WorkspaceModule>();
			workspaceModule.preserveWorkspaces = preserveLayout;
			workspaceModule.workspaceCreated += vacuumables.OnWorkspaceCreated;
			workspaceModule.workspaceCreated += miniWorlds.OnWorkspaceCreated;
			workspaceModule.workspaceCreated += workspace =>
			{
				workspaceModule.workspaceInputs.Add((WorkspaceInput)deviceInputModule.CreateActionMapInputForObject(workspace, null));
				deviceInputModule.UpdatePlayerHandleMaps();
			};
			workspaceModule.workspaceDestroyed += vacuumables.OnWorkspaceDestroyed;
			workspaceModule.workspaceDestroyed += miniWorlds.OnWorkspaceDestroyed;

			UnityBrandColorScheme.sessionGradient = UnityBrandColorScheme.GetRandomCuratedLightGradient();

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
					obj.localScale = Vector3.Scale(Vector3.Scale(obj.localScale, referenceTransform.localScale), miniWorld.miniWorldTransform.lossyScale);

					spatialHashModule.AddObject(obj.gameObject);
					return true;
				}

				return false;
			};

			viewer.AddPlayerModel();

			GetNestedModule<Rays>().CreateAllProxies();

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

			var viewer = GetNestedModule<Viewer>();
			while (!viewer.hmdReady)
				yield return null;

			GetModule<SerializedPreferencesModule>().DeserializePreferences(serializedPreferences);
			m_HasDeserialized = true;
		}

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
					var logEntries = asm.GetType("UnityEditorInternal.LogEntries");
					var clearMethod = logEntries.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public);
					clearMethod.Invoke(null, null);
				}
			}
		}

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
		}

		void Update()
		{
			GetNestedModule<Viewer>().UpdateCamera();

			Rays.UpdateRaycasts();

			GetNestedModule<Rays>().UpdateDefaultProxyRays();

			GetNestedModule<DirectSelection>().UpdateDirectSelection();

			GetModule<KeyboardModule>().UpdateKeyboardMallets();

			GetModule<DeviceInputModule>().ProcessInput();

			var menus = GetNestedModule<Menus>();
			menus.UpdateMenuVisibilityNearWorkspaces();
			menus.UpdateMenuVisibilities();

			GetNestedModule<UI>().UpdateManipulatorVisibilites();
		}

		void ProcessInput(HashSet<IProcessInput> processedInputs, ConsumeControlDelegate consumeControl)
		{
			GetModule<WorkspaceModule>().ProcessInput(consumeControl);

			GetNestedModule<MiniWorlds>().UpdateMiniWorlds();

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
			Nested nested;
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

		static EditorVR()
		{
			ObjectUtils.hideFlags = defaultHideFlags;

			if (!PlayerSettings.virtualRealitySupported)
				Debug.Log("<color=orange>EditorVR requires VR support. Please check Virtual Reality Supported in Edit->Project Settings->Player->Other Settings</color>");

#if !ENABLE_OVR_INPUT && !ENABLE_STEAMVR_INPUT && !ENABLE_SIXENSE_INPUT
			Debug.Log("<color=orange>EditorVR requires at least one partner (e.g. Oculus, Vive) SDK to be installed for input. You can download these from the Asset Store or from the partner's website</color>");
#endif

			// Add EVR tags and layers if they don't exist
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

			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Reset to Defaults", GUILayout.Width(140)))
				ResetPreferences();
			
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
