//#define ENABLE_MINIWORLD_RAY_SELECTION
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.VR.Modules;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.InputNew;
using UnityEngine.VR;
using UnityEngine.VR.Actions;
using UnityEngine.VR.Helpers;
using UnityEngine.VR.Menus;
using UnityEngine.VR.Modules;
using UnityEngine.VR.Proxies;
using UnityEngine.VR.Tools;
using UnityEngine.VR.UI;
using UnityEngine.VR.Utilities;
using UnityEngine.VR.Workspaces;
using UnityObject = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.VR;
#endif

[InitializeOnLoad]
public class EditorVR : MonoBehaviour
{
	public const HideFlags kDefaultHideFlags = HideFlags.DontSave;

	const string kShowInMiniWorldTag = "ShowInMiniWorld";

	/// <summary>
	/// Tag applied to player head model which tracks the camera (for MiniWorld locomotion)
	/// </summary>
	const string kVRPlayerTag = "VRPlayer";

	private const float kDefaultRayLength = 100f;

	private const float kWorkspaceAnglePadding = 25f;
	private const float kWorkspaceYPadding = 0.35f;
	private const int kMaxWorkspacePlacementAttempts = 20;
	private const float kWorkspaceVacuumEnableDistance = 1f; // Disable vacuum bounds if workspace is close to player
	const float kPreviewScale = 0.1f;

	const float kViewerPivotTransitionTime = 0.75f;
	const float kMiniWorldManipulatorScale = 0.2f;

	// Maximum time (in ms) before yielding in CreateFolderData: should be target frame time minus approximately how long one operation will take
	const float kMaxFrameTime = 0.08f;

	[SerializeField]
	private ActionMap m_TrackedObjectActionMap;
	[SerializeField]
	private ActionMap m_StandardToolActionMap;

	[SerializeField]
	ActionMap m_DirectSelectActionMap;

	[SerializeField]
	DefaultProxyRay m_ProxyRayPrefab;

	[SerializeField]
	private Camera m_EventCameraPrefab;
	[SerializeField]
	private MainMenuActivator m_MainMenuActivatorPrefab;

	[SerializeField]
	private KeyboardMallet m_KeyboardMalletPrefab;

	[SerializeField]
	private KeyboardUI m_NumericKeyboardPrefab;

	[SerializeField]
	private KeyboardUI m_StandardKeyboardPrefab;

	[SerializeField]
	private GameObject m_PlayerModelPrefab;

	private readonly Dictionary<Transform, DefaultProxyRay> m_DefaultRays = new Dictionary<Transform, DefaultProxyRay>();
	private readonly Dictionary<Transform, KeyboardMallet> m_KeyboardMallets = new Dictionary<Transform, KeyboardMallet>();

	private KeyboardUI m_NumericKeyboard;
	private KeyboardUI m_StandardKeyboard;

	private TrackedObject m_TrackedObjectInput;

	private MultipleRayInputModule m_InputModule;
	private SpatialHashModule m_SpatialHashModule;
	private IntersectionModule m_IntersectionModule;
	private Camera m_EventCamera;
	private PixelRaycastModule m_PixelRaycastModule;
	private HighlightModule m_HighlightModule;
	private ObjectPlacementModule m_ObjectPlacementModule;
	private SnappingModule m_SnappingModule;
	private LockModule m_LockModule;
	private DragAndDropModule m_DragAndDropModule;

	private bool m_UpdatePixelRaycastModule = true;

	private PlayerHandle m_PlayerHandle;

	private class DeviceData
	{
		public Stack<ITool> tools;
		public ActionMapInput uiInput;
		public MainMenuActivator mainMenuActivator;
		public ActionMapInput directSelectInput;
		public IMainMenu mainMenu;
		public bool restoreMainMenu;
		public IAlternateMenu alternateMenu;
		public ITool currentTool;
		public List<GameObject> toolMenus;
    }

	private readonly Dictionary<InputDevice, DeviceData> m_DeviceData = new Dictionary<InputDevice, DeviceData>();
	private readonly List<IProxy> m_AllProxies = new List<IProxy>();
	private List<ActionMenuData> m_MenuActions = new List<ActionMenuData>();
	private List<Type> m_AllTools;
	private List<IAction> m_AllActions;
	List<Type> m_MainMenuTools;
	private List<Type> m_AllWorkspaceTypes;
	private readonly List<Workspace> m_AllWorkspaces = new List<Workspace>();

	private List<IModule> m_MainMenuModules = new List<IModule>();

	private readonly Dictionary<string, Node> m_TagToNode = new Dictionary<string, Node>
	{
		{ "Left", Node.LeftHand },
		{ "Right", Node.RightHand }
	};

	private class MiniWorldRay
	{
		public Transform originalRayOrigin;
		public ActionMapInput originalDirectSelectInput;
		public IMiniWorld miniWorld;
		public IProxy proxy;
		public Node node;
#if ENABLE_MINIWORLD_RAY_SELECTION
		public ActionMapInput uiInput;
#endif
		public ActionMapInput directSelectInput;
		public IntersectionTester tester;
		public GameObject dragObject;

		public Vector3 dragObjectOriginalScale;
		public Vector3 dragObjectPreviewScale;

		public bool wasHeld;
		public Vector3 originalPositionOffset;
		public Quaternion originalRotationOffset;

		public bool wasContained;
	}

	private readonly Dictionary<Transform, MiniWorldRay> m_MiniWorldRays = new Dictionary<Transform, MiniWorldRay>();
	private readonly List<IMiniWorld> m_MiniWorlds = new List<IMiniWorld>();

	private event Action m_SelectionChanged;

	bool m_HMDReady;

	VRSmoothCamera m_SmoothCamera;
	IGrabObjects m_TransformTool;

	StandardManipulator m_Manipulator;
	Vector3 m_OriginalManipulatorScale;

	class FolderDataIteration
	{
		public FolderData data;
		public bool hasNext;
	}

	readonly List<IProjectFolderList> m_ProjectFolderLists = new List<IProjectFolderList>();
	FolderData[] m_FolderData;
	readonly HashSet<string> m_AssetTypes = new HashSet<string>();
	float m_LastFolderYieldTime;

	readonly List<IFilterUI> m_FilterUIs = new List<IFilterUI>();

