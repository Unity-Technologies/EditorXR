using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.InputNew;
using UnityEngine.VR;
using UnityEngine.VR.Proxies;
using UnityEngine.VR.Tools;
using UnityEngine.VR.Utilities;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.VR;
#endif

[InitializeOnLoad]
public class EditorVR : MonoBehaviour
{
	public const HideFlags kDefaultHideFlags = HideFlags.DontSave;

	[SerializeField]
	private ActionMap m_MenuActionMap;
	[SerializeField]
	private ActionMap m_DefaultActionMap;
	[SerializeField]
	private ActionMap m_TrackedObjectActionMap;
	[SerializeField]
	private ActionMap m_StandardToolActionMap;
	[SerializeField]
	private VRLineRenderer m_PointerRayPrefab;

	private TrackedObject m_TrackedObjectInput;
	private Default m_DefaultActionInput;

	private EventSystem m_EventSystem;
	private MultipleRayInputModule m_InputModule;
	private Camera m_EventCamera;
	//HACK: static event camera for workspaces
	public static Camera eventCamera;

	private PlayerHandle m_PlayerHandle;

	private class DeviceData
	{
		public Stack<ITool> tools;
		public Menu menuInput;
		public ActionMapInput uiInput;
		public IMainMenu mainMenu;
		public ITool currentTool;
	}

	private Dictionary<InputDevice, DeviceData> m_DeviceData = new Dictionary<InputDevice, DeviceData>();
	private List<IProxy> m_AllProxies = new List<IProxy>();
	private IEnumerable<Type> m_AllTools;

	private Dictionary<Type, List<ActionMap>> m_ToolActionMaps;

	private Dictionary<string, Node> m_TagToNode = new Dictionary<string, Node>
	{
		{ "Left", Node.LeftHand },
		{ "Right", Node.RightHand }
	};

	private void Awake()
	{
		VRView.viewerPivot.parent = transform; // Parent the camera pivot under EditorVR
		VRView.viewerPivot.localPosition = Vector3.zero; // HACK reset pivot to match steam origin
		InitializePlayerHandle();
		CreateDefaultActionMapInputs();
		CreateAllProxies();
		CreateDeviceDataForInputDevices();
		CreateEventSystem();
		m_AllTools = U.Object.GetImplementationsOfInterface(typeof(ITool));
		// TODO: Only show tools in the menu for the input devices in the action map that match the devices present in the system.  This is why we're collecting all the action maps
		//		Additionally, if the action map only has a single hand specified, then only show it in that hand's menu.
		m_ToolActionMaps = CollectToolActionMaps(m_AllTools);

		//HACK: Create a workspace at start to work around Vive input issues
		Workspace.ShowWorkspace<ChessboardWorkspace>(transform);	
	}

