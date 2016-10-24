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
using UnityEngine.VR.Menus;
using UnityEngine.VR.Modules;
using UnityEngine.VR.Proxies;
using UnityEngine.VR.Tools;
using UnityEngine.VR.UI;
using UnityEngine.VR.Utilities;
using UnityEngine.VR.Workspaces;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.VR;
#endif

[InitializeOnLoad]
public class EditorVR : MonoBehaviour
{
	public const HideFlags kDefaultHideFlags = HideFlags.DontSave;

	private const float kDefaultRayLength = 100f;

	private const float kWorkspaceAnglePadding = 25f;
	private const float kWorkspaceYPadding = 0.35f;
	private const int kMaxWorkspacePlacementAttempts = 20;
	private const float kWorkspaceVacuumEnableDistance = 1f; // Disable vacuum bounds if workspace is close to player

	[SerializeField]
	private ActionMap m_ShowMenuActionMap;
	[SerializeField]
	private ActionMap m_TrackedObjectActionMap;
	[SerializeField]
	private ActionMap m_StandardToolActionMap;
	[SerializeField]
	private DefaultProxyRay m_ProxyRayPrefab;
	[SerializeField]
	private Camera m_EventCameraPrefab;

	[SerializeField]
	private KeyboardMallet m_KeyboardMalletPrefab;

	[SerializeField]
	private KeyboardUI m_NumericKeyboardPrefab;

	[SerializeField]
	private KeyboardUI m_StandardKeyboardPrefab;

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
	private DragAndDropModule m_DragAndDropModule;

	private bool m_UpdatePixelRaycastModule = true;

	private PlayerHandle m_PlayerHandle;

	private class DeviceData
	{
		public Stack<ITool> tools;
		public ShowMenu showMenuInput;
		public ActionMapInput uiInput;
		public IMainMenu mainMenu;
		public ITool currentTool;
	}