	private void Awake()
	{
		ClearDeveloperConsoleIfNecessary();

		StartCoroutine(LoadProjectFolders());

		VRView.viewerPivot.parent = transform; // Parent the camera pivot under EditorVR
		if (VRSettings.loadedDeviceName == "OpenVR")
		{
			// Steam's reference position should be at the feet and not at the head as we do with Oculus
			VRView.viewerPivot.localPosition = Vector3.zero;
		}
		m_SmoothCamera = U.Object.AddComponent<VRSmoothCamera>(VRView.viewerCamera.gameObject);
		VRView.customPreviewCamera = m_SmoothCamera.smoothCamera;

		InitializePlayerHandle();
		CreateDefaultActionMapInputs();
		CreateAllProxies();
		CreateDeviceDataForInputDevices();

		m_DragAndDropModule = U.Object.AddComponent<DragAndDropModule>(gameObject);

		CreateEventSystem();

		m_PixelRaycastModule = U.Object.AddComponent<PixelRaycastModule>(gameObject);
		m_PixelRaycastModule.ignoreRoot = transform;
		m_HighlightModule = U.Object.AddComponent<HighlightModule>(gameObject);
		m_ObjectPlacementModule = U.Object.AddComponent<ObjectPlacementModule>(gameObject);
		ConnectInterfaces(m_ObjectPlacementModule);
		m_SnappingModule = U.Object.AddComponent<SnappingModule>(gameObject);

		m_LockModule = U.Object.AddComponent<LockModule>(gameObject);
		m_LockModule.openRadialMenu = DisplayAlternateMenu;
		ConnectInterfaces(m_LockModule);

		m_AllTools = U.Object.GetImplementationsOfInterface(typeof(ITool)).ToList();
		m_MainMenuTools = m_AllTools.Where(t => !IsPermanentTool(t)).ToList(); // Don't show tools that can't be selected/toggled
		m_AllWorkspaceTypes = U.Object.GetExtensionsOfClass(typeof(Workspace)).ToList();
		m_MainMenuModules = GetComponents<IModule>().ToList();

		SpawnActions();

		UnityBrandColorScheme.sessionGradient = UnityBrandColorScheme.GetRandomGradient();

		// TODO: Only show tools in the menu for the input devices in the action map that match the devices present in the system.  
		// This is why we're collecting all the action maps. Additionally, if the action map only has a single hand specified, 
		// then only show it in that hand's menu.
		// CollectToolActionMaps(m_AllTools);
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
			var result = (bool)hasFlagMethod.Invoke(window, new [] { clearOnPlayFlag });

			if (result)
			{
				var logEntries = asm.GetType("UnityEditorInternal.LogEntries");
				var clearMethod = logEntries.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public);
				clearMethod.Invoke(null, null);
			}
		}
	}

	private void OnSelectionChanged()
	{
		if (m_SelectionChanged != null)
			m_SelectionChanged();
	}

	IEnumerable<InputDevice> GetSystemDevices()
	{
		// For now let's filter out any other devices other than VR controller devices; Eventually, we may support mouse / keyboard etc.
		return InputSystem.devices.Where(d => d is VRInputDevice && d.tagIndex != -1);
	}

	private void CreateDeviceDataForInputDevices()
	{
		foreach (var device in GetSystemDevices())
		{
			var deviceData = new DeviceData
			{
				tools = new Stack<ITool>(),
				toolMenus = new List<GameObject>()
			};
			m_DeviceData.Add(device, deviceData);
		}
	}

	private IEnumerator Start()
	{
		// Delay until at least one proxy initializes
		bool proxyActive = false;
		while (!proxyActive)
		{
			foreach (var proxy in m_AllProxies)
			{
				if (proxy.active)
				{
					proxyActive = true;
					break;
				}
			}

			yield return null;
		}

		CreateSpatialSystem();
		AddPlayerModel();
		SpawnDefaultTools();
		PrewarmAssets();

		// In case we have anything selected at start, set up manipulators, inspector, etc.
		EditorApplication.delayCall += OnSelectionChanged;

		// This will be the first call to update the player handle (input) maps, sorted by priority
		UpdatePlayerHandleMaps();
	}

	void OnEnable()
	{
		Selection.selectionChanged += OnSelectionChanged;
#if UNITY_EDITOR
		VRView.onGUIDelegate += OnSceneGUI;
#endif
	}

	void OnDisable()
	{
		Selection.selectionChanged -= OnSelectionChanged;
#if UNITY_EDITOR
		VRView.onGUIDelegate -= OnSceneGUI;
#endif
	}

	void OnSceneGUI(EditorWindow obj)
	{
		if (Event.current.type == EventType.ExecuteCommand)
		{
			m_PixelRaycastModule.UpdateIgnoreList();

			ForEachRayOrigin((proxy, pair, device, deviceData) =>
			{
				m_PixelRaycastModule.UpdateRaycast(pair.Value, m_EventCamera);
			}, true);

#if ENABLE_MINIWORLD_RAY_SELECTION
			foreach (var rayOrigin in m_MiniWorldRays.Keys)
				m_PixelRaycastModule.UpdateRaycast(rayOrigin, m_EventCamera);
#endif

			UpdateDefaultProxyRays();

			// Queue up the next round
			m_UpdatePixelRaycastModule = true;

			Event.current.Use();
		}
	}

	void OnDestroy()
	{
		PlayerHandleManager.RemovePlayerHandle(m_PlayerHandle);
	}

	void PrewarmAssets()
	{
		// HACK: Cannot async load assets in the editor yet, so to avoid a hitch let's spawn the menu immediately and then make it invisible
		foreach (var kvp in m_DeviceData)
		{
			var device = kvp.Key;
			var deviceData = m_DeviceData[device];
			var mainMenu = deviceData.mainMenu;

			if (mainMenu == null)
			{
				mainMenu = SpawnMainMenu(typeof(MainMenu), device, false);
				deviceData.mainMenu = mainMenu;
				UpdatePlayerHandleMaps();
			}
		}
	}

	IEnumerator PreloadAssetTypes()
	{
		var hp = new HierarchyProperty(HierarchyType.Assets);
		hp.SetSearchFilter("t:object", 0);

		var types = new HashSet<string>();

		while (hp.Next(null))
		{
			types.Add(hp.pptrValue.GetType().Name);
			yield return null;
		}
	}

	private void Update()
	{
		m_SmoothCamera.enabled = VRView.showDeviceView;

		foreach (var proxy in m_AllProxies)
		{
			proxy.hidden = !proxy.active;
			// TODO remove this after physics are in
			if (proxy.active)
			{
				foreach (var rayOrigin in proxy.rayOrigins.Values)
					m_KeyboardMallets[rayOrigin].CheckForKeyCollision();
			}
		}

		foreach (var kvp in m_DeviceData)
		{
			var deviceData = kvp.Value;
			var mainMenu = deviceData.mainMenu;
			if (mainMenu != null)
			{
				deviceData.toolMenus.RemoveAll((go) => go == null);
				foreach (GameObject go in deviceData.toolMenus)
				{
					go.SetActive(!mainMenu.visible);
				}
			}
		}

		var camera = U.Camera.GetMainCamera();
		// Enable/disable workspace vacuum bounds based on distance to camera
		foreach (var workspace in m_AllWorkspaces)
			workspace.vacuumEnabled = (workspace.transform.position - camera.transform.position).magnitude > kWorkspaceVacuumEnableDistance;

		UpdateMiniWorlds();

#if UNITY_EDITOR
		// HACK: Send a custom event, so that OnSceneGUI gets called, which is requirement for scene picking to occur
		//		Additionally, on some machines it's required to do a delay call otherwise none of this works
		//		I noticed that delay calls were queuing up, so it was necessary to protect against that, so only one is processed
		if (m_UpdatePixelRaycastModule)
		{
			EditorApplication.delayCall += () =>
			{
				if (this != null) // Because this is a delay call, the component will be null when EditorVR closes
				{
					Event e = new Event();
					e.type = EventType.ExecuteCommand;
					VRView.activeView.SendEvent(e);
				}
			};

			m_UpdatePixelRaycastModule = false; // Don't allow another one to queue until the current one is processed
		}
#endif
	}

	private void InitializePlayerHandle()
	{
		m_PlayerHandle = PlayerHandleManager.GetNewPlayerHandle();
		m_PlayerHandle.global = true;
	}

	private Dictionary<Type, List<ActionMap>> CollectToolActionMaps(IEnumerable<Type> toolTypes)
	{
		var toolMaps = new Dictionary<Type, List<ActionMap>>();

		foreach (var t in toolTypes)
		{
			if (!t.IsSubclassOf(typeof(MonoBehaviour)))
				continue;

			var tool = gameObject.AddComponent(t) as ITool;
			List<ActionMap> actionMaps = new List<ActionMap>();

			var customActionMap = tool as ICustomActionMap;
			if (customActionMap != null)
				actionMaps.Add(customActionMap.actionMap);

			var customActionMaps = tool as ICustomActionMaps;
			if (customActionMaps != null)
				actionMaps.AddRange(customActionMaps.actionMaps);

			var standardActionMap = tool as IStandardActionMap;
			if (standardActionMap != null)
				actionMaps.Add(m_StandardToolActionMap);

			toolMaps.Add(t, actionMaps);

			U.Object.Destroy(tool as MonoBehaviour);
		}
		return toolMaps;
	}

	private void CreateDefaultActionMapInputs()
	{
		m_TrackedObjectInput = (TrackedObject)CreateActionMapInput(m_TrackedObjectActionMap, null);
	}

	bool IsPermanentTool(Type type)
	{
		return typeof(ITransformTool).IsAssignableFrom(type)
			|| typeof(SelectionTool).IsAssignableFrom(type)
			|| typeof(ILocomotion).IsAssignableFrom(type);
	}

	private void SpawnDefaultTools()
	{
		// Spawn default tools
		HashSet<InputDevice> devices;
		ITool tool;

		var locomotionTool = typeof(BlinkLocomotionTool);
		if (VRSettings.loadedDeviceName == "Oculus")
			locomotionTool = typeof(JoystickLocomotionTool);

		var transformTool = SpawnTool(typeof(TransformTool), out devices);
		m_TransformTool = transformTool as IGrabObjects;

		foreach (var deviceData in m_DeviceData)
		{
			// Skip keyboard, mouse, gamepads. Selection tool should only be on left and right hands (tagged 0 and 1)
			if (deviceData.Key.tagIndex == -1)
				continue;

			Node? deviceNode = GetDeviceNode(deviceData.Key);

			tool = SpawnTool(typeof(SelectionTool), out devices, deviceData.Key);
			AddToolToDeviceData(tool, devices);
			var selectionTool = tool as SelectionTool;
			selectionTool.node = deviceNode;
			selectionTool.selected += OnAlternateMenuItemSelected; // when a selection occurs in the selection tool, call show in the alternate menu, allowing it to show/hide itself.

			// Using a shared instance of the transform tool across all device tool stacks
			AddToolToStack(deviceData.Key, transformTool);

			if (locomotionTool == typeof(BlinkLocomotionTool))
			{
				tool = SpawnTool(locomotionTool, out devices, deviceData.Key);
				AddToolToDeviceData(tool, devices);
			}

			var mainMenuActivator = m_DeviceData[deviceData.Key].mainMenuActivator = SpawnMainMenuActivator(deviceData.Key);
			mainMenuActivator.node = deviceNode;
			mainMenuActivator.selected += OnMainMenuActivatorSelected;
			mainMenuActivator.hoverStarted += OnMainMenuActivatorHoverStarted;
			mainMenuActivator.hoverEnded += OnMainMenuActivatorHoverEnded;

			var alternateMenu = SpawnAlternateMenu(typeof(RadialMenu), deviceData.Key);
			m_DeviceData[deviceData.Key].alternateMenu = alternateMenu;
			alternateMenu.itemSelected += OnAlternateMenuItemSelected;

			UpdatePlayerHandleMaps();
		}

		if (locomotionTool == typeof(JoystickLocomotionTool))
		{
			tool = SpawnTool(locomotionTool, out devices);
			AddToolToDeviceData(tool, devices);
		}
	}

	private void OnAlternateMenuItemSelected(Node? selectionToolNode)
	{
		if (selectionToolNode == null)
			return;

		var updateMaps = false;
		foreach (var kvp in m_DeviceData)
		{
			Node? node = GetDeviceNode(kvp.Key);
			if (node.HasValue)
			{
				var deviceData = kvp.Value;

				var alternateMenu = deviceData.alternateMenu;
				if (alternateMenu != null)
				{
					alternateMenu.visible = (node.Value == selectionToolNode) && Selection.gameObjects.Length > 0;

					// Hide the main menu if the alternate menu is going to be visible
					var mainMenu = deviceData.mainMenu;
					if (mainMenu != null && alternateMenu.visible)
					{
						mainMenu.visible = false;
						deviceData.restoreMainMenu = false;
					}

					// Move the activator button to an alternate position if the alternate menu will be shown
					var mainMenuActivator = deviceData.mainMenuActivator;
					if (mainMenuActivator != null)
						mainMenuActivator.activatorButtonMoveAway = alternateMenu.visible;

					updateMaps = true;
				}
			}
		}

		if (updateMaps)
			UpdatePlayerHandleMaps();
	}

	private void DisplayAlternateMenu(Node? forNode, GameObject forObject)
	{
		if (forNode == null)
			return;

		var updateMaps = false;
		foreach (var kvp in m_DeviceData)
		{
			Node? node = GetDeviceNode(kvp.Key);
			if (node.HasValue)
			{
				var deviceData = kvp.Value;

				var alternateMenu = deviceData.alternateMenu;
				if (alternateMenu != null)
				{
					alternateMenu.visible = (node.Value == forNode) && (forObject != null);

					// Hide the main menu if the alternate menu is going to be visible
					var mainMenu = deviceData.mainMenu;
					if (mainMenu != null && alternateMenu.visible)
					{
						mainMenu.visible = false;
						deviceData.restoreMainMenu = false;
					}

					// Move the activator button to an alternate position if the alternate menu will be shown
					var mainMenuActivator = deviceData.mainMenuActivator;
					if (mainMenuActivator != null)
						mainMenuActivator.activatorButtonMoveAway = alternateMenu.visible;

					updateMaps = true;
				}
			}
		}

		if (updateMaps)
			UpdatePlayerHandleMaps();
	}

	void OnMainMenuVisiblityChanged(IMainMenu mainMenu)
	{
		UpdatePlayerHandleMaps();
	}

	void OnMainMenuActivatorHoverStarted(Transform rayOrigin)
	{
		ForEachRayOrigin((p, kvp, device, deviceData) =>
		{
			if (kvp.Value == rayOrigin)
			{
				var mainMenu = deviceData.mainMenu;
				if (mainMenu.visible)
				{
					mainMenu.visible = false;
					deviceData.restoreMainMenu = true;
				}
			}
		}, true);
	}

	void OnMainMenuActivatorHoverEnded(Transform rayOrigin)
	{
		ForEachRayOrigin((p, kvp, device, deviceData) =>
		{
			if (kvp.Value == rayOrigin)
			{
				if (deviceData.restoreMainMenu)
				{
					deviceData.mainMenu.visible = true;
					deviceData.restoreMainMenu = false;
				}
			}
		}, true);
	}

	private void OnMainMenuActivatorSelected(Node? activatorNode)
	{
		if (activatorNode == null)
			return;

		foreach (var kvp in m_DeviceData)
		{
			var deviceData = kvp.Value;
			Node? node = GetDeviceNode(kvp.Key);
			if (node.HasValue)
			{
				if (node.Value == activatorNode)
				{
					var mainMenu = deviceData.mainMenu;
					if (mainMenu != null)
						mainMenu.visible = !mainMenu.visible;

					// move to rest position if this is the node that made the selection
					var mainMenuActivator = deviceData.mainMenuActivator;
					if (mainMenuActivator != null)
						mainMenuActivator.activatorButtonMoveAway = false;

					var alternateMenu = deviceData.alternateMenu;
					if (alternateMenu != null)
						alternateMenu.visible = false;
				}
				else if (Selection.gameObjects.Length > 0)
				{
					// Enable the alternate menu on the other hand if the menu was opened on a hand with the alternate menu already enabled
					var alternateMenu = deviceData.alternateMenu;
					if (alternateMenu != null)
					{
						alternateMenu.visible = true;

						var mainMenuActivator = deviceData.mainMenuActivator;
						if (mainMenuActivator != null)
							mainMenuActivator.activatorButtonMoveAway = alternateMenu.visible;

						// Close a menu if it was open, since it would conflict with the alternate menu
						var mainMenu = deviceData.mainMenu;
						if (mainMenu != null)
						{
							mainMenu.visible = false;
							deviceData.restoreMainMenu = false;
						}
					}
				}
			}
		}
	}

	private void SpawnActions()
	{
		IEnumerable<Type> actionTypes = U.Object.GetImplementationsOfInterface(typeof(IAction));
		m_AllActions = new List<IAction>();
		foreach (Type actionType in actionTypes)
		{
			// Don't treat vanilla actions or tool actions as first class actions
			if (actionType.IsNested || !typeof(MonoBehaviour).IsAssignableFrom(actionType))
				continue;

			var action = U.Object.AddComponent(actionType, gameObject) as IAction;
			var attribute = (ActionMenuItemAttribute)actionType.GetCustomAttributes(typeof(ActionMenuItemAttribute), false).FirstOrDefault();

			ConnectInterfaces(action);

			if (attribute != null)
			{
				var actionMenuData = new ActionMenuData()
				{
					name = attribute.name,
					sectionName = attribute.sectionName,
					priority = attribute.priority,
					action = action,
				};

				m_MenuActions.Add(actionMenuData);
			}

			m_AllActions.Add(action);
		}

		m_MenuActions.Sort((x,y) => y.priority.CompareTo(x.priority));
	}

	private void CreateAllProxies()
	{
		foreach (Type proxyType in U.Object.GetImplementationsOfInterface(typeof(IProxy)))
		{
			IProxy proxy = U.Object.CreateGameObjectWithComponent(proxyType, VRView.viewerPivot) as IProxy;
			proxy.trackedObjectInput = m_TrackedObjectInput;
			foreach (var rayOriginPair in proxy.rayOrigins)
			{
				var rayOriginPairValue = rayOriginPair.Value;
				var rayTransform = U.Object.Instantiate(m_ProxyRayPrefab.gameObject, rayOriginPairValue).transform;
				rayTransform.position = rayOriginPairValue.position;
				rayTransform.rotation = rayOriginPairValue.rotation;
				m_DefaultRays.Add(rayOriginPairValue, rayTransform.GetComponent<DefaultProxyRay>());

				var malletTransform = U.Object.Instantiate(m_KeyboardMalletPrefab.gameObject, rayOriginPairValue).transform;
				malletTransform.position = rayOriginPairValue.position;
				malletTransform.rotation = rayOriginPairValue.rotation;
				var mallet = malletTransform.GetComponent<KeyboardMallet>();
				mallet.gameObject.SetActive(false);
				m_KeyboardMallets.Add(rayOriginPairValue, mallet);
			}

			m_AllProxies.Add(proxy);
		}
	}

	private void UpdateDefaultProxyRays()
	{
		// Set ray lengths based on renderer bounds
		foreach (var proxy in m_AllProxies)
		{
			if (!proxy.active)
				continue;

			foreach (var rayOrigin in proxy.rayOrigins.Values)
			{
				var distance = kDefaultRayLength;

				// Give UI priority over scene objects (e.g. For the TransformTool, handles are generally inside of the
				// object, so visually show the ray terminating there instead of the object; UI is already given
				// priority on the input side)
				var uiEventData = m_InputModule.GetPointerEventData(rayOrigin);
				if (uiEventData != null && uiEventData.pointerCurrentRaycast.isValid)
				{
					// Set ray length to distance to UI objects
					distance = uiEventData.pointerCurrentRaycast.distance;
				}
				else
				{
					// If not hitting UI, then check standard raycast and approximate bounds to set distance
					var go = GetFirstGameObject(rayOrigin);
					if (go != null)
					{
						var ray = new Ray(rayOrigin.position, rayOrigin.forward);
						var newDist = distance;
						foreach (var renderer in go.GetComponentsInChildren<Renderer>())
						{
							if (renderer.bounds.IntersectRay(ray, out newDist) && newDist > 0)
								distance = Mathf.Min(distance, newDist);
						}
					}
				}
				m_DefaultRays[rayOrigin].SetLength(distance);
			}
		}
	}

	private void CreateEventSystem()
	{
		// Create event system, input module, and event camera
		U.Object.AddComponent<EventSystem>(gameObject);

		m_InputModule = U.Object.AddComponent<MultipleRayInputModule>(gameObject);
		m_InputModule.getPointerLength = GetPointerLength;

		m_EventCamera = U.Object.Instantiate(m_EventCameraPrefab.gameObject, transform).GetComponent<Camera>();
		m_EventCamera.enabled = false;
		m_InputModule.eventCamera = m_EventCamera;

		m_InputModule.rayEntered += m_DragAndDropModule.OnRayEntered;
		m_InputModule.rayExited += m_DragAndDropModule.OnRayExited;
		m_InputModule.dragStarted += m_DragAndDropModule.OnDragStarted;
		m_InputModule.dragEnded += m_DragAndDropModule.OnDragEnded;

#if ENABLE_MINIWORLD_RAY_SELECTION
		m_InputModule.preProcessRaycastSources = PreProcessRaycastSources;
		m_InputModule.preProcessRaycastSource = PreProcessRaycastSource;
		m_InputModule.postProcessRaycastSources = PostProcessRaycastSources;
#endif

		ForEachRayOrigin((proxy, rayOriginPair, device, deviceData) =>
		{
			// Create ui action map input for device.
			if (deviceData.uiInput == null)
			{
				deviceData.uiInput = CreateActionMapInput(m_InputModule.actionMap, device);
				deviceData.directSelectInput = CreateActionMapInput(m_DirectSelectActionMap, device);
			}

			// Add RayOrigin transform, proxy and ActionMapInput references to input module list of sources
			m_InputModule.AddRaycastSource(proxy, rayOriginPair.Key, deviceData.uiInput);
		});
	}

	void ForEachRayOrigin(Action<IProxy, KeyValuePair<Node, Transform>, InputDevice, DeviceData> callback, bool activeOnly = false)
	{
		foreach (var proxy in m_AllProxies)
		{
			if (activeOnly && !proxy.active)
				continue;

			foreach (var rayOriginPair in proxy.rayOrigins)
			{
				foreach (var device in GetSystemDevices())
				{
					// Find device tagged with the node that matches this RayOrigin node
					var node = GetDeviceNode(device);
					if (node.HasValue && node.Value == rayOriginPair.Key)
					{
						DeviceData deviceData;
						if (m_DeviceData.TryGetValue(device, out deviceData))
							callback(proxy, rayOriginPair, device, deviceData);

						break;
					}
				}
			}
		}
	}

	void CreateSpatialSystem()
	{
		// Create event system, input module, and event camera
		m_SpatialHashModule = U.Object.AddComponent<SpatialHashModule>(gameObject);
		m_SpatialHashModule.Setup();
		m_IntersectionModule = U.Object.AddComponent<IntersectionModule>(gameObject);
		ConnectInterfaces(m_IntersectionModule);
		m_IntersectionModule.Setup(m_SpatialHashModule.spatialHash);

		ForEachRayOrigin((proxy, rayOriginPair, device, deviceData) =>
		{
			var tester = rayOriginPair.Value.GetComponentInChildren<IntersectionTester>();
			tester.active = proxy.active;
			m_IntersectionModule.AddTester(tester);
		});
	}

	GameObject InstantiateUI(GameObject prefab)
	{
		var go = U.Object.Instantiate(prefab, transform);
		foreach (var canvas in go.GetComponentsInChildren<Canvas>())
			canvas.worldCamera = m_EventCamera;

		foreach (var inputField in go.GetComponentsInChildren<InputField>())
		{
			if (inputField is NumericInputField)
				inputField.spawnKeyboard = SpawnNumericKeyboard;
			else if (inputField is StandardInputField)
				inputField.spawnKeyboard = SpawnAlphaNumericKeyboard;
		}

		foreach (var component in go.GetComponentsInChildren<Component>(true))
			ConnectInterfaces(component);
		return go;
	}

	private GameObject InstantiateMenuUI(Node node,MenuOrigin origin,GameObject prefab)
	{
		var go = U.Object.Instantiate(prefab,transform);
		foreach(Canvas canvas in go.GetComponentsInChildren<Canvas>())
			canvas.worldCamera = m_EventCamera;

		// the menu needs to be on the opposite hand to the tool
		if (node == Node.LeftHand)
			node = Node.RightHand;
		else if(node == Node.RightHand)
			node = Node.LeftHand;

		// HACK: if not using this bool, the CreatePrimitiveMenu would be attached to both nodes
		bool once = false;
		ForEachRayOrigin((proxy,rayOriginPair,device,deviceData) =>
		{
			if (once)
				return;

			Dictionary<Node,Transform> tempOrigin = null;

			if(origin == MenuOrigin.Main)
				tempOrigin = proxy.menuOrigins;
			else if(origin == MenuOrigin.Alternate)
				tempOrigin = proxy.alternateMenuOrigins;

			Transform parent;
			if(tempOrigin != null && tempOrigin.TryGetValue(node,out parent))
			{
				once = true;

				if (go.GetComponent<CreatePrimitiveMenu>() != null)
					ConnectInterfaces(go.GetComponent<CreatePrimitiveMenu>(),device);

				go.transform.SetParent(parent);
				go.transform.localPosition = Vector3.zero;
				go.transform.localRotation = Quaternion.identity;

				deviceData.toolMenus.Add(go);
				m_DeviceData[device].mainMenu.visible = false;
			}
		}, true);



		return go;
	}

	private KeyboardUI SpawnNumericKeyboard()
	{
		if (m_StandardKeyboard != null)
			m_StandardKeyboard.gameObject.SetActive(false);
		
		// Check if the prefab has already been instantiated
		if (m_NumericKeyboard == null)
		{
			m_NumericKeyboard = U.Object.Instantiate(m_NumericKeyboardPrefab.gameObject, U.Camera.GetViewerPivot()).GetComponent<KeyboardUI>();
			m_NumericKeyboard.GetComponent<Canvas>().worldCamera = m_EventCamera;
			m_NumericKeyboard.orientationChanged += KeyboardOrientationChanged;
		}
		return m_NumericKeyboard;
	}

	private KeyboardUI SpawnAlphaNumericKeyboard()
	{
		if (m_NumericKeyboard != null)
			m_NumericKeyboard.gameObject.SetActive(false);
		
		// Check if the prefab has already been instantiated
		if (m_StandardKeyboard == null)
		{
			m_StandardKeyboard = U.Object.Instantiate(m_StandardKeyboardPrefab.gameObject, U.Camera.GetViewerPivot()).GetComponent<KeyboardUI>();
			m_StandardKeyboard.GetComponent<Canvas>().worldCamera = m_EventCamera;
			m_StandardKeyboard.orientationChanged += KeyboardOrientationChanged;
		}
		return m_StandardKeyboard;
	}

	void KeyboardOrientationChanged(bool isHorizontal)
	{
		foreach (var kvp in m_KeyboardMallets)
		{
			var mallet = kvp.Value;
			var dpr = kvp.Key.GetComponentInChildren<DefaultProxyRay>();
			if (isHorizontal)
			{
				mallet.Show();
				dpr.Hide();
			}
			else
			{
				mallet.Hide();
				dpr.Show();
			}
		}
	}

	private ActionMapInput CreateActionMapInput(ActionMap map, InputDevice device)
	{
		// Check for improper use of action maps first
		if (device != null && !IsValidActionMapForDevice(map, device))
			return null;

		var devices = device == null ? GetSystemDevices() : new InputDevice[] { device };

		var actionMapInput = ActionMapInput.Create(map);
		// It's possible that there are no suitable control schemes for the device that is being initialized, 
		// so ActionMapInput can't be marked active
		var successfulInitialization = false;
		if (actionMapInput.TryInitializeWithDevices(devices))
			successfulInitialization = true;
		else
		{
			// For two-handed tools, the single device won't work, so collect the devices from the action map
			devices = U.Input.CollectInputDevicesFromActionMaps(new List<ActionMap>() { map });
			if (actionMapInput.TryInitializeWithDevices(devices))
				successfulInitialization = true;
		}

		if (successfulInitialization)
		{
			actionMapInput.autoReinitialize = false;
			actionMapInput.active = true;
		}

		return actionMapInput;
	}

	private void UpdatePlayerHandleMaps()
	{
		var maps = m_PlayerHandle.maps;
		maps.Clear();

		foreach (DeviceData deviceData in m_DeviceData.Values)
		{
			var mainMenu = deviceData.mainMenu;
			if (mainMenu != null && mainMenu.visible)
				AddActionMapInputs(mainMenu, maps);

			var alternateMenu = deviceData.alternateMenu;
			if (alternateMenu != null && alternateMenu.visible)
				AddActionMapInputs(alternateMenu, maps);

			maps.Add(deviceData.directSelectInput);

			maps.Add(deviceData.uiInput);
		}

		foreach (var ray in m_MiniWorldRays.Values)
		{
			maps.Add(ray.directSelectInput);
#if ENABLE_MINIWORLD_RAY_SELECTION
			maps.Add(ray.uiInput);
#endif
		}

		maps.Add(m_TrackedObjectInput);

		foreach (DeviceData deviceData in m_DeviceData.Values)
		{
			foreach (ITool tool in deviceData.tools)
				AddActionMapInputs(tool, maps);
		}
	}

	private void AddActionMapInputs(object obj, List<ActionMapInput> maps)
	{
		IStandardActionMap standardActionMap = obj as IStandardActionMap;
		if (standardActionMap != null)
		{
			if (!maps.Contains(standardActionMap.standardInput))
				maps.Add(standardActionMap.standardInput);
		}

		ICustomActionMap customActionMap = obj as ICustomActionMap;
		if (customActionMap != null)
		{
			if (!maps.Contains(customActionMap.actionMapInput))
				maps.Add(customActionMap.actionMapInput);
		}

		ICustomActionMaps customActionMaps = obj as ICustomActionMaps;
		if (customActionMaps != null)
		{
			foreach (var input in customActionMaps.actionMapInputs)
			{
				if (!maps.Contains(input))
					maps.Add(input);
			}
		}
	}

	private void LogError(string error)
	{
		Debug.LogError(string.Format("EVR: {0}", error));
	}

	/// <summary>
	/// Spawn a tool on a tool stack for a specific device (e.g. right hand).
	/// </summary>
	/// <param name="toolType">The tool to spawn</param>
	/// <param name="usedDevices">A list of the used devices coming from the action map</param>
	/// <param name="device">The input device whose tool stack the tool should be spawned on (optional). If not 
	/// specified, then it uses the action map to determine which devices the tool should be spawned on.</param>
	/// <returns> Returns tool that was spawned or null if the spawn failed.</returns>
	private ITool SpawnTool(Type toolType, out HashSet<InputDevice> usedDevices, InputDevice device = null)
	{
		usedDevices = new HashSet<InputDevice>();
		if (!typeof(ITool).IsAssignableFrom(toolType))
			return null;

		var deviceSlots = new HashSet<DeviceSlot>();
		var tool = U.Object.AddComponent(toolType, gameObject) as ITool;

		var actionMapInputs = new List<ActionMapInput>();
		ConnectActionMaps(tool, device, actionMapInputs);
		foreach (var actionMapInput in actionMapInputs)
		{
			usedDevices.UnionWith(actionMapInput.GetCurrentlyUsedDevices());
			U.Input.CollectDeviceSlotsFromActionMapInput(actionMapInput, ref deviceSlots);
		}

		ConnectInterfaces(tool, device);

		return tool;
	}

	private void AddToolToDeviceData(ITool tool, HashSet<InputDevice> devices)
	{
		foreach (var dev in devices)
			AddToolToStack(dev, tool);
	}

	private IMainMenu SpawnMainMenu(Type type, InputDevice device, bool visible)
	{
		if (!typeof(IMainMenu).IsAssignableFrom(type))
			return null;

		var mainMenu = U.Object.AddComponent(type, gameObject) as IMainMenu;
		ConnectActionMaps(mainMenu, device);
		ConnectInterfaces(mainMenu, device);
		mainMenu.visible = visible;

		return mainMenu;
	}

	private IAlternateMenu SpawnAlternateMenu (Type type, InputDevice device)
	{
		if (!typeof(IAlternateMenu).IsAssignableFrom(type))
			return null;

		var alternateMenu = U.Object.AddComponent(type, gameObject) as IAlternateMenu;
		ConnectActionMaps(alternateMenu, device);
		ConnectInterfaces(alternateMenu, device);
		alternateMenu.visible = false;

		return alternateMenu;
	}

	private MainMenuActivator SpawnMainMenuActivator (InputDevice device)
	{
		var mainMenuActivator = U.Object.Instantiate(m_MainMenuActivatorPrefab.gameObject).GetComponent<MainMenuActivator>();
		ConnectInterfaces(mainMenuActivator, device);

		return mainMenuActivator;
	}

	private Node? GetDeviceNode(InputDevice device)
	{
		var tags = InputDeviceUtility.GetDeviceTags(device.GetType());
		if (tags != null && device.tagIndex != -1)
		{
			var tag = tags[device.tagIndex];
			Node node;
			if (m_TagToNode.TryGetValue(tag, out node))
				return node;
		}

		return null;
	}

	private void ConnectActionMaps(object obj, InputDevice device, List<ActionMapInput> actionMapInputs = null)
	{
		var standardMap = obj as IStandardActionMap;
		if (standardMap != null)
		{
			standardMap.standardInput = (Standard)CreateActionMapInput(m_StandardToolActionMap, device);
			if (actionMapInputs != null)
				actionMapInputs.Add(standardMap.standardInput);
		}

		var trackedObjectMap = obj as ITrackedObjectActionMap;
		if (trackedObjectMap != null)
		{
			trackedObjectMap.trackedObjectInput = m_TrackedObjectInput;
		}

		var customMap = obj as ICustomActionMap;
		if (customMap != null)
		{
			customMap.actionMapInput = CreateActionMapInput(customMap.actionMap, device);
			if (actionMapInputs != null)
				actionMapInputs.Add(customMap.actionMapInput);
		}

		var customMaps = obj as ICustomActionMaps;
		if (customMaps != null)
		{
			var actionMaps = customMaps.actionMaps;
			var inputs = new ActionMapInput[actionMaps.Length];
			for (int i = 0; i < actionMaps.Length; i++)
			{
				var input = CreateActionMapInput(actionMaps[i], device);
				inputs[i] = input;
				if (actionMapInputs != null)
					actionMapInputs.Add(input);
			}
			customMaps.actionMapInputs = inputs;
		}
	}

	private void ConnectInterfaces(object obj)
	{
		ConnectInterfaces(obj, null);
	}

	private void ConnectInterfaces(object obj, InputDevice device)
	{
		var connectInterfaces = obj as IConnectInterfaces;
		if (connectInterfaces != null)
			connectInterfaces.connectInterfaces = ConnectInterfaces;

		var menuOrigins = obj as IMenuOrigins;

		if (device != null)
		{
			foreach (var proxy in m_AllProxies)
			{
				if (!proxy.active)
					continue;

				var node = GetDeviceNode(device);
				if (node.HasValue)
				{
					bool continueSearching = true;

					Transform rayOrigin;

					var iTool = obj as ITool;
					if(iTool != null)
					{
						iTool.selfNode = node.Value;
					}

					var ray = obj as IRay;
					if (ray != null && proxy.rayOrigins.TryGetValue(node.Value, out rayOrigin))
					{
						ray.rayOrigin = rayOrigin;

						// Specific proxy ray setting
						DefaultProxyRay dpr = null;
						var customRay = obj as ICustomRay;
						if (customRay != null)
						{
							dpr = rayOrigin.GetComponentInChildren<DefaultProxyRay>();
							customRay.showDefaultRay = dpr.Show;
							customRay.hideDefaultRay = dpr.Hide;
						}

						var lockableRay = obj as ILockRay;
						if (lockableRay != null)
						{
							dpr = dpr ?? rayOrigin.GetComponentInChildren<DefaultProxyRay>();
							lockableRay.lockRay = dpr.LockRay;
							lockableRay.unlockRay = dpr.UnlockRay;
						}

						continueSearching = false;
					}

					if (menuOrigins != null)
					{
						Transform mainMenuOrigin;
						if (proxy.menuOrigins.TryGetValue(node.Value, out mainMenuOrigin))
						{
							menuOrigins.menuOrigin = mainMenuOrigin;
							Transform alternateMenuOrigin;
							if (proxy.alternateMenuOrigins.TryGetValue(node.Value, out alternateMenuOrigin))
								menuOrigins.alternateMenuOrigin = alternateMenuOrigin;
						}
					}

					if (!continueSearching)
						break;
				}
			}
		}

		var locomotion = obj as ILocomotion;
		if (locomotion != null)
			locomotion.viewerPivot = VRView.viewerPivot;

		var instantiateUI = obj as IInstantiateUI;
		if (instantiateUI != null)
			instantiateUI.instantiateUI = InstantiateUI;

		var createWorkspace = obj as ICreateWorkspace;
		if (createWorkspace != null)
			createWorkspace.createWorkspace = CreateWorkspace;

		var instantiateMenuUI = obj as IInstantiateMenuUI;
		if(instantiateMenuUI != null)
			instantiateMenuUI.instantiateMenuUI = InstantiateMenuUI;

		var raycaster = obj as IRaycaster;
		if (raycaster != null)
			raycaster.getFirstGameObject = GetFirstGameObject;

		var highlight = obj as IHighlight;
		if (highlight != null)
			highlight.setHighlight = m_HighlightModule.SetHighlight;

		var placeObjects = obj as IPlaceObjects;
		if (placeObjects != null)
			placeObjects.placeObject = PlaceObject;

		var locking = obj as ILocking;
		if (locking != null)
		{
			locking.toggleLocked = m_LockModule.ToggleLocked;
			locking.getLocked = m_LockModule.IsLocked;
			locking.checkHover = m_LockModule.CheckHover;
		}

		var positionPreview = obj as IPreview;
		if (positionPreview != null)
		{
			positionPreview.preview = m_ObjectPlacementModule.Preview;
			positionPreview.getPreviewOriginForRayOrigin = GetPreviewOriginForRayOrigin;
		}

		var selectionChanged = obj as ISelectionChanged;
		if (selectionChanged != null)
			m_SelectionChanged += selectionChanged.OnSelectionChanged;

		var toolActions = obj as IToolActions;
		if (toolActions != null)
		{
			var actions = toolActions.toolActions;
			foreach (var action in actions)
			{
				var actionMenuData = new ActionMenuData()
				{
					name = action.GetType().Name,
					sectionName = ActionMenuItemAttribute.kDefaultActionSectionName,
					priority = int.MaxValue,
					action = action,
				};
				m_MenuActions.Add(actionMenuData);
			}
			UpdateAlternateMenuActions();
		}

		var directSelection = obj as IDirectSelection;
		if (directSelection != null)
			directSelection.getDirectSelection = GetDirectSelection;

		var grabObjects = obj as IGrabObjects;
		if (grabObjects != null)
		{
			grabObjects.canGrabObject = CanGrabObject;
			grabObjects.grabObject = GrabObject;
			grabObjects.dropObject = DropObject;
		}

		var snapping = obj as ISnapping;
		if (snapping != null)
		{
			snapping.onSnapEnded = m_SnappingModule.OnSnapEnded;
			snapping.onSnapHeld = m_SnappingModule.OnSnapHeld;
			snapping.onSnapStarted = m_SnappingModule.OnSnapStarted;
			snapping.onSnapUpdate = m_SnappingModule.OnSnapUpdate;
		}
		
		var spatialHash = obj as ISpatialHash;
		if (spatialHash != null)
		{
			spatialHash.addObjectToSpatialHash = AddObjectToSpatialHash;
			spatialHash.removeObjectFromSpatialHash = RemoveObjectFromSpatialHash;
		}


		var folderList = obj as IProjectFolderList;
		if (folderList != null)
			folderList.getFolderData = GetFolderData;

		var filterUI = obj as IFilterUI;
		if (filterUI != null)
			filterUI.getFilterList = GetFilterList;

		var mainMenu = obj as IMainMenu;
		if (mainMenu != null)
		{
			mainMenu.menuTools = m_MainMenuTools;
			mainMenu.menuModules = m_MainMenuModules;
			mainMenu.selectTool = SelectTool;
			mainMenu.menuWorkspaces = m_AllWorkspaceTypes.ToList();
			mainMenu.node = GetDeviceNode(device);
			mainMenu.menuVisibilityChanged += OnMainMenuVisiblityChanged;
		}

		var alternateMenu = obj as IAlternateMenu;
		if (alternateMenu != null)
		{
			alternateMenu.menuActions = m_MenuActions;
			alternateMenu.node = GetDeviceNode(device);
			alternateMenu.setup();
		}
	}

	private void DisconnectInterfaces(object obj)
	{
		var selectionChanged = obj as ISelectionChanged;
		if (selectionChanged != null)
			m_SelectionChanged -= selectionChanged.OnSelectionChanged;

		var toolActions = obj as IToolActions;
		if (toolActions != null)
		{
			var actions = toolActions.toolActions;
			m_MenuActions = m_MenuActions.Where(a => !actions.Contains(a.action)).ToList();
			UpdateAlternateMenuActions();
		}
	}

	private void UpdateAlternateMenuActions()
	{
		foreach (var deviceData in m_DeviceData.Values)
		{
			var altMenu = deviceData.alternateMenu;
			if (altMenu != null)
				altMenu.menuActions = m_MenuActions;
		}
	}

	// NOTE: This is for the length of the pointer object, not the length of the ray coming out of the pointer
	private float GetPointerLength(Transform rayOrigin)
	{
		var length = 0f;

		// Check if this is a MiniWorldRay
		MiniWorldRay ray;
		if (m_MiniWorldRays.TryGetValue(rayOrigin, out ray))
			rayOrigin = ray.originalRayOrigin;

		DefaultProxyRay dpr;
		if (m_DefaultRays.TryGetValue(rayOrigin, out dpr))
		{
			length = dpr.pointerLength;
			// If this is a MiniWorldRay, scale the pointer length to the correct size relative to MiniWorld objects
			if (ray != null)
			{
				var miniWorld = ray.miniWorld;
				// As the miniworld gets smaller, the ray length grows, hence localScale.Inverse().
				// Assume that both transforms have uniform scale, so we just need .x
				length *= miniWorld.referenceTransform.TransformVector(miniWorld.miniWorldTransform.localScale.Inverse()).x;
			}
		}

		return length;
	}

	private InputDevice GetInputDeviceForTool(ITool tool)
	{
		foreach (var kvp in m_DeviceData)
		{
			foreach (var t in kvp.Value.tools)
			{
				if (t == tool)
					return kvp.Key;
			}
		}
		return null;
	}

	private bool SelectTool(Node targetNode, Type toolType)
	{
		InputDevice deviceToAssignTool = null;
		foreach (var kvp in m_DeviceData)
		{
			Node? node = GetDeviceNode(kvp.Key);
			if (node.HasValue && node.Value == targetNode)
			{
				deviceToAssignTool = kvp.Key;
				break;
			}
		}

		if (deviceToAssignTool == null)
			return false;

		var spawnTool = true;
		DeviceData deviceData;
		if (m_DeviceData.TryGetValue(deviceToAssignTool, out deviceData))
		{
			// If this tool was on the current device already, then simply remove it
			if (deviceData.currentTool != null && deviceData.currentTool.GetType() == toolType)
			{
				DespawnTool(deviceData, deviceData.currentTool);

				// Don't spawn a new tool, since we are only removing the old tool
				spawnTool = false;
			}
		}

		if (spawnTool)
		{
			// Spawn tool and collect all devices that this tool will need
			HashSet<InputDevice> usedDevices;
			var newTool = SpawnTool(toolType, out usedDevices, deviceToAssignTool);

			// It's possible this tool uses no action maps, so at least include the device this tool was spawned on
			if (usedDevices.Count == 0)
				usedDevices.Add(deviceToAssignTool);

			// Exclusive mode tools always take over all tool stacks
			if (newTool is IExclusiveMode) 
			{
				foreach (var dev in m_DeviceData.Keys)
					usedDevices.Add(dev);
			}

			foreach (var dev in usedDevices)
			{
				deviceData = m_DeviceData[dev];
				if (deviceData.currentTool != null) // Remove the current tool on all devices this tool will be spawned on
					DespawnTool(deviceData, deviceData.currentTool);

				AddToolToStack(dev, newTool);
			}
		}

		UpdatePlayerHandleMaps();

		return true;
	}

	private void DespawnTool(DeviceData deviceData, ITool tool)
	{
		if (!IsPermanentTool(tool.GetType()))
		{
			// Remove the tool if it is the current tool on this device tool stack
			if (deviceData.currentTool == tool)
			{
				if (deviceData.tools.Peek() != deviceData.currentTool)
					Debug.LogError("Tool at top of stack is not current tool.");

				deviceData.tools.Pop();
				deviceData.currentTool = deviceData.tools.Peek();

				// Pop this tool of any other stack that references it (for single instance tools)
				foreach (var otherDeviceData in m_DeviceData.Values) 
				{
					if (otherDeviceData != deviceData) 
					{
						if (otherDeviceData.currentTool == tool) 
						{
							otherDeviceData.tools.Pop();
							otherDeviceData.currentTool = otherDeviceData.tools.Peek();

							if (tool is IExclusiveMode)
								SetToolsEnabled(otherDeviceData, true);
						}
					}
				}
			}
			DisconnectInterfaces(tool);

			// Exclusive tools disable other tools underneath, so restore those
			if (tool is IExclusiveMode)
				SetToolsEnabled(deviceData, true);

			U.Object.Destroy(tool as MonoBehaviour);
		}
	}

	void SetToolsEnabled(DeviceData deviceData, bool value)
	{
		foreach (var t in deviceData.tools) 
		{
			var mb = t as MonoBehaviour;
			mb.enabled = value;
		}
	}

	private bool IsValidActionMapForDevice(ActionMap actionMap, InputDevice device)
	{
		var untaggedDevicesFound = 0;
		var taggedDevicesFound = 0;
		var nonMatchingTagIndices = 0;
		var matchingTagIndices = 0;

		if (actionMap == null)
			return false;

		foreach (var scheme in actionMap.controlSchemes)
		{
			foreach (var serializableDeviceType in scheme.deviceSlots)
			{
				if (serializableDeviceType.tagIndex != -1)
				{
					taggedDevicesFound++;
					if (serializableDeviceType.tagIndex != device.tagIndex)
						nonMatchingTagIndices++;
					else
						matchingTagIndices++;
				}
				else
					untaggedDevicesFound++;
			}
		}

		if (nonMatchingTagIndices > 0 && matchingTagIndices == 0)
		{
			LogError(string.Format("The action map {0} contains a specific device tag, but is being spawned on the wrong device tag", actionMap));
			return false;
		}

		if (taggedDevicesFound > 0 && untaggedDevicesFound != 0)
		{
			LogError(string.Format("The action map {0} contains both a specific device tag and an unspecified tag, which is not supported", actionMap.name));
			return false;
		}

		return true;
	}

	private void AddToolToStack(InputDevice device, ITool tool)
	{
		if (tool != null)
		{
			var deviceData = m_DeviceData[device];

			// Exclusive tools render other tools disabled while they are on the stack
			if (tool is IExclusiveMode)
				SetToolsEnabled(deviceData, false);

			deviceData.tools.Push(tool);
			deviceData.currentTool = tool;
		}
	}

	private void CreateWorkspace<T>(Action<Workspace> createdCallback = null) where T : Workspace
	{
		CreateWorkspace(typeof(T), createdCallback);
	}

	private void CreateWorkspace(Type t, Action<Workspace> createdCallback = null)
	{
		if (!typeof(Workspace).IsAssignableFrom(t))
			return;

		var defaultOffset = Workspace.kDefaultOffset;
		var defaultTilt = Workspace.kDefaultTilt;

		var cameraTransform = U.Camera.GetMainCamera().transform;
		var headPosition = cameraTransform.position;
		var headRotation = U.Math.ConstrainYawRotation(cameraTransform.rotation);

		float arcLength = Mathf.Atan(Workspace.kDefaultBounds.x /
			(defaultOffset.z - Workspace.kDefaultBounds.z * 0.5f)) * Mathf.Rad2Deg		//Calculate arc length at front of workspace
			+ kWorkspaceAnglePadding;													//Need some extra padding because workspaces are tilted
		float heightOffset = Workspace.kDefaultBounds.y + kWorkspaceYPadding;			//Need padding in Y as well

		float currentRotation = arcLength;
		float currentHeight = 0;

		int count = 0;
		int direction = 1;
		Vector3 halfBounds = Workspace.kDefaultBounds * 0.5f;

		Vector3 position;
		Quaternion rotation;
		var viewerPivot = U.Camera.GetViewerPivot();

		// Spawn to one of the sides of the player instead of directly in front of the player
		do
		{
			//The next position will be rotated by currentRotation, as if the hands of a clock
			Quaternion rotateAroundY = Quaternion.AngleAxis(currentRotation * direction, Vector3.up);
			position = headPosition + headRotation * rotateAroundY * defaultOffset + Vector3.up * currentHeight;
			rotation = headRotation * rotateAroundY * defaultTilt;

			//Every other iteration, rotate a little further
			if (direction < 0)
				currentRotation += arcLength;

			//Switch directions every iteration (left, right, left, right)
			direction *= -1;

			//If we've one more than half way around, we have tried the whole circle, bump up one level and keep trying
			if (currentRotation > 180)
			{
				direction = -1;
				currentRotation = 0;
				currentHeight += heightOffset;
			}
		}
		//While the current position is occupied, try a new one
		while (Physics.CheckBox(position, halfBounds, rotation, ~LayerMask.NameToLayer("UI")) && count++ < kMaxWorkspacePlacementAttempts) ;

		Workspace workspace = (Workspace)U.Object.CreateGameObjectWithComponent(t, viewerPivot);
		m_AllWorkspaces.Add(workspace);
		workspace.destroyed += OnWorkspaceDestroyed;
		workspace.isMiniWorldRay = IsMiniWorldRay;
		ConnectInterfaces(workspace);
		workspace.transform.position = position;
		workspace.transform.rotation = rotation;

		//Explicit setup call (instead of setting up in Awake) because we need interfaces to be hooked up first
		workspace.Setup();

		if (createdCallback != null)
			createdCallback(workspace);

		var projectFolderList = workspace as IProjectFolderList;
		if (projectFolderList != null)
			m_ProjectFolderLists.Add(projectFolderList);

		var filterUI = workspace as IFilterUI;
		if (filterUI != null)
			m_FilterUIs.Add(filterUI);

		var miniWorld = workspace as IMiniWorld;
		if (miniWorld != null)
		{
			m_MiniWorlds.Add(miniWorld);

#if ENABLE_MINIWORLD_RAY_SELECTION
			miniWorld.preProcessRender = PreProcessMiniWorldRender;
			miniWorld.postProcessRender = PostProcessMiniWorldRender;
#endif

			ForEachRayOrigin((proxy, rayOriginPair, device, deviceData) =>
			{
				var miniWorldRayOrigin = InstantiateMiniWorldRay();
				miniWorldRayOrigin.parent = workspace.transform;

#if ENABLE_MINIWORLD_RAY_SELECTION
				var uiInput = CreateActionMapInput(m_InputModule.actionMap, device);
				uiInput.active = false;
#endif

				var directSelectInput = CreateActionMapInput(m_DirectSelectActionMap, device);
				directSelectInput.active = false;

#if ENABLE_MINIWORLD_RAY_SELECTION
				// Add RayOrigin transform, proxy and ActionMapInput references to input module list of sources
				m_InputModule.AddRaycastSource(proxy, rayOriginPair.Key, uiInput, miniWorldRayOrigin);
#endif

				var tester = miniWorldRayOrigin.GetComponentInChildren<IntersectionTester>();
				tester.active = false;

				m_MiniWorldRays[miniWorldRayOrigin] = new MiniWorldRay
				{
					originalRayOrigin = rayOriginPair.Value,
					originalDirectSelectInput = deviceData.directSelectInput,
					miniWorld = miniWorld,
					proxy = proxy,
					node = rayOriginPair.Key,
#if ENABLE_MINIWORLD_RAY_SELECTION
					uiInput = uiInput,
#endif
					directSelectInput = directSelectInput,
					tester = tester
				};

				m_IntersectionModule.AddTester(tester);
			});
		}

		UpdatePlayerHandleMaps();
	}

	/// <summary>
	/// Re-use DefaultProxyRay and strip off objects and components not needed for MiniWorldRays
	/// </summary>
	Transform InstantiateMiniWorldRay()
	{
		var miniWorldRay = U.Object.Instantiate(m_ProxyRayPrefab.gameObject).transform;
		U.Object.Destroy(miniWorldRay.GetComponent<DefaultProxyRay>());

		var renderers = miniWorldRay.GetComponentsInChildren<Renderer>();
		foreach (var renderer in renderers)
		{
			if (!renderer.GetComponent<IntersectionTester>())
				U.Object.Destroy(renderer.gameObject);
			else
				renderer.enabled = false;
		}

		return miniWorldRay;
	}

	private void OnWorkspaceDestroyed(Workspace workspace)
	{
		m_AllWorkspaces.Remove(workspace);

		DisconnectInterfaces(workspace);

		var projectFolderList = workspace as IProjectFolderList;
		if (projectFolderList != null)
			m_ProjectFolderLists.Remove(projectFolderList);

		var filterUI = workspace as IFilterUI;
		if (filterUI != null)
			m_FilterUIs.Remove(filterUI);

		var miniWorld = workspace as IMiniWorld;
		if (miniWorld != null)
		{
			//Clean up MiniWorldRays
			m_MiniWorlds.Remove(miniWorld);
			var miniWorldRaysCopy = new Dictionary<Transform, MiniWorldRay>(m_MiniWorldRays);
			foreach (var ray in miniWorldRaysCopy)
			{
				var miniWorldRay = ray.Value;
				var maps = m_PlayerHandle.maps;
				if (miniWorldRay.miniWorld == miniWorld)
				{
					var rayOrigin = ray.Key;
#if ENABLE_MINIWORLD_RAY_SELECTION
					maps.Remove(miniWorldRay.uiInput);
#endif
					maps.Remove(miniWorldRay.directSelectInput);
#if ENABLE_MINIWORLD_RAY_SELECTION
					m_InputModule.RemoveRaycastSource(rayOrigin);
#endif
					m_MiniWorldRays.Remove(rayOrigin);
				}
			}
		}
	}

	private void UpdateMiniWorlds()
	{
		if (m_MiniWorlds.Count == 0)
			return;

		// Update ignore list
		var renderers = GetComponentsInChildren<Renderer>(true);
		var ignoreList = new List<Renderer>(renderers.Length);
		foreach (var renderer in renderers)
		{
			if (renderer.CompareTag(kVRPlayerTag))
				continue;
			if (renderer.CompareTag(kShowInMiniWorldTag))
				continue;
			ignoreList.Add(renderer);
		}

		var directSelection = m_TransformTool;
		foreach (var miniWorld in m_MiniWorlds)
		{
			miniWorld.ignoreList = ignoreList;
		}

		// Update MiniWorldRays
		foreach (var ray in m_MiniWorldRays)
		{
			var miniWorldRayOrigin = ray.Key;
			var miniWorldRay = ray.Value;

			if (!miniWorldRay.proxy.active)
			{
				miniWorldRay.tester.active = false;
				continue;
			}

			// Transform into reference space
			var miniWorld = miniWorldRay.miniWorld;
			var originalRayOrigin = miniWorldRay.originalRayOrigin;
			var referenceTransform = miniWorld.referenceTransform;
			miniWorldRayOrigin.position = referenceTransform.position + Vector3.Scale(miniWorld.miniWorldTransform.InverseTransformPoint(originalRayOrigin.position), miniWorld.referenceTransform.localScale);
			miniWorldRayOrigin.rotation = referenceTransform.rotation * Quaternion.Inverse(miniWorld.miniWorldTransform.rotation) * originalRayOrigin.rotation;
			miniWorldRayOrigin.localScale = Vector3.Scale(miniWorld.miniWorldTransform.localScale.Inverse(), referenceTransform.localScale);

			// Set miniWorldRayOrigin active state based on whether controller is inside corresponding MiniWorld
			var originalPointerPosition = originalRayOrigin.position + originalRayOrigin.forward * GetPointerLength(originalRayOrigin);
			var isContained = miniWorld.Contains(originalPointerPosition);
			miniWorldRay.tester.active = isContained;

			var directSelectInput = (DirectSelectInput)miniWorldRay.directSelectInput;

			if (directSelectInput.select.wasJustPressed)
			{
				var dragObject = GetDirectSelectionForRayOrigin(miniWorldRayOrigin, directSelectInput);

				if (dragObject)
				{
					// Only one ray can grab an object, otherwise PlaceObject is called on each trigger release
					// This does not prevent TransformTool from doing two-handed scaling
					var otherRayHasThisObject = false;
					foreach (var otherRay in m_MiniWorldRays.Values)
					{
						if (otherRay != miniWorldRay && otherRay.dragObject == dragObject)
							otherRayHasThisObject = true;
					}

					if (!otherRayHasThisObject)
					{
						miniWorldRay.dragObject = dragObject;
						miniWorldRay.dragObjectOriginalScale = dragObject.transform.localScale;
						var totalBounds = U.Object.GetTotalBounds(dragObject.transform);
						if (totalBounds != null)
							miniWorldRay.dragObjectPreviewScale = dragObject.transform.localScale * (kPreviewScale / totalBounds.Value.size.MaxComponent());
					}
				}
			}

			// Transfer objects to and from original ray and MiniWorld ray
			if (directSelection != null)
			{
				var pointerLengthDiff = GetPointerLength(miniWorldRayOrigin) - GetPointerLength(originalRayOrigin);

				// If the original ray was directly manipulating an object, we need to transfer ownership when it enters the MiniWorld
				var heldObject = directSelection.GetHeldObject(originalRayOrigin);
				if (heldObject && isContained && !miniWorldRay.wasContained)
					directSelection.TransferHeldObject(originalRayOrigin, directSelectInput, miniWorldRayOrigin, pointerLengthDiff * Vector3.forward);

				// In the case where we have transferred an object, transfer it back if it leaves the MiniWorld
				// This is a different case from when an object was first grabbed within the MiniWorld and becomes a preview, because miniWorldRay.dragObject is not set
				heldObject = directSelection.GetHeldObject(miniWorldRayOrigin);
				if (heldObject && !isContained && miniWorldRay.wasContained && !miniWorldRay.dragObject)
					directSelection.TransferHeldObject(miniWorldRayOrigin, miniWorldRay.originalDirectSelectInput, originalRayOrigin, pointerLengthDiff * Vector3.back);
			}

			// Transfer objects between MiniWorlds
			if (!miniWorldRay.dragObject)
			{
				if (isContained)
				{
					foreach (var kvp in m_MiniWorldRays)
					{
						var otherRayOrigin = kvp.Key;
						var otherRay = kvp.Value;
						var otherObject = otherRay.dragObject;
						if (otherRay != miniWorldRay && !otherRay.wasContained && otherObject)
						{
							miniWorldRay.dragObject = otherObject;
							miniWorldRay.dragObjectOriginalScale = otherRay.dragObjectOriginalScale;
							miniWorldRay.dragObjectPreviewScale = otherRay.dragObjectPreviewScale;
							miniWorldRay.directSelectInput.active = true;

							otherRay.dragObject = null;
							otherRay.directSelectInput.active = false;

							if (directSelection != null)
							{
								var heldObject = directSelection.GetHeldObject(otherRayOrigin);
								if (heldObject)
								{
									directSelection.TransferHeldObject(otherRayOrigin, miniWorldRay.directSelectInput, miniWorldRayOrigin,
										Vector3.zero); // Set the new offset to zero because the object will have moved (this could be improved by taking original offset into account)
								}
							}

							break;
						}
					}
				}
			}

			if (!miniWorldRay.dragObject)
			{
				miniWorldRay.wasContained = isContained;
				continue;
			}

			var dragObjectTransform = miniWorldRay.dragObject.transform;

			if (directSelectInput.select.isHeld)
			{
				if (isContained)
				{
					// Scale the object back to its original scale when it re-enters the MiniWorld
					if (!miniWorldRay.wasContained)
					{
						dragObjectTransform.localScale = miniWorldRay.dragObjectOriginalScale;

						// Add the object (back) to TransformTool
						if (directSelection != null)
						{
							if (miniWorldRay.wasHeld)
								U.Math.SetTransformOffset(miniWorldRayOrigin, dragObjectTransform, miniWorldRay.originalPositionOffset, miniWorldRay.originalRotationOffset);
							else
								U.Math.SetTransformOffset(miniWorldRayOrigin, dragObjectTransform, GetPointerLength(miniWorldRayOrigin) * Vector3.forward, Quaternion.identity);

							directSelection.AddHeldObject(miniWorldRay.node, miniWorldRayOrigin, dragObjectTransform, directSelectInput);
						}
					}
				}
				else
				{
					if (dragObjectTransform.tag == kVRPlayerTag)
					{
						if (directSelection != null)
							directSelection.DropHeldObject(dragObjectTransform.transform);

						// Drop player at edge of MiniWorld
						miniWorldRay.dragObject = null;
					}
					else
					{
						if (miniWorldRay.wasContained)
						{
							var otherwiseContained = false;
							foreach (var world in m_MiniWorlds)
							{
								if (world.Contains(originalPointerPosition))
								{
									otherwiseContained = true;
								}
							}

							if (!otherwiseContained)
							{
								// Store the original scale in case the object re-enters the MiniWorld
								miniWorldRay.dragObjectOriginalScale = dragObjectTransform.localScale;

								// Drop from TransformTool to take control of object
								if (directSelection != null)
								{
									directSelection.DropHeldObject(dragObjectTransform, out miniWorldRay.originalPositionOffset, out miniWorldRay.originalRotationOffset);
									miniWorldRay.wasHeld = true;
								}

								dragObjectTransform.localScale = miniWorldRay.dragObjectPreviewScale;
							}
						}

						m_ObjectPlacementModule.Preview(dragObjectTransform, GetPreviewOriginForRayOrigin(originalRayOrigin));
					}
				}
			}

			// Release the current object if the trigger is no longer held
			if (directSelectInput.select.wasJustReleased)
			{
				if (directSelection != null)
					directSelection.DropHeldObject(dragObjectTransform);

				// If the user has pulled an object out of the MiniWorld, use PlaceObject to grow it back to its original scale
				if (!isContained)
					PlaceObject(dragObjectTransform, miniWorldRay.dragObjectOriginalScale);

				miniWorldRay.dragObject = null;
				miniWorldRay.wasHeld = false;
			}

			miniWorldRay.wasContained = isContained;
		}
	}

	private GameObject GetFirstGameObject(Transform rayOrigin)
	{
		var go = m_PixelRaycastModule.GetFirstGameObject(rayOrigin);
		if (go)
			return go;

		// If a raycast did not find an object use the spatial hash as a final test
		if (m_IntersectionModule)
		{
			var tester = rayOrigin.GetComponentInChildren<IntersectionTester>();
			var renderer = m_IntersectionModule.GetIntersectedObjectForTester(tester);
			if (renderer)
				return renderer.gameObject;
		}

		foreach (var ray in m_MiniWorldRays)
		{
			var miniWorldRay = ray.Value;
			if (miniWorldRay.originalRayOrigin.Equals(rayOrigin))
			{
				var tester = miniWorldRay.tester;
				if (!tester.active)
					continue;

#if ENABLE_MINIWORLD_RAY_SELECTION
				var miniWorldRayOrigin = ray.Key;
				go = m_PixelRaycastModule.GetFirstGameObject(miniWorldRayOrigin);
				if (go)
					return go;
#endif

				var renderer = m_IntersectionModule.GetIntersectedObjectForTester(tester);
				if (renderer)
					return renderer.gameObject;
			}
		}

				return null;
	}

	Dictionary<Transform, DirectSelection> GetDirectSelection()
	{
		var results = new Dictionary<Transform, DirectSelection>();

		ForEachRayOrigin((proxy, rayOriginPair, device, deviceData) =>
		{
			var rayOrigin = rayOriginPair.Value;
			var obj = GetDirectSelectionForRayOrigin(rayOrigin, deviceData.directSelectInput);
			if (obj)
			{
				results[rayOrigin] = new DirectSelection
				{
					gameObject = obj,
					node = rayOriginPair.Key,
					input = deviceData.directSelectInput
				};
			}
		}, true);

		foreach (var ray in m_MiniWorldRays)
		{
			var rayOrigin = ray.Key;
			var miniWorldRay = ray.Value;
			var go = GetDirectSelectionForRayOrigin(rayOrigin, miniWorldRay.directSelectInput);
			if (go != null)
			{
				results[rayOrigin] = new DirectSelection
				{
					gameObject = go,
					node = ray.Value.node,
					input = miniWorldRay.directSelectInput
				};
			}
		}
		return results;
	}

	GameObject GetDirectSelectionForRayOrigin(Transform rayOrigin, ActionMapInput input)
	{
		var directSelection = m_TransformTool;

		if (m_IntersectionModule)
		{
			var tester = rayOrigin.GetComponentInChildren<IntersectionTester>();

			var renderer = m_IntersectionModule.GetIntersectedObjectForTester(tester);
			if (renderer)
			{
				input.active = true;
				return renderer.gameObject;
			}
		}

		MiniWorldRay ray;
		input.active = (directSelection != null && directSelection.GetHeldObject(rayOrigin))
			|| (m_MiniWorldRays.TryGetValue(rayOrigin, out ray) && ray.dragObject);

		return null;
	}

	bool CanGrabObject(DirectSelection selection, Transform rayOrigin)
	{
		if (selection.gameObject.tag == kVRPlayerTag && !m_MiniWorldRays.ContainsKey(rayOrigin))
			return false;

		return true;
	}

	bool GrabObject(IGrabObjects grabber, DirectSelection selection, Transform rayOrigin)
	{
		if (!CanGrabObject(selection, rayOrigin))
			return false;

		// Detach the player head model so that it is not affected by its parent transform
		if (selection.gameObject.tag == kVRPlayerTag)
			selection.gameObject.transform.parent = null;

		return true;
	}

	void DropObject(IGrabObjects grabber, Transform grabbedObject, Transform rayOrigin)
	{
		// Dropping the player head updates the viewer pivot
		if (grabbedObject.tag == kVRPlayerTag)
			StartCoroutine(UpdateViewerPivot(grabbedObject));
	}

	IEnumerator UpdateViewerPivot(Transform playerHead)
	{
		var viewerPivot = U.Camera.GetViewerPivot();

		// Smooth motion will cause Workspaces to lag behind camera
		var components = viewerPivot.GetComponentsInChildren<SmoothMotion>();
		foreach (var smoothMotion in components)
		{
			smoothMotion.enabled = false;
		}

		// Hide player head to avoid jarring impact
		var playerHeadRenderers = playerHead.GetComponentsInChildren<Renderer>();
		foreach (var renderer in playerHeadRenderers)
		{
			renderer.enabled = false;
		}

		var mainCamera = U.Camera.GetMainCamera().transform;
		var startPosition = viewerPivot.position;
		var startRotation = viewerPivot.rotation;

		var rotationDiff = U.Math.ConstrainYawRotation(Quaternion.Inverse(mainCamera.rotation) * playerHead.rotation);
		var cameraDiff = viewerPivot.position - mainCamera.position;
		cameraDiff.y = 0;
		var rotationOffset = rotationDiff * cameraDiff - cameraDiff;

		var endPosition = viewerPivot.position + (playerHead.position - mainCamera.position) + rotationOffset;
		var endRotation = viewerPivot.rotation * rotationDiff;
		var startTime = Time.realtimeSinceStartup;
		var diffTime = 0f;

		while (diffTime < kViewerPivotTransitionTime)
		{
			diffTime = Time.realtimeSinceStartup - startTime;
			var t = diffTime / kViewerPivotTransitionTime;
			// Use a Lerp instead of SmoothDamp for constant velocity (avoid motion sickness)
			viewerPivot.position = Vector3.Lerp(startPosition, endPosition, t);
			viewerPivot.rotation = Quaternion.Lerp(startRotation, endRotation, t);
			yield return null;
		}

		viewerPivot.position = endPosition;
		viewerPivot.rotation = endRotation;

		playerHead.parent = mainCamera;
		playerHead.localRotation = Quaternion.identity;
		playerHead.localPosition = Vector3.zero;

		foreach (var smoothMotion in components)
		{
			smoothMotion.enabled = true;
		}

		foreach (var renderer in playerHeadRenderers)
		{
			renderer.enabled = true;
		}
	}

	private void PlaceObject(Transform obj, Vector3 targetScale)
	{
		foreach (var miniWorld in m_MiniWorlds)
		{
			if (!miniWorld.Contains(obj.position))
				continue;
			
			var referenceTransform = miniWorld.referenceTransform;
			obj.transform.parent = null;
			obj.position = referenceTransform.position + Vector3.Scale(miniWorld.miniWorldTransform.InverseTransformPoint(obj.position), miniWorld.referenceTransform.localScale);
			obj.rotation = referenceTransform.rotation * Quaternion.Inverse(miniWorld.miniWorldTransform.rotation) * obj.rotation;
			obj.localScale = Vector3.Scale(Vector3.Scale(obj.localScale, referenceTransform.localScale), miniWorld.miniWorldTransform.lossyScale);
			return;
		}

		m_ObjectPlacementModule.PlaceObject(obj, targetScale);
	}

	private Transform GetPreviewOriginForRayOrigin(Transform rayOrigin)
	{
		return (from proxy in m_AllProxies
				from origin in proxy.rayOrigins
					where origin.Value.Equals(rayOrigin)
						select proxy.previewOrigins[origin.Key]).FirstOrDefault();
	}

	bool IsMiniWorldRay(Transform rayOrigin)
	{
		return m_MiniWorldRays.ContainsKey(rayOrigin);
	}

	void AddPlayerModel()
	{
		var playerHead = U.Object.Instantiate(m_PlayerModelPrefab, U.Camera.GetMainCamera().transform, false).GetComponent<Renderer>();
		m_SpatialHashModule.spatialHash.AddObject(playerHead, playerHead.bounds);
	}

	void AddObjectToSpatialHash(UnityObject obj)
	{
		if (m_SpatialHashModule)
			m_SpatialHashModule.AddObject(obj);
		else
			Debug.LogError("Tried to add " + obj + " to spatial hash but it doesn't exist yet");
	}

	void RemoveObjectFromSpatialHash(UnityObject obj)
	{
		if (m_SpatialHashModule)
			m_SpatialHashModule.RemoveObject(obj);
		else
			Debug.LogError("Tried to remove " + obj + " from spatial hash but it doesn't exist yet");
	}

	bool PreProcessRaycastSources()
	{
		if (!m_Manipulator)
			m_Manipulator = GetComponentInChildren<StandardManipulator>();

		if (m_Manipulator)
			m_OriginalManipulatorScale = m_Manipulator.transform.localScale;

		return true;
	}

	bool PreProcessRaycastSource(Transform rayOrigin)
	{
		if (m_Manipulator)
		{
			MiniWorldRay ray;
			if (m_MiniWorldRays.TryGetValue(rayOrigin, out ray))
				m_Manipulator.transform.localScale = ray.miniWorld.miniWorldScale * kMiniWorldManipulatorScale;
			else
				m_Manipulator.transform.localScale = m_OriginalManipulatorScale;
		}
		return true;
	}

	void PostProcessRaycastSources()
	{
		if (m_Manipulator)
			m_Manipulator.transform.localScale = m_OriginalManipulatorScale;
	}

	bool PreProcessMiniWorldRender(IMiniWorld miniWorld)
	{
		if (m_Manipulator)
			m_Manipulator.transform.localScale = miniWorld.miniWorldScale * kMiniWorldManipulatorScale;
		return true;
	}

	void PostProcessMiniWorldRender(IMiniWorld miniWorld)
	{
		if (m_Manipulator)
			m_Manipulator.transform.localScale = m_OriginalManipulatorScale;
	}

	List<string> GetFilterList()
	{
		return m_AssetTypes.ToList();
	}

	FolderData[] GetFolderData()
	{
		if (m_FolderData == null)
			return null;

		var assetsFolder = new FolderData(m_FolderData[0]) { expanded = true };

		return new[] { assetsFolder };
	}

	IEnumerator LoadProjectFolders(bool quickStart = true)
	{
		FolderDataIteration iteration = null;

		if (quickStart)
		{
			//Start with a quick pass to get assets without types
			foreach (var e in CreateFolderData())
			{
				iteration = e;
			}

			m_FolderData = new[] { iteration.data };

			yield return null;
		}

		yield break;

		m_AssetTypes.Clear();

		//Create a new list with actual types
		foreach (var e in CreateFolderData(m_AssetTypes))
		{
			iteration = e;
			yield return null;
		}

		m_FolderData = new[] { iteration.data };

		// Send new data to existing folderLists
		foreach (var list in m_ProjectFolderLists)
		{
			list.folderData = GetFolderData();
		}

		// Send new data to existing filterUIs
		foreach (var filterUI in m_FilterUIs)
		{
			filterUI.filterList = GetFilterList();
		}
	}

	// Call with no assetTypes for quick load (and no types)
	IEnumerable<FolderDataIteration> CreateFolderData(HashSet<string> assetTypes = null, bool hasNext = true, HierarchyProperty hp = null)
	{
		if (hp == null)
		{
			hp = new HierarchyProperty(HierarchyType.Assets);
			hp.SetSearchFilter("t:object", 0);
		}
		var name = hp.name;
		var instanceID = hp.instanceID;
		var depth = hp.depth;
		var folderList = new List<FolderData>();
		var assetList = new List<AssetData>();
		if (hasNext)
		{
			hasNext = hp.Next(null);
			while (hasNext && hp.depth > depth)
			{
				if (hp.isFolder)
				{
					FolderDataIteration folder = null;

					foreach (var e in CreateFolderData(assetTypes, hasNext, hp))
					{
						folder = e;
						if (assetTypes != null)
							yield return e;
					}

					folderList.Add(folder.data);
					hasNext = folder.hasNext;
				}
				else if (hp.isMainRepresentation) // Ignore sub-assets (mixer children, terrain splats, etc.)
					assetList.Add(CreateAssetData(hp, assetTypes));

				if (hasNext)
					hasNext = hp.Next(null);

				if (assetTypes != null && Time.realtimeSinceStartup - m_LastFolderYieldTime > kMaxFrameTime)
				{
					m_LastFolderYieldTime = Time.realtimeSinceStartup;
					yield return null;
				}
			}

			if (hasNext)
				hp.Previous(null);
		}

		yield return new FolderDataIteration
		{
			data = new FolderData(name, folderList.Count > 0 ? folderList.ToArray() : null, assetList.ToArray(), instanceID),
			hasNext = hasNext
		};
	}

	AssetData CreateAssetData(HierarchyProperty hp, HashSet<string> assetTypes = null)
	{
		var type = "";
		if (assetTypes != null)
		{
			type = hp.pptrValue.GetType().Name;
			switch (type)
			{
				case "GameObject":
					switch (PrefabUtility.GetPrefabType(EditorUtility.InstanceIDToObject(hp.instanceID)))
					{
						case PrefabType.ModelPrefab:
							type = "Model";
							break;
						default:
							type = "Prefab";
							break;
					}
					break;
				case "MonoScript":
					type = "Script";
					break;
				case "SceneAsset":
					type = "Scene";
					break;
				case "AudioMixerController":
					type = "AudioMixer";
					break;
			}

			assetTypes.Add(type);
		}

		return new AssetData(hp.name, hp.instanceID, hp.icon, type);
	}