	private void CreateDeviceDataForInputDevices()
	{
		foreach (var device in InputSystem.devices)
		{
			var deviceData = new DeviceData
			{
				tools = new Stack<ITool>(),
				menuInput = (Menu)CreateActionMapInput(CloneActionMapForDevice(m_MenuActionMap, device))
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

		// HACK: U.AddComponent doesn't work properly from an IEnumerator (missing default references when spawned), so currently
		// it's necessary to spawn the tools in a separate non-IEnumerator context.
		EditorApplication.delayCall += () =>
		{
			HashSet<InputDevice> devices;
			var tool = SpawnTool(typeof(JoystickLocomotionTool), out devices);
			AddToolToDeviceData(tool, devices);
		};
	}

	private void OnDestroy()
	{
		PlayerHandleManager.RemovePlayerHandle(m_PlayerHandle);
	}

	private void Update()
	{
		foreach (var proxy in m_AllProxies)
		{
			proxy.hidden = !proxy.active;
		}

		foreach (var kvp in m_DeviceData)
		{
			if (kvp.Value.menuInput.show.wasJustPressed)
			{
				var device = kvp.Key;
				if (m_DeviceData[device].mainMenu != null) // Close menu if already open
				{
					U.Object.Destroy(m_DeviceData[device].mainMenu as MonoBehaviour);
					m_DeviceData[device].mainMenu = null;
				}
				else
				{
					// HACK to workaround missing MonoScript serialized fields
					EditorApplication.delayCall += () =>
					{
						SpawnMainMenu(typeof(MainMenuDev), device);
					};
				}
			}
		}
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
		m_TrackedObjectInput = (TrackedObject)CreateActionMapInput(m_TrackedObjectActionMap);
		m_DefaultActionInput = (Default)CreateActionMapInput(m_DefaultActionMap);

		UpdatePlayerHandleMaps();
	}

	private void CreateAllProxies()
	{
		foreach (Type proxyType in U.Object.GetImplementationsOfInterface(typeof(IProxy)))
		{
			IProxy proxy = U.Object.CreateGameObjectWithComponent(proxyType, VRView.viewerPivot) as IProxy;
			proxy.trackedObjectInput = m_PlayerHandle.GetActions<TrackedObject>();
			foreach (var rayOriginBase in proxy.rayOrigins)
			{
				var rayTransform = U.Object.InstantiateAndSetActive(m_PointerRayPrefab.gameObject, rayOriginBase.Value).transform;
				rayTransform.position = rayOriginBase.Value.position;
				rayTransform.rotation = rayOriginBase.Value.rotation;
			}
			m_AllProxies.Add(proxy);
		}
	}

	private void CreateEventSystem()
	{
		// Create event system, input module, and event camera
		m_EventSystem = U.Object.AddComponent<EventSystem>(gameObject);
		m_InputModule = U.Object.AddComponent<MultipleRayInputModule>(gameObject);
		m_EventCamera = U.Object.InstantiateAndSetActive(m_InputModule.EventCameraPrefab.gameObject, transform).GetComponent<Camera>();
		//HACK: static event camera for workspaces
		eventCamera = m_EventCamera;
		m_InputModule.eventCamera = m_EventCamera;
		m_InputModule.eventCamera.clearFlags = CameraClearFlags.Nothing;
		m_InputModule.eventCamera.cullingMask = 0;

		foreach (var proxy in m_AllProxies)
		{
			foreach (var rayOriginBase in proxy.rayOrigins)
			{
				foreach (var device in InputSystem.devices) // Find device tagged with the node that matches this RayOrigin node
				{
					if (device.TagIndex != -1 && m_TagToNode[VRInputDevice.Tags[device.TagIndex]] == rayOriginBase.Key)
					{
						DeviceData deviceData;
						if (m_DeviceData.TryGetValue(device, out deviceData))
						{
							// Create ui action map input for device.
							if (deviceData.uiInput == null)
								deviceData.uiInput = CreateActionMapInput(CloneActionMapForDevice(m_InputModule.actionMap, device));

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
		var go = U.Object.InstantiateAndSetActive(prefab, transform);
		foreach (Canvas canvas in go.GetComponentsInChildren<Canvas>())
			canvas.worldCamera = m_EventCamera;
		return go;
	}

	private ActionMapInput CreateActionMapInput(ActionMap map)
	{
		var actionMapInput = ActionMapInput.Create(map);
		actionMapInput.TryInitializeWithDevices(m_PlayerHandle.GetApplicableDevices());
		actionMapInput.active = true;
		return actionMapInput;
	}

	private void UpdatePlayerHandleMaps()
	{
		var maps = m_PlayerHandle.maps;
		maps.Clear();

		foreach (DeviceData deviceData in m_DeviceData.Values)
		{
			maps.Add(deviceData.menuInput);

			// Not every tool has UI
			if (deviceData.uiInput != null)
				maps.Add(deviceData.uiInput);
		}

		maps.Add(m_TrackedObjectInput);

		foreach (DeviceData deviceData in m_DeviceData.Values)
		{
			foreach (ITool tool in deviceData.tools.Reverse())
			{
				IStandardActionMap standardActionMap = tool as IStandardActionMap;
				if (standardActionMap != null)
				{
					if (!maps.Contains(standardActionMap.standardInput))
						maps.Add(standardActionMap.standardInput);
				}

				ICustomActionMap customActionMap = tool as ICustomActionMap;
				if (customActionMap != null)
				{
					if (!maps.Contains(customActionMap.actionMapInput))
						maps.Add(customActionMap.actionMapInput);
				}
			}
		}

		maps.Add(m_DefaultActionInput);
	}

	private void LogError(string error)
	{
		Debug.LogError(string.Format("EVR: {0}", error));
	}

	/// <summary>
	/// Spawn a tool on a tool stack (e.g. right hand). In some cases, a tool may be device tag agnostic (e.g. right or
	/// left hand), so in those cases we map the source bindings of the action map input to the correct device tag.
	/// </summary>
	/// <param name="toolType">The tool to spawn</param>
	/// <param name="device">The input device whose tool stack the tool should be spawned on 
	/// (optional). If not specified, then it uses the action map to determine which devices the tool should be spawned
	/// on.</param>
	/// <returns> Returns tool that was spawned or null if the spawn failed.</returns>
	private ITool SpawnTool(Type toolType, out HashSet<InputDevice> usedDevices, InputDevice device = null)
	{
		usedDevices = new HashSet<InputDevice>();
		if (!typeof(ITool).IsAssignableFrom(toolType))
			return null;

		var serializableTypes = new HashSet<SerializableType>();
		var tool = U.Object.AddComponent(toolType, gameObject) as ITool;
		var standardMap = tool as IStandardActionMap;
		if (standardMap != null)
		{
			var actionMap = m_StandardToolActionMap;
			if (device != null)
				actionMap = CloneActionMapForDevice(actionMap, device);

			standardMap.standardInput = (Standard)CreateActionMapInput(actionMap);
			usedDevices.UnionWith(standardMap.standardInput.GetCurrentlyUsedDevices());
			U.Input.CollectSerializableTypesFromActionMapInput(standardMap.standardInput, ref serializableTypes);
		}

		var customMap = tool as ICustomActionMap;
		if (customMap != null)
		{
			var actionMap = customMap.actionMap;
			if (device != null)
				actionMap = CloneActionMapForDevice(actionMap, device);

			customMap.actionMapInput = CreateActionMapInput(actionMap);
			usedDevices.UnionWith(customMap.actionMapInput.GetCurrentlyUsedDevices());
			U.Input.CollectSerializableTypesFromActionMapInput(customMap.actionMapInput, ref serializableTypes);
		}

		ConnectInterfaces(tool, device);
		return tool;
	}

	private void AddToolToDeviceData(ITool tool, HashSet<InputDevice> devices)
	{
		foreach (var dev in devices)
			AddToolToStack(dev, tool);
	}

	private void SpawnMainMenu(Type type, InputDevice device)
	{
		if (!typeof(IMainMenu).IsAssignableFrom(type))
			return;

		var mainMenu = U.Object.AddComponent(type, gameObject) as IMainMenu;
		if (mainMenu != null)
		{
			mainMenu.menuTools = m_AllTools.ToList();
			mainMenu.selectTool = SelectTool;
			m_DeviceData[device].mainMenu = mainMenu;
			ConnectInterfaces(mainMenu, device);
		}
	}

	private void ConnectInterfaces(object obj, InputDevice device = null)
	{
		if (device != null)
		{
			var ray = obj as IRay;
			if (ray != null)
			{
				foreach (var proxy in m_AllProxies)
				{
					if (!proxy.active)
						continue;

					var tags = InputDeviceUtility.GetDeviceTags(device.GetType());
					if (device.TagIndex == -1)
						continue;

					var tag = tags[device.TagIndex];
					Node node;
					if (m_TagToNode.TryGetValue(tag, out node))
					{
						Transform rayOrigin;
						if (proxy.rayOrigins.TryGetValue(node, out rayOrigin))
						{
							ray.rayOrigin = rayOrigin;
							break;
						}
					}
				}
			}
		}

		var locomotionComponent = obj as ILocomotion;
		if (locomotionComponent != null)
			locomotionComponent.viewerPivot = VRView.viewerPivot;

		var instantiateUITool = obj as IInstantiateUI;
		if (instantiateUITool != null)
			instantiateUITool.instantiateUI = InstantiateUI;
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

	private bool SelectTool(IMainMenu menu, Type tool)
	{
		InputDevice device = null;
		foreach (var kvp in m_DeviceData)
		{
			if (kvp.Value.mainMenu == menu)
				device = kvp.Key;
		}

		if (device == null)
			return false;

		// HACK to workaround missing serialized fields coming from the MonoScript
		EditorApplication.delayCall += () =>
		{
			// Spawn tool and collect all devices that this tool will need
			HashSet<InputDevice> usedDevices;
			var newTool = SpawnTool(tool, out usedDevices, device);

			foreach (var dev in usedDevices)
			{
				var deviceData = m_DeviceData[dev];
				if (deviceData.currentTool != null) // Remove the current tool on all devices this tool will be spawned on
					DespawnTool(deviceData.currentTool);

				deviceData.tools.Push(newTool);
				deviceData.currentTool = newTool;
			}
			UpdatePlayerHandleMaps();
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

	private ActionMap CloneActionMapForDevice(ActionMap actionMap, InputDevice device)
	{
		var cloneMap = ScriptableObject.CreateInstance<ActionMap>();
		EditorUtility.CopySerialized(actionMap, cloneMap);
		UpdateActionMapForDevice(cloneMap, device);

		return cloneMap;
	}

	private void UpdateActionMapForDevice(ActionMap actionMap, InputDevice device)
	{
		var untaggedDevicesFound = 0;
		var taggedDevicesFound = 0;
		var nonMatchingTagIndices = 0;
		var matchingTagIndices = 0;

		foreach (var scheme in actionMap.controlSchemes)
		{
			foreach (var serializableDeviceType in scheme.serializableDeviceTypes)
			{
				if (serializableDeviceType.TagIndex != -1)
				{
					taggedDevicesFound++;
					if (serializableDeviceType.TagIndex != device.TagIndex)
						nonMatchingTagIndices++;
					else
						matchingTagIndices++;
				}
				else
					untaggedDevicesFound++;
			}
		}

		if (nonMatchingTagIndices > 0 && matchingTagIndices == 0) // There is no specified tag that matches the current device, but there is one that does not match
			LogError(string.Format("The action map {0} contains a specific device tag, but is being spawned on the wrong device tag", actionMap));

		if (taggedDevicesFound > 0 && untaggedDevicesFound != 0)
			LogError(string.Format("The action map {0} contains both a specific device tag and an unspecified tag, which is not supported", actionMap.name));

		// Update all device tags and bindings tagged with no tag (index -1)
		foreach (var scheme in actionMap.controlSchemes)
		{
			foreach (var serializableDeviceType in scheme.serializableDeviceTypes)
			{
				if (serializableDeviceType.TagIndex == -1)
					serializableDeviceType.TagIndex = device.TagIndex;
			}

			foreach (var binding in scheme.bindings)
			{
				foreach (var source in binding.sources)
				{
					if (source.deviceType.value == device.GetType() && source.deviceType.TagIndex == -1)
						source.deviceType.TagIndex = device.TagIndex;
				}
			}
		}
	}

	private void AddToolToStack(InputDevice device, ITool tool)
	{
		if (tool != null)
		{
			m_DeviceData[device].tools.Push(tool);
			UpdatePlayerHandleMaps();
		}
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
		//	in edit mode we need to handle lifecycle explicitly
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
