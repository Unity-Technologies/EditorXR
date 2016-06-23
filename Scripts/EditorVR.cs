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
    private IEnumerable<Type> m_AllProxies;
    private IEnumerable<Type> m_AllTools;

	private Dictionary<Type, List<ActionMap>> m_ToolActionMaps;

    void Awake()
    {
        EditorVRView.viewerPivot.parent = transform; // Parent the camera pivot under EditorVR
        EditorVRView.viewerPivot.localPosition = Vector3.zero; // HACK reset pivot to match steam origin
        InitializePlayerHandle();
        CreateDefaultActionMapInputs();
        CreateAllProxies();
		foreach (var device in InputSystem.devices)
		{
			m_ToolStacks.Add(device, new Stack<ITool>());
		}
		m_AllTools = U.GetImplementationsOfInterface(typeof(ITool));
		// TODO: Only show tools in the menu for the input devices in the action map that match the devices present in the system.  This is why we're collecting all the action maps
		m_ToolActionMaps = CollectToolActionMaps(m_AllTools);

		SpawnTool(typeof(JoystickLocomotionTool));
	    SpawnTool(typeof(MakeCubeTool));
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
        m_AllProxies = U.GetImplementationsOfInterface(typeof(IProxy));
        foreach (Type proxyType in m_AllProxies)
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

	private void SpawnTool(Type toolType)
	{
		if (!typeof(ITool).IsAssignableFrom(toolType))
			return;

		ITool tool = U.AddComponent(toolType, gameObject) as ITool;
		IStandardActionMap standardMap = tool as IStandardActionMap;
		if (standardMap != null)
		{
			standardMap.StandardInput = (Standard)CreateActionMapInput(m_StandardToolActionMap);
		}
			
		ICustomActionMap customMap = tool as ICustomActionMap;
		if (customMap != null)
		{
			customMap.ActionMapInput = CreateActionMapInput(customMap.ActionMap);
		}

		var devices = U.CollectInputDevicesFromActionMaps(m_ToolActionMaps[toolType]);

        ILocomotion locomotionComponent = tool as ILocomotion;
        if (locomotionComponent != null)
        {
            locomotionComponent.ViewerPivot = EditorVRView.viewerPivot;
        }

	    foreach (var device in devices)
	    {
		    AddToolToStack(device, tool);
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
