using UnityEngine;
using System.Collections;
using System;
using UnityEngine.Assertions;
using UnityEngine.InputNew;
using UnityEngine.VR.Proxies;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;
using UnityEngine.VR.Tools;
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

    private PlayerHandle m_PlayerHandle;

	private class ToolStack
	{
		public Stack<ITool> tools;
		public Menu menuInput;
		public ActionMapInput uiInput;
	}

    private Dictionary<InputDevice, ToolStack> m_ToolStacks = new Dictionary<InputDevice, ToolStack>();
    private List<IProxy> m_AllProxies = new List<IProxy>();
    private IEnumerable<Type> m_AllTools;

	private Dictionary<Type, List<ActionMap>> m_ToolActionMaps;

	private Dictionary<string, Node> m_TagToNode = new Dictionary<string, Node>
	{
		{ "Left", Node.LeftHand },
		{ "Right", Node.RightHand }
	};

	// TEMP
	InputDevice leftHand = null;
	InputDevice rightHand = null;

	private void Awake()
    {
        EditorVRView.viewerPivot.parent = transform; // Parent the camera pivot under EditorVR
        EditorVRView.viewerPivot.localPosition = Vector3.zero; // HACK reset pivot to match steam origin
        InitializePlayerHandle();
        CreateDefaultActionMapInputs();
        CreateAllProxies();
		CreateToolStacks();
        CreateEventSystem();
		m_AllTools = U.GetImplementationsOfInterface(typeof(ITool));
		// TODO: Only show tools in the menu for the input devices in the action map that match the devices present in the system.  This is why we're collecting all the action maps
		//		Additionally, if the action map only has a single hand specified, then only show it in that hand's menu.
		m_ToolActionMaps = CollectToolActionMaps(m_AllTools);		
    }

	private void CreateToolStacks()
	{
		foreach (var device in InputSystem.devices)
		{
			// HACK to grab left and right hand for now
			if (device.GetType() == typeof(VRInputDevice) && device.TagIndex != -1)
			{
				if (VRInputDevice.Tags[device.TagIndex] == "Left")
					leftHand = device;
				else if (VRInputDevice.Tags[device.TagIndex] == "Right")
					rightHand = device;
			}
			var toolStack = new ToolStack
			{
				tools = new Stack<ITool>(),
				menuInput = (Menu)CreateActionMapInput(CloneActionMapForDevice(m_MenuActionMap, device))
			};
			m_ToolStacks.Add(device, toolStack);
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
				if (proxy.Active)
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
			SpawnTool(typeof(JoystickLocomotionTool));
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
			proxy.Hidden = !proxy.Active;
		}

		foreach (var kvp in m_ToolStacks)
		{
			if (kvp.Value.menuInput.show.wasJustPressed && !kvp.Value.tools.Any(t => t.GetType() == typeof(MainMenuDev)))
			{
				// HACK to workaround missing MonoScript serialized fields
			    var device = kvp.Key;
				EditorApplication.delayCall += () =>
				{
					SpawnTool(typeof(MainMenuDev), device);
				};
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
				actionMaps.Add(customActionMap.ActionMap);

		    var standardActionMap = tool as IStandardActionMap;
			if (standardActionMap != null)
				actionMaps.Add(m_StandardToolActionMap);

		    toolMaps.Add(t, actionMaps);

			U.Destroy(tool as MonoBehaviour);
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
        foreach (Type proxyType in U.GetImplementationsOfInterface(typeof(IProxy)))
        {
            IProxy proxy = U.CreateGameObjectWithComponent(proxyType, EditorVRView.viewerPivot) as IProxy;
		    proxy.TrackedObjectInput = m_PlayerHandle.GetActions<TrackedObject>();
            foreach (var rayOriginBase in proxy.RayOrigins)
            {
                var rayTransform = U.InstantiateAndSetActive(m_PointerRayPrefab.gameObject, rayOriginBase.Value).transform;
                rayTransform.position = rayOriginBase.Value.position;
                rayTransform.rotation = rayOriginBase.Value.rotation;
            }
			m_AllProxies.Add(proxy);
        }
    }

    private void CreateEventSystem()
    {
        // Create event system, input module, and event camera
        m_EventSystem = U.AddComponent<EventSystem>(gameObject);
        m_InputModule = U.AddComponent<MultipleRayInputModule>(gameObject);
        m_EventCamera = U.InstantiateAndSetActive(m_InputModule.EventCameraPrefab.gameObject, transform).GetComponent<Camera>();
        m_InputModule.EventCamera = m_EventCamera;
        m_InputModule.EventCamera.clearFlags = CameraClearFlags.Nothing;
        m_InputModule.EventCamera.cullingMask = 0;

        foreach (var proxy in m_AllProxies)
        {
            foreach (var rayOriginBase in proxy.RayOrigins)
            {
                foreach (var device in InputSystem.devices) // Find device tagged with the node that matches this RayOrigin node, and update the action map copy
                {
                    if (device.TagIndex != -1 && m_TagToNode[VRInputDevice.Tags[device.TagIndex]] == rayOriginBase.Key)
                    {
	                    ToolStack toolStack;
	                    if (m_ToolStacks.TryGetValue(device, out toolStack))
	                    {
		                    // Add ActionMapInput to player handle maps stack below default maps and above tools, and increase the offset index where tool inputs will be added
                            if(toolStack.uiInput == null)
		                        toolStack.uiInput = CreateActionMapInput(CloneActionMapForDevice(m_InputModule.ActionMap, device));

		                    // Add RayOrigin transform, proxy and ActionMapInput references to input module list of sources
                            m_InputModule.AddRaycastSource(proxy, rayOriginBase.Value, toolStack.uiInput);
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
        var go = U.InstantiateAndSetActive(prefab, transform);
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

		foreach (ToolStack toolStack in m_ToolStacks.Values)
		{
			maps.Add(toolStack.menuInput);

			// Not every tool has UI
			if (toolStack.uiInput != null)
				maps.Add(toolStack.uiInput);
		}

	    maps.Add(m_TrackedObjectInput);

	    foreach (ToolStack toolStack in m_ToolStacks.Values)
        {
            foreach (ITool tool in toolStack.tools.Reverse())
            {
	            IStandardActionMap standardActionMap = tool as IStandardActionMap;
	            if (standardActionMap != null)
	            {
		            if (!maps.Contains(standardActionMap.StandardInput))
		            {
			            maps.Add(standardActionMap.StandardInput);
		            }
				}

				ICustomActionMap customActionMap = tool as ICustomActionMap;
	            if (customActionMap != null)
	            {
		            if (!maps.Contains(customActionMap.ActionMapInput))
		            {
						maps.Add(customActionMap.ActionMapInput);
					}
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
	/// <param name="device">The input device that serves as a key for the tool stack that the tool should spawned on 
	/// (optional). If not specified, then it uses the action map to determine which devices the tool should be spawned
	/// on.</param>
	private void SpawnTool(Type toolType, InputDevice device = null)
	{
		if (!typeof(ITool).IsAssignableFrom(toolType))
			return;

		HashSet<SerializableType> serializableTypes = new HashSet<SerializableType>();
		var tool = U.AddComponent(toolType, gameObject) as ITool;
		var standardMap = tool as IStandardActionMap;
		if (standardMap != null)
		{
			ActionMap actionMap = m_StandardToolActionMap;

			if (device != null)
			{
				actionMap = CloneActionMapForDevice(actionMap, device);
			}

			standardMap.StandardInput = (Standard)CreateActionMapInput(actionMap);
			U.CollectSerializableTypesFromActionMapInput(standardMap.StandardInput, ref serializableTypes);
		}
			
		var customMap = tool as ICustomActionMap;
		if (customMap != null)
		{
			ActionMap actionMap = customMap.ActionMap;

			if (device != null)
			{
				actionMap = CloneActionMapForDevice(actionMap, device);
			}

			customMap.ActionMapInput = CreateActionMapInput(actionMap);
			U.CollectSerializableTypesFromActionMapInput(customMap.ActionMapInput, ref serializableTypes);
		}

		if (device != null)
		{
			var untaggedDevicesFound = 0;
			var taggedDevicesFound = 0;
			foreach (var serializableType in serializableTypes)
			{
				if (serializableType.TagIndex != -1)
				{
					taggedDevicesFound++;

					if (serializableType.TagIndex != device.TagIndex)
					{
						LogError(
							string.Format("The action map for {0} contains a specific device tag, but is being spawned on the wrong device tag",
								toolType));
						U.Destroy(tool as MonoBehaviour);
						return;
					}
				}
				else
				{
					untaggedDevicesFound++;
				}
			}

			if (taggedDevicesFound > 0 && untaggedDevicesFound != 0)
			{
				LogError(
					string.Format("The action map for {0} contains both a specific device tag and an unspecified tag, which is not supported",
						toolType));
				U.Destroy(tool as MonoBehaviour);
				return;
			}
		}

		HashSet<InputDevice> devices = null;
		if (device != null)
		{
			devices = new HashSet<InputDevice> {device};
		}
		else
		{
			// TODO: Do we need to collect devices across all control schemes?
			devices = U.CollectInputDevicesFromActionMaps(m_ToolActionMaps[toolType]);
		}

		if (device != null)
		{
			var ray = tool as IRay;
			if (ray != null)
			{
				// TODO: Get active proxy per node, pass its ray origin.
				foreach (var proxy in m_AllProxies)
				{
					if (proxy.Active)
					{
						var tags = InputDeviceUtility.GetDeviceTags(device.GetType());
						var tag = tags[device.TagIndex];
						Node node;
						if (m_TagToNode.TryGetValue(tag, out node))
						{
							Transform rayOrigin;
							if (proxy.RayOrigins.TryGetValue(node, out rayOrigin))
							{
								ray.RayOrigin = rayOrigin;
								break;
							}
						}
					}
				}
			}
		}

		var locomotionComponent = tool as ILocomotion;
        if (locomotionComponent != null)
        {
            locomotionComponent.ViewerPivot = EditorVRView.viewerPivot;
        }

        var instantiateUITool = tool as IInstantiateUI;
	    if (instantiateUITool != null)
	        instantiateUITool.InstantiateUI = InstantiateUI;

		var mainMenuTool = tool as IMainMenu;
		if (mainMenuTool != null)
		{
			mainMenuTool.MenuTools = m_AllTools.ToList();
			mainMenuTool.SelectTool = SelectTool;
		}

		foreach (var dev in devices)
	    {
		    AddToolToStack(dev, tool);
	    }
	}

	private InputDevice GetInputDeviceForTool(ITool tool)
	{
		foreach (var kvp in m_ToolStacks)
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
		var device = GetInputDeviceForTool(menu as ITool);
		if (device != null)
		{
			// HACK to workaround missing serialized fields coming from the MonoScript
			EditorApplication.delayCall += () =>
			{
				SpawnTool(tool, device);
			};
			return true;
		}

		return false;
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
		foreach (var scheme in actionMap.controlSchemes)
		{
			foreach (var serializableDeviceType in scheme.serializableDeviceTypes)
			{
				if (serializableDeviceType.value == device.GetType() && serializableDeviceType.TagIndex == -1)
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
            m_ToolStacks[device].tools.Push(tool);
            UpdatePlayerHandleMaps();
        }
    }

#if UNITY_EDITOR
    private static EditorVR s_Instance;
	private static InputManager s_InputManager;

	static EditorVR()
	{
		EditorVRView.onEnable += OnEVREnabled;
		EditorVRView.onDisable += OnEVRDisabled;
	}

	private static void OnEVREnabled()
	{
	    InitializeInputManager();
	    s_Instance = U.CreateGameObjectWithComponent<EditorVR>();
	}

    private static void InitializeInputManager()
    {
        // HACK: InputSystem has a static constructor that is relied upon for initializing a bunch of other components, so
        //	in edit mode we need to handle lifecycle explicitly
        InputManager[] managers = Resources.FindObjectsOfTypeAll<InputManager>();
        foreach (var m in managers)
        {
            U.Destroy(m.gameObject);
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
        U.SetRunInEditModeRecursively(s_InputManager.gameObject, true);
    }

    private static void OnEVRDisabled()
	{
		U.Destroy(s_Instance.gameObject);
		U.Destroy(s_InputManager.gameObject);
	}
#endif
}
