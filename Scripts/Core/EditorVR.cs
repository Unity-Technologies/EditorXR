#if UNITY_EDITOR
using System;
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

namespace UnityEditor.Experimental.EditorVR
{
	[InitializeOnLoad]
#if UNITY_EDITORVR
	[RequiresTag(k_VRPlayerTag)]
	sealed partial class EditorVR
	{
		public const HideFlags DefaultHideFlags = HideFlags.DontSave;
		const string k_VRPlayerTag = "VRPlayer";

		[SerializeField]
		private GameObject m_PlayerModelPrefab;

		[SerializeField]
		GameObject m_PreviewCameraPrefab;

		[SerializeField]
		ProxyExtras m_ProxyExtras;

		DragAndDropModule m_DragAndDropModule;
		HighlightModule m_HighlightModule;
		SceneObjectModule m_SceneObjectModule;
		LockModule m_LockModule;
		SelectionModule m_SelectionModule;
		HierarchyModule m_HierarchyModule;
		ProjectFolderModule m_ProjectFolderModule;
		ActionsModule m_ActionsModule;
		KeyboardModule m_KeyboardModule;
		SpatialHashModule m_SpatialHashModule;
		IntersectionModule m_IntersectionModule;
		DeviceInputModule m_DeviceInputModule;
		MultipleRayInputModule m_InputModule;
		PixelRaycastModule m_PixelRaycastModule;
		WorkspaceModule m_WorkspaceModule;
		TooltipModule m_TooltipModule;

		DirectSelection m_DirectSelection;
		Interfaces m_Interfaces;
		Menus m_Menus;
		MiniWorlds m_MiniWorlds;
		Rays m_Rays;
		Tools m_Tools;
		UI m_UI;
		Viewer m_Viewer;
		Vacuumables m_Vacuumables;

		event Action m_SelectionChanged;

		IPreviewCamera m_CustomPreviewCamera;

		readonly List<DeviceData> m_DeviceData = new List<DeviceData>();

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
		}

