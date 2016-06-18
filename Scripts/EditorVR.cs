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
    private VRLineRenderer m_PointerRayPrefab;

    private readonly List<ActionMap> kDefaultActionMaps = new List<ActionMap> ();

    private int m_ToolActionMapInputIndex; // Index to start adding input maps for active tools

    private PlayerHandle m_Handle;

    private Dictionary<Node, Stack<ITool>> m_ToolStacks = new Dictionary<Node, Stack<ITool>>();
    private IEnumerable<Type> m_AllProxies;
    private IEnumerable<Type> m_AllTools;

    void Awake()
    {
        EditorVRView.viewerPivot.parent = transform; // Parent the camera pivot under EditorVR
        EditorVRView.viewerPivot.localPosition = Vector3.zero; // HACK reset pivot to match steam origin
        InitializePlayerHandle();
        CreateDefaultActionMapInputs();
        CreateAllProxies();
        foreach (var node in Enum.GetValues(typeof(Node)))
        {
            m_ToolStacks.Add((Node)node, new Stack<ITool>());
        }
        m_AllTools = U.GetImplementationsOfInterface(typeof(ITool));
        AddToolToStack(Node.Left, typeof(JoystickLocomotionTool));
        AddToolToStack(Node.Right, typeof(JoystickLocomotionTool));
    }

    private void InitializePlayerHandle()
    {
        m_Handle = PlayerHandleManager.GetNewPlayerHandle();
        m_Handle.global = true;
    }

    private void CreateDefaultActionMapInputs()
    {
        kDefaultActionMaps.Add(m_MenuActionMap);
        kDefaultActionMaps.Add(m_TrackedObjectActionMap);
        kDefaultActionMaps.Add(m_DefaultActionMap);

        m_Handle.maps.Add(CreateActionMapInput(m_MenuActionMap));
        m_Handle.maps.Add(CreateActionMapInput(m_TrackedObjectActionMap));
        m_ToolActionMapInputIndex = m_Handle.maps.Count; // Set index where active tool action map inputs will be added
        m_Handle.maps.Add(CreateActionMapInput(m_DefaultActionMap));
    }

    private void CreateAllProxies()
    {
        m_AllProxies = U.GetImplementationsOfInterface(typeof(IProxy));
        foreach (Type proxyType in m_AllProxies)
        {
            IProxy proxy = U.CreateGameObjectWithComponent(proxyType, EditorVRView.viewerPivot) as IProxy;
		    proxy.TrackedObjectInput = m_Handle.GetActions<TrackedObject>();
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
        actionMapInput.TryInitializeWithDevices(m_Handle.GetApplicableDevices());
        actionMapInput.active = true;
        return actionMapInput;
    }

    private void UpdateHandleMaps()
    {
        var maps = m_Handle.maps;
        maps.RemoveRange(m_ToolActionMapInputIndex, maps.Count - kDefaultActionMaps.Count);
        foreach (Stack<ITool> stack in m_ToolStacks.Values)
        {
            foreach (ITool tool in stack.Reverse())
            {
                if (tool.SingleInstance && maps.Contains(tool.ActionMapInput))
                    continue;
                maps.Insert(m_ToolActionMapInputIndex, tool.ActionMapInput);
            }
        }
    }

    private void AddToolToStack(Node node, Type toolType)
    {
        ITool toolComponent = U.AddComponent(toolType, gameObject) as ITool;
        if (toolComponent != null)
        {
            toolComponent.ActionMapInput = CreateActionMapInput(toolComponent.ActionMap);
            ILocomotion locomotionComponent = toolComponent as ILocomotion;
            if (locomotionComponent != null)
            {
                locomotionComponent.ViewerPivot = EditorVRView.viewerPivot;
            }
            m_ToolStacks[node].Push(toolComponent);
            UpdateHandleMaps();
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
