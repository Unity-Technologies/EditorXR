using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
	private ActionMap m_DefaultActionMap;
	[SerializeField]
	private ActionMap m_TrackedObjectActionMap;
	[SerializeField]
	private ActionMap m_StandardToolActionMap;
	[SerializeField]
	private DefaultProxyRay m_ProxyRayPrefab;
	[SerializeField]
	private Camera m_EventCameraPrefab;

	private readonly Dictionary<Transform, DefaultProxyRay> m_DefaultRays = new Dictionary<Transform, DefaultProxyRay>();

	private TrackedObject m_TrackedObjectInput;
	private Default m_DefaultActionInput;

	private MultipleRayInputModule m_InputModule;
	private Camera m_EventCamera;
	private PixelRaycastModule m_PixelRaycastModule;
	private HighlightModule m_HighlightModule;
	private ObjectPlacementModule m_ObjectPlacementModule;

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
	private IEnumerable<Type> m_AllTools;
	private IEnumerable<Type> m_AllWorkspaceTypes;
	private readonly List<Workspace> m_AllWorkspaces = new List<Workspace>();

	private Dictionary<string, Node> m_TagToNode = new Dictionary<string, Node>
	{
		{ "Left", Node.LeftHand },
		{ "Right", Node.RightHand }
	};

	private void Awake()
	{
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
		CreateEventSystem();

		m_PixelRaycastModule = U.Object.AddComponent<PixelRaycastModule>(gameObject);
		m_PixelRaycastModule.ignoreRoot = transform;
		m_HighlightModule = U.Object.AddComponent<HighlightModule>(gameObject);
		m_ObjectPlacementModule = U.Object.AddComponent<ObjectPlacementModule>(gameObject);

		m_AllTools = U.Object.GetImplementationsOfInterface(typeof(ITool));
		m_AllWorkspaceTypes = U.Object.GetExtensionsOfClass(typeof(Workspace));

		// TODO: Only show tools in the menu for the input devices in the action map that match the devices present in the system.  
		// This is why we're collecting all the action maps. Additionally, if the action map only has a single hand specified, 
		// then only show it in that hand's menu.
		// CollectToolActionMaps(m_AllTools);
	}

	private void CreateDeviceDataForInputDevices()
	{
		foreach (var device in InputSystem.devices)
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
		//Workspaces don't need to wait until devices are active
		CreateDefaultWorkspaces();

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
		SpawnDefaultTools();
	}

	private void OnEnable()
	{
#if UNITY_EDITOR
		VRView.onGUIDelegate += OnSceneGUI;
#endif
	}

	private void OnDisable()
	{
#if UNITY_EDITOR
		VRView.onGUIDelegate -= OnSceneGUI;
#endif
	}

	private void OnSceneGUI(EditorWindow obj)
	{
		if (Event.current.type == EventType.MouseMove)
		{
			m_PixelRaycastModule.UpdateRaycasts(m_AllProxies, m_EventCamera);
			UpdateDefaultProxyRays();
		}
	}

	private void OnDestroy()
	{
		PlayerHandleManager.RemovePlayerHandle(m_PlayerHandle);
	}

	private void Update()
	{
		foreach (var proxy in m_AllProxies)
			proxy.hidden = !proxy.active;

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
				else
				{
					// HACK to workaround missing MonoScript serialized fields
					EditorApplication.delayCall += () =>
					{
						m_DeviceData[device].mainMenu = SpawnMainMenu(typeof(MainMenu), device);
						UpdatePlayerHandleMaps();
					};
				}
			}
		}

		var camera = U.Camera.GetMainCamera();
		// Enable/disable workspace vacuum bounds based on distance to camera
		foreach (var workspace in m_AllWorkspaces)
			workspace.vacuumEnabled = (workspace.transform.position - camera.transform.position).magnitude > kWorkspaceVacuumEnableDistance;

