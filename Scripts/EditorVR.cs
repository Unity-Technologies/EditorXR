using UnityEngine;
using System.Collections;
using System;
using UnityEngine.Assertions;
using UnityEngine.InputNew;
using UnityEngine.VR.Proxies;
using System.Collections.Generic;
using System.Linq;
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

    private readonly List<ActionMap> kDefaultActionMaps = new List<ActionMap> ();

    private int m_ToolActionMapInputIndex; // Index to start adding input maps for active tools

    private PlayerHandle m_PlayerHandle;

    private Dictionary<InputDevice, Stack<ITool>> m_ToolStacks = new Dictionary<InputDevice, Stack<ITool>>();
    private List<IProxy> m_AllProxies = new List<IProxy>();
    private IEnumerable<Type> m_AllTools;

	private Dictionary<Type, List<ActionMap>> m_ToolActionMaps;

	private Dictionary<string, Node> m_TagToNode = new Dictionary<string, Node>
	{
		{ "Left", Node.LeftHand },
		{ "Right", Node.RightHand }
	};

    void Awake()
    {
        EditorVRView.viewerPivot.parent = transform; // Parent the camera pivot under EditorVR
        EditorVRView.viewerPivot.localPosition = Vector3.zero; // HACK reset pivot to match steam origin
        InitializePlayerHandle();
        CreateDefaultActionMapInputs();
        CreateAllProxies();
		// TEMP
	    InputDevice leftHand = null;
	    InputDevice rightHand = null;
		foreach (var device in InputSystem.devices)
		{
			if (device.GetType() == typeof(VRInputDevice) && device.TagIndex != -1)
			{
				if (VRInputDevice.Tags[device.TagIndex] == "Left")
					leftHand = device;
				else if (VRInputDevice.Tags[device.TagIndex] == "Right")
					rightHand = device;
			}
			m_ToolStacks.Add(device, new Stack<ITool>());
		}
		m_AllTools = U.GetImplementationsOfInterface(typeof(ITool));
		// TODO: Only show tools in the menu for the input devices in the action map that match the devices present in the system.  This is why we're collecting all the action maps
		//		Additionally, if the action map only has a single hand specified, then only show it in that hand's menu.
		m_ToolActionMaps = CollectToolActionMaps(m_AllTools);

		SpawnTool(typeof(JoystickLocomotionTool));
	    SpawnTool(typeof(MakeCubeTool), rightHand);
		SpawnTool(typeof(MakeSphereTool), leftHand);
	}

	void OnDestroy()
	{
		PlayerHandleManager.RemovePlayerHandle(m_PlayerHandle);
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
        kDefaultActionMaps.Add(m_MenuActionMap);
        kDefaultActionMaps.Add(m_TrackedObjectActionMap);
        kDefaultActionMaps.Add(m_DefaultActionMap);

        m_PlayerHandle.maps.Add(CreateActionMapInput(m_MenuActionMap));
        m_PlayerHandle.maps.Add(CreateActionMapInput(m_TrackedObjectActionMap));
        m_ToolActionMapInputIndex = m_PlayerHandle.maps.Count; // Set index where active tool action map inputs will be added
        m_PlayerHandle.maps.Add(CreateActionMapInput(m_DefaultActionMap));
    }

    private void CreateAllProxies()
    {
        foreach (Type proxyType in U.GetImplementationsOfInterface(typeof(IProxy)))
        {
            IProxy proxy = U.CreateGameObjectWithComponent(proxyType, EditorVRView.viewerPivot) as IProxy;
		    proxy.TrackedObjectInput = m_PlayerHandle.GetActions<TrackedObject>();
            if (!proxy.Active)
            {
                proxy.Hidden = true;
                continue;
            }
            foreach (var rayOriginBase in proxy.RayOrigins)
            {
                var rayTransform = U.InstantiateAndSetActive(m_PointerRayPrefab.gameObject, rayOriginBase.Value).transform;
                rayTransform.position = rayOriginBase.Value.position;
                rayTransform.rotation = rayOriginBase.Value.rotation;
            }
			m_AllProxies.Add(proxy);
        }
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
        maps.RemoveRange(m_ToolActionMapInputIndex, maps.Count - kDefaultActionMaps.Count);
        foreach (Stack<ITool> stack in m_ToolStacks.Values)
        {
            foreach (ITool tool in stack.Reverse())
            {
	            IStandardActionMap standardActionMap = tool as IStandardActionMap;
	            if (standardActionMap != null)
	            {
		            if (!maps.Contains(standardActionMap.StandardInput))
		            {
			            maps.Insert(m_ToolActionMapInputIndex, standardActionMap.StandardInput);
		            }
				}

				ICustomActionMap customActionMap = tool as ICustomActionMap;
	            if (customActionMap != null)
	            {
		            if (!maps.Contains(customActionMap.ActionMapInput))
		            {
						maps.Insert(m_ToolActionMapInputIndex, customActionMap.ActionMapInput);
					}
				}					
            }
        }
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
				actionMap = ScriptableObject.CreateInstance<ActionMap>();
				EditorUtility.CopySerialized(m_StandardToolActionMap, actionMap);
				UpdateActionMapForDevice(actionMap, device);
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
				actionMap = ScriptableObject.CreateInstance<ActionMap>();
				EditorUtility.CopySerialized(customMap.ActionMap, actionMap);
				UpdateActionMapForDevice(actionMap, device);
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

		ILocomotion locomotionComponent = tool as ILocomotion;
        if (locomotionComponent != null)
        {
            locomotionComponent.ViewerPivot = EditorVRView.viewerPivot;
        }

	    foreach (var dev in devices)
	    {
		    AddToolToStack(dev, tool);
	    }
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
            m_ToolStacks[device].Push(tool);
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