#if UNITY_EDITOR
	private static EditorVR s_Instance;
	private static InputManager s_InputManager;

	[MenuItem("Window/EditorVR %e", false)]
	public static void ShowEditorVR()
	{
		// Using a utility window improves performance by saving from the overhead of DockArea.OnGUI()
		VRView.GetWindow<VRView>(true, "EditorVR", true);
	}

	[MenuItem("Window/EditorVR", true)]
	public static bool ShouldShowEditorVR()
	{
		return PlayerSettings.virtualRealitySupported;
	}

	static EditorVR()
	{
		VRView.onEnable += OnEVREnabled;
		VRView.onDisable += OnEVRDisabled;
	}

	private static void OnEVREnabled()
	{
		InitializeInputManager();
		s_Instance = U.Object.CreateGameObjectWithComponent<EditorVR>();
		EditorApplication.projectWindowChanged += OnProjectWindowChanged;
	}

	private static void InitializeInputManager()
	{
		// HACK: InputSystem has a static constructor that is relied upon for initializing a bunch of other components, so
		// in edit mode we need to handle lifecycle explicitly
		InputManager[] managers = Resources.FindObjectsOfTypeAll<InputManager>();
		foreach (var m in managers)
		{
			U.Object.Destroy(m.gameObject);
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
		s_InputManager.gameObject.hideFlags = kDefaultHideFlags;
		U.Object.SetRunInEditModeRecursively(s_InputManager.gameObject, true);

		U.Object.Destroy(s_InputManager.GetComponent<JoystickInputToEvents>());
		U.Object.Destroy(s_InputManager.GetComponent<KeyboardInputToEvents>());
	}

	static void OnProjectWindowChanged()
	{
		s_Instance.StartCoroutine(s_Instance.LoadProjectFolders(false));
	}

	private static void OnEVRDisabled()
	{
		EditorApplication.projectWindowChanged -= OnProjectWindowChanged;
		U.Object.Destroy(s_Instance.gameObject);
		U.Object.Destroy(s_InputManager.gameObject);
	}
#endif
}