#if UNITY_EDITOR
		// HACK: Send a "mouse moved" event, so scene picking can occur for the controller
		Event e = new Event();
		e.type = EventType.MouseMove;
		VRView.activeView.SendEvent(e);
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
		m_DefaultActionInput = (Default)CreateActionMapInput(m_DefaultActionMap, null);

		UpdatePlayerHandleMaps();
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
			foreach (var deviceData in m_DeviceData)
			{
				// Skip keyboard, mouse, gamepads. Selection tool should only be on left and right hands (tagged 0 and 1)
				if (deviceData.Key.tagIndex == -1)
					continue;

				tool = SpawnTool(typeof(SelectionTool), out devices, deviceData.Key);
				AddToolToDeviceData(tool, devices);

				tool = SpawnTool(typeof(BlinkLocomotionTool), out devices, deviceData.Key);
				AddToolToDeviceData(tool, devices);
			}

			tool = SpawnTool(typeof(TransformTool), out devices);
			AddToolToDeviceData(tool, devices);
		};
	}

	private void CreateAllProxies()
	{
		foreach (Type proxyType in U.Object.GetImplementationsOfInterface(typeof(IProxy)))
		{
			IProxy proxy = U.Object.CreateGameObjectWithComponent(proxyType, VRView.viewerPivot) as IProxy;
			proxy.trackedObjectInput = m_PlayerHandle.GetActions<TrackedObject>();
			foreach (var rayOriginBase in proxy.rayOrigins)
			{
				var rayTransform = U.Object.Instantiate(m_ProxyRayPrefab.gameObject, rayOriginBase.Value).transform;
				rayTransform.position = rayOriginBase.Value.position;
				rayTransform.rotation = rayOriginBase.Value.rotation;
				m_DefaultRays.Add(rayOriginBase.Value, rayTransform.GetComponent<DefaultProxyRay>());
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
					// If not hitting UI, then check pixel raycast and approximate bounds to set distance
					var go = m_PixelRaycastModule.GetFirstGameObject(rayOrigin);
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
		foreach (var proxy in m_AllProxies)
		{
			foreach (var rayOriginBase in proxy.rayOrigins)
			{
				foreach (var device in InputSystem.devices) // Find device tagged with the node that matches this RayOrigin node
				{
					var node = GetDeviceNode(device);
					if (node.HasValue && node.Value == rayOriginBase.Key)
					{
						DeviceData deviceData;
						if (m_DeviceData.TryGetValue(device, out deviceData))
						{
							// Create ui action map input for device.
							if (deviceData.uiInput == null)
								deviceData.uiInput = CreateActionMapInput(m_InputModule.actionMap, device);

							// Add RayOrigin transform, proxy and ActionMapInput references to input module list of sources
							m_InputModule.AddRaycastSource(proxy, rayOriginBase.Key, deviceData.uiInput);
						}
						break;
					}
				}
			}
		}
		UpdatePlayerHandleMaps();
	}

	private GameObject InstantiateUI(GameObject prefab)
	{
		var go = U.Object.Instantiate(prefab, transform);
		foreach (Canvas canvas in go.GetComponentsInChildren<Canvas>())
			canvas.worldCamera = m_EventCamera;
		return go;
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

		maps.Add(m_TrackedObjectInput);

		foreach (DeviceData deviceData in m_DeviceData.Values)
		{
			foreach (ITool tool in deviceData.tools)
				AddActionMapInputs(tool, maps);
		}

		maps.Add(m_DefaultActionInput);
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

	private IMainMenu SpawnMainMenu(Type type, InputDevice device)
	{
		if (!typeof(IMainMenu).IsAssignableFrom(type))
			return null;

		var mainMenu = U.Object.AddComponent(type, gameObject) as IMainMenu;
		ConnectActionMaps(mainMenu, device);
		ConnectInterfaces(mainMenu, device);
		mainMenu.visible = true;

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
			raycaster.getFirstGameObject = m_PixelRaycastModule.GetFirstGameObject;

		var highlight = obj as IHighlight;
		if (highlight != null)
			highlight.setHighlight = m_HighlightModule.SetHighlight;

		var placeObjects = obj as IPlaceObjects;
		if (placeObjects != null)
			placeObjects.placeObject = m_ObjectPlacementModule.PlaceObject;

		if (mainMenu != null)
		{
			mainMenu.menuTools = m_AllTools.ToList();
			mainMenu.selectTool = SelectTool;
			mainMenu.menuWorkspaces = m_AllWorkspaceTypes.ToList();
			mainMenu.createWorkspace = CreateWorkspace;
			mainMenu.node = GetDeviceNode(device);
			mainMenu.setup();
		}
	}

	private float GetPointerLength(Transform rayOrigin)
	{
		float length = 0f;
		DefaultProxyRay dpr;
		if (m_DefaultRays.TryGetValue(rayOrigin, out dpr))
		{
			length = dpr.pointerLength;
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

	private bool SelectTool(Node targetNode, Type tool)
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
			// Spawn tool and collect all devices that this tool will need
			HashSet<InputDevice> usedDevices;
			var newTool = SpawnTool(tool, out usedDevices, deviceToAssignTool);

			foreach (var dev in usedDevices)
			{
				var deviceData = m_DeviceData[dev];
				if (deviceData.currentTool != null) // Remove the current tool on all devices this tool will be spawned on
					DespawnTool(deviceData.currentTool);

				AddToolToStack(dev, newTool);
			}
		};

		return true;
	}

	private void DespawnTool(ITool tool)
	{
		foreach (var deviceData in m_DeviceData.Values)
		{
			// Remove the tool if it is the current tool on this device tool stack
			if (deviceData.currentTool == tool)
			{
				if (deviceData.tools.Peek() != deviceData.currentTool)
				{
					Debug.LogError("Tool at top of stack is not current tool.");
					continue;
				}
				deviceData.tools.Pop();
				deviceData.currentTool = null;
			}
		}
		U.Object.Destroy(tool as MonoBehaviour);
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
			UpdatePlayerHandleMaps();
		}
	}

	private void CreateDefaultWorkspaces()
	{
		CreateWorkspace<ProjectWorkspace>();
		CreateWorkspace<ChessboardWorkspace>();
		CreateWorkspace<ConsoleWorkspace>();
	}

	private void CreateWorkspace<T>() where T : Workspace
	{
		CreateWorkspace(typeof(T));
	}

	private void CreateWorkspace(Type t)
	{
		var defaultOffset = Workspace.kDefaultOffset;
		var defaultTilt = Workspace.kDefaultTilt;

		var viewerPivot = U.Camera.GetViewerPivot();
		Vector3 position = viewerPivot.position + defaultOffset;

		Quaternion rotation = defaultTilt;
		float arcLength = Mathf.Atan(Workspace.kDefaultBounds.x /
			(defaultOffset.z - Workspace.kDefaultBounds.z * 0.5f)) * Mathf.Rad2Deg		//Calculate arc length at front of workspace
			+ kWorkspaceAnglePadding;													//Need some extra padding because workspaces are tilted
		float heightOffset = Workspace.kDefaultBounds.y + kWorkspaceYPadding;			//Need padding in Y as well

		float currentRotation = arcLength;
		float currentHeight = 0;

		int count = 0;
		int direction = 1;
		Vector3 halfBounds = Workspace.kDefaultBounds * 0.5f;
		
		//While the current position is occupied, try a new one
		while (Physics.CheckBox(position, halfBounds, rotation) && count++ < kMaxWorkspacePlacementAttempts)
		{
			//The next position will be rotated by currentRotation, as if the hands of a clock
			Quaternion rotateAroundY = Quaternion.AngleAxis(currentRotation * direction, Vector3.up);
			position = viewerPivot.position + rotateAroundY * defaultOffset + Vector3.up * currentHeight;
			rotation = rotateAroundY * defaultTilt;
			
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

		Workspace workspace = (Workspace)U.Object.CreateGameObjectWithComponent(t, U.Camera.GetViewerPivot());
		m_AllWorkspaces.Add(workspace);
		workspace.destroyed += OnWorkspaceDestroyed;
		ConnectInterfaces(workspace);
		workspace.transform.position = position;
		workspace.transform.rotation = rotation;
		
		//Explicit setup call (instead of setting up in Awake) because we need interfaces to be hooked up first
		workspace.Setup();
	}

	private void OnWorkspaceDestroyed(Workspace workspace)
	{
		m_AllWorkspaces.Remove(workspace);
	}

#if UNITY_EDITOR
	private static EditorVR s_Instance;
	private static InputManager s_InputManager;

	[MenuItem("Window/EditorVR", false)]
	public static void ShowEditorVR()
	{
		VRView.GetWindow<VRView>("EditorVR", true);
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