		void Awake()
		{
			Nested.evr = this; // Set this once for the convenience of all nested classes 

			ClearDeveloperConsoleIfNecessary();

			m_DirectSelection = new DirectSelection();
			m_Interfaces = new Interfaces();
			m_Menus = new Menus();
			m_MiniWorlds = new MiniWorlds();
			m_Rays = new Rays();
			m_Tools = new Tools();
			m_UI = new UI();
			m_Viewer = new Viewer();
			m_Vacuumables = new Vacuumables();

			m_HierarchyModule = AddModule<HierarchyModule>();
			m_ProjectFolderModule = AddModule<ProjectFolderModule>();

			VRView.cameraRig.parent = transform; // Parent the camera rig under EditorVR
			if (VRSettings.loadedDeviceName == "OpenVR")
			{
				// Steam's reference position should be at the feet and not at the head as we do with Oculus
				VRView.cameraRig.localPosition = Vector3.zero;
			}

			var hmdOnlyLayerMask = 0;
			if (m_PreviewCameraPrefab)
			{
				var go = ObjectUtils.Instantiate(m_PreviewCameraPrefab);
				m_CustomPreviewCamera = go.GetComponentInChildren<IPreviewCamera>();
				if (m_CustomPreviewCamera != null)
				{
					VRView.customPreviewCamera = m_CustomPreviewCamera.previewCamera;
					m_CustomPreviewCamera.vrCamera = VRView.viewerCamera;
					hmdOnlyLayerMask = m_CustomPreviewCamera.hmdOnlyLayerMask;
					m_Interfaces.ConnectInterfaces(m_CustomPreviewCamera);
				}
			}
			VRView.cullingMask = UnityEditor.Tools.visibleLayers | hmdOnlyLayerMask;

			m_DeviceInputModule = AddModule<DeviceInputModule>();
			m_DeviceInputModule.InitializePlayerHandle();
			m_DeviceInputModule.CreateDefaultActionMapInputs();
			m_DeviceInputModule.processInput = ProcessInput;
			m_DeviceInputModule.updatePlayerHandleMaps = m_Tools.UpdatePlayerHandleMaps;

			m_UI.Initialize();

			m_KeyboardModule = AddModule<KeyboardModule>();

			m_DragAndDropModule = AddModule<DragAndDropModule>();
			m_InputModule.rayEntered += m_DragAndDropModule.OnRayEntered;
			m_InputModule.rayExited += m_DragAndDropModule.OnRayExited;
			m_InputModule.dragStarted += m_DragAndDropModule.OnDragStarted;
			m_InputModule.dragEnded += m_DragAndDropModule.OnDragEnded;

			m_TooltipModule = AddModule<TooltipModule>();
			m_Interfaces.ConnectInterfaces(m_TooltipModule);
			m_InputModule.rayEntered += m_TooltipModule.OnRayEntered;
			m_InputModule.rayExited += m_TooltipModule.OnRayExited;

			m_PixelRaycastModule = AddModule<PixelRaycastModule>();
			m_PixelRaycastModule.ignoreRoot = transform;
			m_PixelRaycastModule.raycastCamera = m_UI.eventCamera;

			m_HighlightModule = AddModule<HighlightModule>();
			m_ActionsModule = AddModule<ActionsModule>();

			m_LockModule = AddModule<LockModule>();
			m_LockModule.updateAlternateMenu = (rayOrigin, o) => m_Menus.SetAlternateMenuVisibility(rayOrigin, o != null);

			m_SelectionModule = AddModule<SelectionModule>();
			m_SelectionModule.selected += m_Rays.SetLastSelectionRayOrigin; // when a selection occurs in the selection tool, call show in the alternate menu, allowing it to show/hide itself.
			m_SelectionModule.getGroupRoot = GetGroupRoot;

			m_SpatialHashModule = AddModule<SpatialHashModule>();
			m_SpatialHashModule.shouldExcludeObject = go => go.GetComponentInParent<EditorVR>();
			m_SpatialHashModule.Setup();

			m_IntersectionModule = AddModule<IntersectionModule>();
			m_Interfaces.ConnectInterfaces(m_IntersectionModule);
			m_IntersectionModule.Setup(m_SpatialHashModule.spatialHash);

			m_Menus.mainMenuTools = m_Tools.allTools.Where(t => !m_Tools.IsPermanentTool(t)).ToList(); // Don't show tools that can't be selected/toggled

			m_WorkspaceModule = AddModule<WorkspaceModule>();
			m_WorkspaceModule.workspaceCreated += m_Vacuumables.OnWorkspaceCreated;
			m_WorkspaceModule.workspaceCreated += m_MiniWorlds.OnWorkspaceCreated;
			m_WorkspaceModule.workspaceCreated += (workspace) => { m_DeviceInputModule.UpdatePlayerHandleMaps(); };
			m_WorkspaceModule.workspaceDestroyed += m_Vacuumables.OnWorkspaceDestroyed;
			m_WorkspaceModule.workspaceDestroyed += (workspace) => { m_Interfaces.DisconnectInterfaces(workspace); };
			m_WorkspaceModule.workspaceDestroyed += m_MiniWorlds.OnWorkspaceDestroyed;

			UnityBrandColorScheme.sessionGradient = UnityBrandColorScheme.GetRandomGradient();

			m_SceneObjectModule = AddModule<SceneObjectModule>();
			m_SceneObjectModule.shouldPlaceObject = (obj, targetScale) =>
			{
				foreach (var miniWorld in m_MiniWorlds.worlds)
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

			m_Rays.CreateAllProxies();

			// In case we have anything selected at start, set up manipulators, inspector, etc.
			EditorApplication.delayCall += OnSelectionChanged;
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

			m_Menus.UpdateAlternateMenuOnSelectionChanged(m_Rays.lastSelectionRayOrigin);
		}

		void OnEnable()
		{
			Selection.selectionChanged += OnSelectionChanged;
		}

		void OnDisable()
		{
			Selection.selectionChanged -= OnSelectionChanged;
		}

		void OnDestroy()
		{
			if (m_CustomPreviewCamera != null)
				ObjectUtils.Destroy(((MonoBehaviour)m_CustomPreviewCamera).gameObject);

			m_MiniWorlds.OnDestroy();
		}

		void Update()
		{
			if (m_CustomPreviewCamera != null)
				m_CustomPreviewCamera.enabled = VRView.showDeviceView && VRView.customPreviewCamera != null;

			m_Rays.UpdateDefaultProxyRays();

			m_KeyboardModule.UpdateKeyboardMallets();

			m_DeviceInputModule.ProcessInput();

			m_Menus.UpdateMenuVisibilityNearWorkspaces();
			m_Menus.UpdateMenuVisibilities();

			m_UI.UpdateManipulatorVisibilites();
		}

		void ProcessInput(HashSet<IProcessInput> processedInputs, ConsumeControlDelegate consumeControl)
		{
			m_MiniWorlds.UpdateMiniWorlds(consumeControl);

			m_InputModule.ProcessInput(null, consumeControl);

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

		T AddModule<T>() where T : Component
		{
			T module = ObjectUtils.AddComponent<T>(gameObject);
			m_Interfaces.ConnectInterfaces(module);
			return module;
		}

		static GameObject GetGroupRoot(GameObject hoveredObject)
		{
			if (!hoveredObject)
				return null;

			var groupRoot = PrefabUtility.FindPrefabRoot(hoveredObject);
			if (groupRoot == hoveredObject)
				groupRoot = FindGroupRoot(hoveredObject.transform).gameObject;

			return groupRoot;
		}

		static Transform FindGroupRoot(Transform transform)
		{
			// Don't allow grouping selection for the player head, otherwise we'd select the EditorVRCamera
			if (transform.CompareTag(k_VRPlayerTag))
				return transform;

			var parent = transform.parent;
			if (parent)
			{
				if (parent.GetComponent<Renderer>())
					return FindGroupRoot(parent);

				return parent;
			}

			return transform;
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
			s_InputManager.gameObject.hideFlags = DefaultHideFlags;
			ObjectUtils.SetRunInEditModeRecursively(s_InputManager.gameObject, true);

			// These components were allocating memory every frame and aren't currently used in EditorVR
			ObjectUtils.Destroy(s_InputManager.GetComponent<JoystickInputToEvents>());
			ObjectUtils.Destroy(s_InputManager.GetComponent<MouseInputToEvents>());
			ObjectUtils.Destroy(s_InputManager.GetComponent<KeyboardInputToEvents>());
			ObjectUtils.Destroy(s_InputManager.GetComponent<TouchInputToEvents>());
		}

		static void OnVRViewDisabled()
		{
			ObjectUtils.Destroy(s_Instance.gameObject);
			ObjectUtils.Destroy(s_InputManager.gameObject);
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