	private readonly Dictionary<InputDevice, DeviceData> m_DeviceData = new Dictionary<InputDevice, DeviceData>();
	private readonly List<IProxy> m_AllProxies = new List<IProxy>();
	private List<Type> m_AllTools;
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
		public IMiniWorld miniWorld;
		public IProxy proxy;
		public ActionMapInput uiInput;
		public GameObject hoverObject;
		public GameObject dragObject;
		public Vector3 dragObjectOriginalScale;
		public Vector3 dragObjectPositionOffset;
		public Quaternion dragObjectRotationOffset;
	}

	private readonly Dictionary<Transform, MiniWorldRay> m_MiniWorldRays = new Dictionary<Transform, MiniWorldRay>();
	private readonly List<IMiniWorld> m_MiniWorlds = new List<IMiniWorld>();

	private ITransformTool m_TransformTool;

	private event Action m_SelectionChanged;

	bool m_HMDReady;

	private void Awake()
	{
		ClearDeveloperConsoleIfNecessary();

		VRView.viewerPivot.parent = transform; // Parent the camera pivot under EditorVR
		if (VRSettings.loadedDeviceName == "OpenVR")
		{
			// Steam's reference position should be at the feet and not at the head as we do with Oculus
			VRView.viewerPivot.localPosition = Vector3.zero;
		}
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
		m_SnappingModule = U.Object.AddComponent<SnappingModule>(gameObject);

		m_AllTools = U.Object.GetImplementationsOfInterface(typeof(ITool)).ToList();
		m_MainMenuTools = m_AllTools.Where(t => !IsPermanentTool(t)).ToList(); // Don't show tools that can't be selected/toggled
		m_AllWorkspaceTypes = U.Object.GetExtensionsOfClass(typeof(Workspace)).ToList();

		m_MainMenuModules = GetComponents<IModule>().ToList();
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
			m_SelectionChanged.Invoke();
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
				showMenuInput = (ShowMenu)CreateActionMapInput(m_ShowMenuActionMap, device)
			};
			m_DeviceData.Add(device, deviceData);
		}
	}

	private IEnumerator Start()
	{
		while (!m_HMDReady)
			yield return null;
		CreateDefaultWorkspaces();

		// In case we have anything selected at start, set up manipulators, inspector, etc.
		EditorApplication.delayCall += OnSelectionChanged;

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
		SpawnDefaultTools();
		StartCoroutine(PrewarmAssets());

		// Call OnSelectionChanged one more time for tools
		EditorApplication.delayCall += OnSelectionChanged;

		// This will be the first call to update the player handle (input) maps, sorted by priority
		UpdatePlayerHandleMaps();
	}

	private void OnEnable()
	{
		Selection.selectionChanged += OnSelectionChanged;
#if UNITY_EDITOR
		VRView.onGUIDelegate += OnSceneGUI;
		VRView.onHMDReady += OnHMDReady;
#endif
	}

	private void OnDisable()
	{
		Selection.selectionChanged -= OnSelectionChanged;
#if UNITY_EDITOR
		VRView.onGUIDelegate -= OnSceneGUI;
		VRView.onHMDReady -= OnHMDReady;
#endif
	}

	void OnHMDReady()
	{
		m_HMDReady = true;
	}

	private void OnSceneGUI(EditorWindow obj)
	{
		if (Event.current.type == EventType.ExecuteCommand)
		{
			var miniWorldRayHasObject = false;

			m_PixelRaycastModule.UpdateIgnoreList();

			foreach (var proxy in m_AllProxies)
			{
				if (!proxy.active)
					continue;

				foreach (var rayOrigin in proxy.rayOrigins.Values)
					m_PixelRaycastModule.UpdateRaycast(rayOrigin, m_EventCamera);

				foreach (var miniWorldRay in m_MiniWorldRays)
				{
					miniWorldRay.Value.hoverObject = m_PixelRaycastModule.UpdateRaycast(miniWorldRay.Key, m_EventCamera, GetPointerLength(miniWorldRay.Key));

					if (miniWorldRay.Value.hoverObject || miniWorldRay.Value.dragObject)
						miniWorldRayHasObject = true;
				}
			}

			// If any active miniWorldRay hovers over a selected object, switch to the DirectManipulator
			if (m_TransformTool != null)
				m_TransformTool.mode = miniWorldRayHasObject ? TransformMode.Direct : TransformMode.Standard;

			UpdateDefaultProxyRays();

			// Queue up the next round
			m_UpdatePixelRaycastModule = true;
		}
	}

	private void OnDestroy()
	{
		PlayerHandleManager.RemovePlayerHandle(m_PlayerHandle);
	}

	IEnumerator PrewarmAssets()
	{
		// HACK: Cannot async load assets in the editor yet, so to avoid a hitch let's spawn the menu immediately and then make it invisible
		List<IMainMenu> menus = new List<IMainMenu>();
		foreach (var kvp in m_DeviceData)
		{
			var device = kvp.Key;
			var deviceData = m_DeviceData[device];
			var mainMenu = deviceData.mainMenu;

			if (mainMenu == null)
			{
				// HACK to workaround missing MonoScript serialized fields
				EditorApplication.delayCall += () =>
				{
					mainMenu = SpawnMainMenu(typeof(MainMenu), device, true);
					deviceData.mainMenu = mainMenu;
					UpdatePlayerHandleMaps();
				};

				while (mainMenu == null)
					yield return null;

				menus.Add(mainMenu);
			}
		}

		foreach (var mainMenu in menus)
		{
			while (!mainMenu.visible)
				yield return null;

			mainMenu.visible = false;
		}
	}

	private void Update()
	{
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
			if (kvp.Value.showMenuInput.show.wasJustPressed)
			{
				var device = kvp.Key;
				var mainMenu = m_DeviceData[device].mainMenu;

				if (mainMenu != null)
				{
					// Toggle menu
					mainMenu.visible = !mainMenu.visible;
				}
			}
		}

		var camera = U.Camera.GetMainCamera();
		// Enable/disable workspace vacuum bounds based on distance to camera
		foreach (var workspace in m_AllWorkspaces)
			workspace.vacuumEnabled = (workspace.transform.position - camera.transform.position).magnitude > kWorkspaceVacuumEnableDistance;

		UpdateMiniWorldRays();

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
		// HACK: U.AddComponent doesn't work properly from an IEnumerator (missing default references when spawned), so currently
		// it's necessary to spawn the tools in a separate non-IEnumerator context.
		EditorApplication.delayCall += () =>
		{
			// Spawn default tools
			HashSet<InputDevice> devices;
			ITool tool;

			var locomotionTool = typeof(BlinkLocomotionTool);
			if (VRSettings.loadedDeviceName == "Oculus")
				locomotionTool = typeof(JoystickLocomotionTool);

			foreach (var deviceData in m_DeviceData)
			{
				// Skip keyboard, mouse, gamepads. Selection tool should only be on left and right hands (tagged 0 and 1)
				if (deviceData.Key.tagIndex == -1)
					continue;

				tool = SpawnTool(typeof(SelectionTool), out devices, deviceData.Key);
				AddToolToDeviceData(tool, devices);

				if (locomotionTool == typeof(BlinkLocomotionTool))
				{
					tool = SpawnTool(locomotionTool, out devices, deviceData.Key);
					AddToolToDeviceData(tool, devices);
				}
			}

			if (locomotionTool == typeof(JoystickLocomotionTool))
			{
				tool = SpawnTool(locomotionTool, out devices);
				AddToolToDeviceData(tool, devices);
			}

			tool = SpawnTool(typeof(TransformTool), out devices);
			m_TransformTool = tool as ITransformTool;
			AddToolToDeviceData(tool, devices);
		};
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
				mallet.Hide();
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

		ForEachRayOrigin((proxy, rayOriginPair, device, deviceData) =>
		{
			// Create ui action map input for device.
			if (deviceData.uiInput == null)
				deviceData.uiInput = CreateActionMapInput(m_InputModule.actionMap, device);

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

	private void CreateSpatialSystem()
	{
		// Create event system, input module, and event camera
		m_SpatialHashModule = U.Object.AddComponent<SpatialHashModule>(gameObject);
		m_SpatialHashModule.Setup();
		m_IntersectionModule = U.Object.AddComponent<IntersectionModule>(gameObject);
		m_IntersectionModule.Setup(m_SpatialHashModule.spatialHash);

		ForEachRayOrigin((proxy, rayOriginPair, device, deviceData) =>
		{
			var tester = rayOriginPair.Value.GetComponentInChildren<IntersectionTester>();
			tester.active = proxy.active;
			m_IntersectionModule.AddTester(tester);
		});
	}

	private GameObject InstantiateUI(GameObject prefab)
	{
		var go = U.Object.Instantiate(prefab, transform);
		foreach (Canvas canvas in go.GetComponentsInChildren<Canvas>())
			canvas.worldCamera = m_EventCamera;

		foreach (InputField inputField in go.GetComponentsInChildren<InputField>())
		{
			if (inputField is NumericInputField)
				inputField.spawnKeyboard = SpawnNumericKeyboard;
			else if (inputField is StandardInputField)
				inputField.spawnKeyboard = SpawnAlphaNumericKeyboard;
		}

		return go;
	}

	private KeyboardUI SpawnNumericKeyboard()
	{
		if(m_StandardKeyboard != null)
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

		var devices = device == null ? m_PlayerHandle.GetApplicableDevices() : new InputDevice[] { device };

		var actionMapInput = ActionMapInput.Create(map);
		// It's possible that there are no suitable control schemes for the device that is being initialized, 
		// so ActionMapInput can't be marked active
		if (actionMapInput.TryInitializeWithDevices(devices))
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
			maps.Add(deviceData.showMenuInput);

			if (deviceData.mainMenu != null)
				AddActionMapInputs(deviceData.mainMenu, maps);

			// Not every tool has UI
			if (deviceData.uiInput != null)
				maps.Add(deviceData.uiInput);
		}

		maps.AddRange(m_MiniWorldRays.Select(miniWorldRay => miniWorldRay.Value.uiInput));

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

		var customMap = obj as ICustomActionMap;
		if (customMap != null)
		{
			customMap.actionMapInput = CreateActionMapInput(customMap.actionMap, device);
			if (actionMapInputs != null)
				actionMapInputs.Add(customMap.actionMapInput);
		}
	}

	private void ConnectInterfaces(object obj, InputDevice device = null)
	{
		var mainMenu = obj as IMainMenu;

		if (device != null)
		{
			var ray = obj as IRay;
			if (ray != null)
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
						if (proxy.rayOrigins.TryGetValue(node.Value, out rayOrigin))
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

						if (mainMenu != null)
						{
							Transform mainMenuOrigin;
							if (proxy.menuOrigins.TryGetValue(node.Value, out mainMenuOrigin))
							{
								mainMenu.menuOrigin = mainMenuOrigin;
								Transform alternateMenuOrigin;
								if (proxy.alternateMenuOrigins.TryGetValue(node.Value, out alternateMenuOrigin))
									mainMenu.alternateMenuOrigin = alternateMenuOrigin;
							}
						}

						if (!continueSearching)
							break;
					}
				}
			}
		}

		var locomotion = obj as ILocomotion;
		if (locomotion != null)
			locomotion.viewerPivot = VRView.viewerPivot;

		var instantiateUI = obj as IInstantiateUI;
		if (instantiateUI != null)
			instantiateUI.instantiateUI = InstantiateUI;

		var raycaster = obj as IRaycaster;
		if (raycaster != null)
			raycaster.getFirstGameObject = GetFirstGameObject;

		var highlight = obj as IHighlight;
		if (highlight != null)
			highlight.setHighlight = m_HighlightModule.SetHighlight;

		var placeObjects = obj as IPlaceObjects;
		if (placeObjects != null)
			placeObjects.placeObject = PlaceObject;

		var positionPreview = obj as IPreview;
		if (positionPreview != null)
		{
			positionPreview.preview = m_ObjectPlacementModule.Preview;
			positionPreview.getPreviewOriginForRayOrigin = GetPreviewOriginForRayOrigin;
		}

		var selectionChanged = obj as ISelectionChanged;
		if (selectionChanged != null)
			m_SelectionChanged += selectionChanged.OnSelectionChanged;

		var snapping = obj as ISnapping;
		if (snapping != null)
		{
			snapping.onSnapEnded = m_SnappingModule.OnSnapEnded;
			snapping.onSnapHeld = m_SnappingModule.OnSnapHeld;
			snapping.onSnapStarted = m_SnappingModule.OnSnapStarted;
			snapping.onSnapUpdate = m_SnappingModule.OnSnapUpdate;
		}

		if (mainMenu != null)
		{
			mainMenu.menuTools = m_MainMenuTools;
			mainMenu.menuModules = m_MainMenuModules;
			mainMenu.selectTool = SelectTool;
			mainMenu.menuWorkspaces = m_AllWorkspaceTypes.ToList();
			mainMenu.createWorkspace = CreateWorkspace;
			mainMenu.node = GetDeviceNode(device);
			mainMenu.setup();
		}
	}

	private void DisconnectInterfaces(object obj)
	{
		var selectionChanged = obj as ISelectionChanged;
		if (selectionChanged != null)
			m_SelectionChanged -= selectionChanged.OnSelectionChanged;
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
			if (ray != null) {
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

		// HACK to workaround missing serialized fields coming from the MonoScript
		EditorApplication.delayCall += () =>
		{
			var spawnTool = true;
			DeviceData deviceData;
			if (m_DeviceData.TryGetValue(deviceToAssignTool, out deviceData))
			{
				// If this tool was on the current device already, then simply toggle it off
				if (deviceData.currentTool != null && deviceData.currentTool.GetType() == toolType)
				{
					DespawnTool(deviceData, deviceData.currentTool);

					// Don't spawn a new tool, since we are toggling the old tool
					spawnTool = false;
				}
			}

			if (spawnTool)
			{
				// Spawn tool and collect all devices that this tool will need
				HashSet<InputDevice> usedDevices;
				var newTool = SpawnTool(toolType, out usedDevices, deviceToAssignTool);

				foreach (var dev in usedDevices)
				{
					deviceData = m_DeviceData[dev];
					if (deviceData.currentTool != null) // Remove the current tool on all devices this tool will be spawned on
						DespawnTool(deviceData, deviceData.currentTool);

					AddToolToStack(dev, newTool);
				}
			}

			UpdatePlayerHandleMaps();
		};

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
			}
			DisconnectInterfaces(tool);
			U.Object.Destroy(tool as MonoBehaviour);
		}
	}

	private bool IsValidActionMapForDevice(ActionMap actionMap, InputDevice device)
	{
		var untaggedDevicesFound = 0;
		var taggedDevicesFound = 0;
		var nonMatchingTagIndices = 0;
		var matchingTagIndices = 0;

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
			m_DeviceData[device].tools.Push(tool);
			m_DeviceData[device].currentTool = tool;
		}
	}

	private void CreateDefaultWorkspaces()
	{
		CreateWorkspace<ProjectWorkspace>();
	}
	
	private void CreateWorkspace<T>() where T : Workspace
	{
		CreateWorkspace(typeof(T));
	}

	private void CreateWorkspace(Type t)
	{
		var defaultOffset = Workspace.kDefaultOffset;
		var defaultTilt = Workspace.kDefaultTilt;

		var cameraTransform = U.Camera.GetMainCamera().transform;
		var headPosition = cameraTransform.position;
		var headRotation = Quaternion.Euler(0, cameraTransform.rotation.eulerAngles.y, 0);

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
		// HACK to workaround missing MonoScript serialized fields
		EditorApplication.delayCall += () =>
		{
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
			while (Physics.CheckBox(position, halfBounds, rotation) && count++ < kMaxWorkspacePlacementAttempts) ;

			Workspace workspace = (Workspace)U.Object.CreateGameObjectWithComponent(t, viewerPivot);
			m_AllWorkspaces.Add(workspace);
			workspace.destroyed += OnWorkspaceDestroyed;
			ConnectInterfaces(workspace);
			workspace.transform.position = position;
			workspace.transform.rotation = rotation;

			//Explicit setup call (instead of setting up in Awake) because we need interfaces to be hooked up first
			workspace.Setup();

			var miniWorld = workspace as IMiniWorld;
			if (miniWorld == null)
				return;

			m_MiniWorlds.Add(miniWorld);

			ForEachRayOrigin((proxy, rayOriginPair, device, deviceData) =>
			{
				// Create MiniWorld rayOrigin
				var miniWorldRayOrigin = new GameObject("MiniWorldRayOrigin").transform;
				miniWorldRayOrigin.parent = workspace.transform;

				var uiInput = CreateActionMapInput(m_InputModule.actionMap, device);
				m_PlayerHandle.maps.Insert(m_PlayerHandle.maps.IndexOf(deviceData.uiInput), uiInput);
				// Add RayOrigin transform, proxy and ActionMapInput references to input module list of sources
				m_InputModule.AddRaycastSource(proxy, rayOriginPair.Key, uiInput, miniWorldRayOrigin);
				m_MiniWorldRays[miniWorldRayOrigin] = new MiniWorldRay()
				{
					originalRayOrigin = rayOriginPair.Value,
					miniWorld = miniWorld,
					proxy = proxy,
					uiInput = uiInput
				};
			}, true);
		};
	}

	private void OnWorkspaceDestroyed(Workspace workspace)
	{
		m_AllWorkspaces.Remove(workspace);

		DisconnectInterfaces(workspace);

		var miniWorld = workspace as IMiniWorld;
		if (miniWorld == null)
			return;

		//Clean up MiniWorldRays
		m_MiniWorlds.Remove(miniWorld);
		var miniWorldRaysCopy = new Dictionary<Transform, MiniWorldRay>(m_MiniWorldRays);
		foreach (var ray in miniWorldRaysCopy.Where(ray => ray.Value.miniWorld.Equals(miniWorld)))
		{
			m_PlayerHandle.maps.Remove(ray.Value.uiInput);
			m_InputModule.RemoveRaycastSource(ray.Key);
			m_MiniWorldRays.Remove(ray.Key);
		}
	}

	private void UpdateMiniWorldRays()
	{
		foreach (var ray in m_MiniWorldRays)
		{
			var miniWorldRayOrigin = ray.Key;
			if (!ray.Value.proxy.active)
			{
				miniWorldRayOrigin.gameObject.SetActive(false);
				continue;
			}

			// Transform into reference space
			var miniWorld = ray.Value.miniWorld;
			var originalRayOrigin = ray.Value.originalRayOrigin;
			var referenceTransform = miniWorld.referenceTransform;
			miniWorldRayOrigin.position = referenceTransform.position + Vector3.Scale(miniWorld.miniWorldTransform.InverseTransformPoint(originalRayOrigin.position), miniWorld.referenceTransform.localScale);
			miniWorldRayOrigin.rotation = referenceTransform.rotation * Quaternion.Inverse(miniWorld.miniWorldTransform.rotation) * originalRayOrigin.rotation;

			// Set miniWorldRayOrigin active state based on whether controller is inside corresponding MiniWorld
			var pointerLength = GetPointerLength(originalRayOrigin);
			var isContained = miniWorld.Contains(originalRayOrigin.position + originalRayOrigin.forward * pointerLength);
			miniWorldRayOrigin.gameObject.SetActive(isContained);

			// Keep input alive if we are dragging an object, otherwise MultipleRayInputModule will reset our control state
			ray.Value.uiInput.active = isContained || ray.Value.dragObject;

			var uiInput = (UIActions)ray.Value.uiInput;
			var hoverObject = ray.Value.hoverObject;

			// Instead of using wasJustPressed, use isHeld to allow dragging with a single press
			if (hoverObject && Selection.gameObjects.Contains(hoverObject) && !ray.Value.dragObject && uiInput.select.isHeld)
			{
				//Disable original ray so that it doesn't interrupt dragging by activating its ActionMapInput
				originalRayOrigin.gameObject.SetActive(false);

				ray.Value.dragObject = hoverObject; // Capture object for later use
				var inverseRotation = Quaternion.Inverse(ray.Key.rotation);
				ray.Value.dragObjectRotationOffset = inverseRotation * hoverObject.transform.rotation;
				ray.Value.dragObjectPositionOffset = inverseRotation * (hoverObject.transform.position - ray.Key.position);
				ray.Value.dragObjectOriginalScale = hoverObject.transform.localScale;
			}

			if (!ray.Value.dragObject)
				continue;

			if (uiInput.@select.isHeld)
			{
				var selectedObjectTransform = ray.Value.dragObject.transform;
				// If the pointer is inside the MiniWorld, position at an offset from controller position
				if (ray.Key.gameObject.activeSelf)
				{
					selectedObjectTransform.localScale = ray.Value.dragObjectOriginalScale;
					selectedObjectTransform.position = ray.Key.position + ray.Key.rotation * ray.Value.dragObjectPositionOffset;
					selectedObjectTransform.rotation = ray.Key.rotation * ray.Value.dragObjectRotationOffset;
				}
				// If the object is outside, attach to controller as a preview
				else
				{
					m_ObjectPlacementModule.Preview(selectedObjectTransform, GetPreviewOriginForRayOrigin(originalRayOrigin));

					selectedObjectTransform.transform.localScale = Vector3.one;
					var totalBounds = U.Object.GetTotalBounds(selectedObjectTransform.transform);
					if (totalBounds != null)
						selectedObjectTransform.transform.localScale = Vector3.one * (0.1f / totalBounds.Value.size.MaxComponent());
				}
			}

			// Release the current object if the trigger is no longer held
			if (!uiInput.@select.isHeld && ray.Value.dragObject)
			{
				originalRayOrigin.gameObject.SetActive(true);

				PlaceObject(ray.Value.dragObject.transform, ray.Value.dragObjectOriginalScale);
				ray.Value.dragObject = null;
			}
		}
	}

	private GameObject GetFirstGameObject(Transform rayOrigin)
	{
		var go = m_PixelRaycastModule.GetFirstGameObject(rayOrigin);
		if (go)
			return go;

		if (m_IntersectionModule)
		{
			// If a raycast did not find an object, it's possible that the tester is completely contained within the object,
			// so in that case use the spatial hash as a final test
			var tester = rayOrigin.GetComponentInChildren<IntersectionTester>();
		if (m_IntersectionModule)
		{
			var renderer = m_IntersectionModule.GetIntersectedObjectForTester(tester);
			if (renderer)
				return renderer.gameObject;
			}
		}

		foreach (var miniWorldRay in m_MiniWorldRays)
		{
			if (miniWorldRay.Value.originalRayOrigin.Equals(rayOrigin))
			{
				go = m_PixelRaycastModule.GetFirstGameObject(miniWorldRay.Key);
				if (go)
					return go;
			}
		}

		return null;
	}

	private void PlaceObject(Transform obj, Vector3 targetScale)
	{
		var inMiniWorld = false;
		foreach (var miniWorld in m_MiniWorlds)
		{
			if (!miniWorld.Contains(obj.position))
				continue;

			inMiniWorld = true;
			var referenceTransform = miniWorld.referenceTransform;
			obj.transform.parent = null;
			obj.position = referenceTransform.position + Vector3.Scale(miniWorld.miniWorldTransform.InverseTransformPoint(obj.position), miniWorld.referenceTransform.localScale);
			obj.rotation = referenceTransform.rotation * Quaternion.Inverse(miniWorld.miniWorldTransform.rotation) * obj.rotation;
			obj.localScale = Vector3.Scale(Vector3.Scale(obj.localScale, referenceTransform.localScale), miniWorld.miniWorldTransform.lossyScale);
			break;
		}
		if (!inMiniWorld)
			m_ObjectPlacementModule.PlaceObject(obj, targetScale);
	}

	private Transform GetPreviewOriginForRayOrigin(Transform rayOrigin)
	{
		return (from proxy in m_AllProxies
				from origin in proxy.rayOrigins
					where origin.Value.Equals(rayOrigin)
						select proxy.previewOrigins[origin.Key]).FirstOrDefault();
	}

#if UNITY_EDITOR
	private static EditorVR s_Instance;
	private static InputManager s_InputManager;

	[MenuItem("Window/EditorVR", false)]
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
	}

	private static void OnEVRDisabled()
	{
		U.Object.Destroy(s_Instance.gameObject);
		U.Object.Destroy(s_InputManager.gameObject);
	}
#endif
}