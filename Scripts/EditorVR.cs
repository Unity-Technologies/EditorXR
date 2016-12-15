//#define ENABLE_MINIWORLD_RAY_SELECTION
#if !UNITY_EDITORVR
#pragma warning disable 67, 414, 649
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.Experimental.EditorVR;
using UnityEngine.Experimental.EditorVR.Actions;
using UnityEngine.Experimental.EditorVR.Extensions;
using UnityEngine.Experimental.EditorVR.Helpers;
using UnityEngine.Experimental.EditorVR.Manipulators;
using UnityEngine.Experimental.EditorVR.Menus;
using UnityEngine.Experimental.EditorVR.Modules;
using UnityEngine.Experimental.EditorVR.Proxies;
using UnityEngine.Experimental.EditorVR.Tools;
using UnityEngine.Experimental.EditorVR.UI;
using UnityEngine.Experimental.EditorVR.Utilities;
using UnityEngine.Experimental.EditorVR.Workspaces;
using UnityEngine.InputNew;
using UnityEngine.VR;

namespace UnityEditor.Experimental.EditorVR
{
#if UNITY_EDITOR
	[InitializeOnLoad]
#endif
	[RequiresTag(kVRPlayerTag)]
	public class EditorVR : MonoBehaviour
	{
		delegate void ForEachRayOriginCallback(IProxy proxy, KeyValuePair<Node, Transform> rayOriginPair, InputDevice device, DeviceData deviceData);

		public const HideFlags kDefaultHideFlags = HideFlags.DontSave;
		const string kVRPlayerTag = "VRPlayer";
		const float kDefaultRayLength = 100f;
		const float kPreviewScale = 0.1f;
		const float kViewerPivotTransitionTime = 0.75f;
		const string kNull = "null";
		const byte kMinStencilRef = 2;

		// Minimum time to spend loading the project folder before yielding
		const float kMinProjectFolderLoadTime = 0.005f;

		// Maximum time (in ms) before yielding in CreateFolderData: should be target frame time
		const float kMaxFrameTime = 0.01f;

		static readonly Vector3 kDefaultWorkspaceOffset = new Vector3(0, -0.15f, 0.4f);
		static readonly Quaternion kDefaultWorkspaceTilt = Quaternion.AngleAxis(-20, Vector3.right);

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
		MainMenuActivator m_MainMenuActivatorPrefab;

		[SerializeField]
		private KeyboardMallet m_KeyboardMalletPrefab;

		[SerializeField]
		private KeyboardUI m_NumericKeyboardPrefab;

		[SerializeField]
		private KeyboardUI m_StandardKeyboardPrefab;

		[SerializeField]
		private GameObject m_PlayerModelPrefab;

		[SerializeField]
		GameObject m_PreviewCameraPrefab;

		[SerializeField]
		ProxyExtras m_ProxyExtras;

		[SerializeField]
		PinnedToolButton m_PinnedToolButtonPrefab;

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
		private LockModule m_LockModule;
		private DragAndDropModule m_DragAndDropModule;

		private bool m_UpdatePixelRaycastModule = true;
		bool m_PixelRaycastIgnoreListDirty = true;

		private PlayerHandle m_PlayerHandle;

		class ToolData
		{
			public ITool tool;
			public ActionMapInput input;
		}

		[Flags]
		enum MenuHideFlags
		{
			Hidden = 1 << 0,
			OverActivator = 1 << 1,
			NearWorkspace = 1 << 2,
		}

		class DeviceData
		{
			public readonly Stack<ToolData> toolData = new Stack<ToolData>();
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
			public readonly Dictionary<IMenu, MenuHideFlags> menuHideFlags = new Dictionary<IMenu, MenuHideFlags>();
			public readonly Dictionary<IMenu, float> menuSizes = new Dictionary<IMenu, float>();
		}

		private readonly Dictionary<InputDevice, DeviceData> m_DeviceData = new Dictionary<InputDevice, DeviceData>();
		private readonly List<IProxy> m_Proxies = new List<IProxy>();
		private List<ActionMenuData> m_MenuActions = new List<ActionMenuData>();
		private List<Type> m_AllTools;
		private List<Type> m_AllWorkspaceTypes;
		private List<IAction> m_Actions;
		List<Type> m_MainMenuTools;
		private readonly List<IWorkspace> m_Workspaces = new List<IWorkspace>();

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
		bool m_MiniWorldIgnoreListDirty = true;

		private event Action m_SelectionChanged;
		Transform m_LastSelectionRayOrigin;

		IPreviewCamera m_CustomPreviewCamera;

		StandardManipulator m_StandardManipulator;
		ScaleManipulator m_ScaleManipulator;

		IGrabObject m_ObjectGrabber;

		bool m_ControllersReady;

		readonly List<IVacuumable> m_Vacuumables = new List<IVacuumable>();

		readonly List<IUsesProjectFolderData> m_ProjectFolderLists = new List<IUsesProjectFolderData>();
		List<FolderData> m_FolderData;
		readonly HashSet<string> m_AssetTypes = new HashSet<string>();
		float m_ProjectFolderLoadStartTime;
		float m_ProjectFolderLoadYieldTime;

#if UNITY_EDITOR
		readonly List<IUsesHierarchyData> m_HierarchyLists = new List<IUsesHierarchyData>();
		HierarchyData m_HierarchyData;
		HierarchyProperty m_HierarchyProperty;
#endif

		readonly List<IFilterUI> m_FilterUIs = new List<IFilterUI>();

		readonly HashSet<object> m_ConnectedInterfaces = new HashSet<object>();

		readonly HashSet<InputControl> m_LockedControls = new HashSet<InputControl>();

		byte stencilRef
		{
			get { return m_StencilRef; }
			set
			{
				// Stencil reference range is 0 to 255
				m_StencilRef = (byte)Mathf.Clamp(value, kMinStencilRef, byte.MaxValue);

				// Wrap
				if (m_StencilRef == byte.MaxValue)
					m_StencilRef = kMinStencilRef;
			}
		}

		byte m_StencilRef = kMinStencilRef;

#if UNITY_EDITORVR
		private void Awake()
		{
			ClearDeveloperConsoleIfNecessary();

			UpdateProjectFolders();
			UpdateHierarchyData();

			VRView.viewerPivot.parent = transform; // Parent the camera pivot under EditorVR
			if (VRSettings.loadedDeviceName == "OpenVR")
			{
				// Steam's reference position should be at the feet and not at the head as we do with Oculus
				VRView.viewerPivot.localPosition = Vector3.zero;
			}

			var hmdOnlyLayerMask = 0;
			if (m_PreviewCameraPrefab)
			{
				var go = U.Object.Instantiate(m_PreviewCameraPrefab);
				m_CustomPreviewCamera = go.GetComponentInChildren<IPreviewCamera>();
				if (m_CustomPreviewCamera != null)
				{
					VRView.customPreviewCamera = m_CustomPreviewCamera.previewCamera;
					m_CustomPreviewCamera.vrCamera = VRView.viewerCamera;
					hmdOnlyLayerMask = m_CustomPreviewCamera.hmdOnlyLayerMask;
				}
			}
			VRView.cullingMask = UnityEditor.Tools.visibleLayers | hmdOnlyLayerMask;

			InitializePlayerHandle();
			CreateDefaultActionMapInputs();
			CreateAllProxies();
			CreateDeviceDataForInputDevices();

			m_DragAndDropModule = U.Object.AddComponent<DragAndDropModule>(gameObject);

			CreateEventSystem();

			m_PixelRaycastModule = U.Object.AddComponent<PixelRaycastModule>(gameObject);
			m_PixelRaycastModule.ignoreRoot = transform;
			m_HighlightModule = U.Object.AddComponent<HighlightModule>(gameObject);
			m_LockModule = U.Object.AddComponent<LockModule>(gameObject);
			m_LockModule.updateAlternateMenu = (rayOrigin, o) => SetAlternateMenuVisibility(rayOrigin, o != null);
			ConnectInterfaces(m_LockModule);

			m_AllTools = U.Object.GetImplementationsOfInterface(typeof(ITool)).ToList();
			m_MainMenuTools = m_AllTools.Where(t => !IsPermanentTool(t)).ToList(); // Don't show tools that can't be selected/toggled
			m_AllWorkspaceTypes = U.Object.GetImplementationsOfInterface(typeof(IWorkspace)).ToList();

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
				var result = (bool)hasFlagMethod.Invoke(window, new[] { clearOnPlayFlag });

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

			UpdateAlternateMenuOnSelectionChanged(m_LastSelectionRayOrigin);
		}

		// TODO: Find a better callback for when objects are created or destroyed
		void OnHierarchyChanged()
		{
			m_MiniWorldIgnoreListDirty = true;
			m_PixelRaycastIgnoreListDirty = true;

			UpdateHierarchyData();
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
				m_DeviceData.Add(device, new DeviceData());
			}
		}

		private IEnumerator Start()
		{
			// Delay until at least one proxy initializes
			bool proxyActive = false;
			while (!proxyActive)
			{
				foreach (var proxy in m_Proxies)
				{
					if (proxy.active)
					{
						proxyActive = true;
						break;
					}
				}

				yield return null;
			}

			m_ControllersReady = true;

			if (m_ProxyExtras)
			{
				var extraData = m_ProxyExtras.data;
				ForEachRayOrigin((proxy, pair, device, deviceData) =>
				{
					List<GameObject> prefabs;
					if (extraData.TryGetValue(pair.Key, out prefabs))
					{
						foreach (var prefab in prefabs)
						{
							var go = InstantiateUI(prefab);
							go.transform.SetParent(pair.Value, false);
						}
					}
				});
			}

			CreateSpatialSystem();

			m_ObjectPlacementModule = U.Object.AddComponent<ObjectPlacementModule>(gameObject);
			ConnectInterfaces(m_ObjectPlacementModule);

			SpawnActions();
			SpawnDefaultTools();
			AddPlayerModel();
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
			EditorApplication.hierarchyWindowChanged += OnHierarchyChanged;
			VRView.onGUIDelegate += OnSceneGUI;
			EditorApplication.projectWindowChanged += UpdateProjectFolders;
#endif
		}

		void OnDisable()
		{
			Selection.selectionChanged -= OnSelectionChanged;
#if UNITY_EDITOR
			EditorApplication.hierarchyWindowChanged -= OnHierarchyChanged;
			VRView.onGUIDelegate -= OnSceneGUI;
			EditorApplication.projectWindowChanged -= UpdateProjectFolders;
#endif
		}

		void OnSceneGUI(EditorWindow obj)
		{
			if (Event.current.type == EventType.ExecuteCommand)
			{
				if (m_PixelRaycastIgnoreListDirty)
				{
					m_PixelRaycastModule.UpdateIgnoreList();
					m_PixelRaycastIgnoreListDirty = false;
				}

				ForEachRayOrigin((proxy, pair, device, deviceData) =>
				{
					m_PixelRaycastModule.UpdateRaycast(pair.Value, m_EventCamera);
				});

#if ENABLE_MINIWORLD_RAY_SELECTION
			foreach (var rayOrigin in m_MiniWorldRays.Keys)
				m_PixelRaycastModule.UpdateRaycast(rayOrigin, m_EventCamera);
#endif

				// Queue up the next round
				m_UpdatePixelRaycastModule = true;

				Event.current.Use();
			}
		}

		void OnDestroy()
		{
			if (m_CustomPreviewCamera != null)
				U.Object.Destroy(((MonoBehaviour)m_CustomPreviewCamera).gameObject);

			PlayerHandleManager.RemovePlayerHandle(m_PlayerHandle);
		}

		void PrewarmAssets()
		{
			// HACK: Cannot async load assets in the editor yet, so to avoid a hitch let's spawn the menu immediately and then make it invisible
			foreach (var kvp in m_DeviceData)
			{
				var device = kvp.Key;
				var deviceData = kvp.Value;
				var mainMenu = deviceData.mainMenu;

				if (mainMenu == null)
				{
					mainMenu = SpawnMainMenu(typeof(MainMenu), device, false, out deviceData.mainMenuInput);
					deviceData.mainMenu = mainMenu;
					deviceData.menuHideFlags[mainMenu] = MenuHideFlags.Hidden;
					UpdatePlayerHandleMaps();
				}
			}
		}

		void ConsumeControl(InputControl control)
		{
			// Consuming a control inherently locks it (for now), since consuming a control for one frame only might leave
			// another AMI to pick up a wasPressed the next frame, since it's own input would have been cleared. The
			// control is released when it returns to it's default value
			m_LockedControls.Add(control);

			var ami = control.provider as ActionMapInput;
			foreach (var input in m_PlayerHandle.maps)
			{
				if (input != ami)
					input.ResetControl(control);
			}
		}

		private void Update()
		{
			if (m_CustomPreviewCamera != null)
				m_CustomPreviewCamera.enabled = VRView.showDeviceView && VRView.customPreviewCamera != null;

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

			if (!m_ControllersReady)
				return;

			UpdateDefaultProxyRays();

			UpdateKeyboardMallets();

			ProcessInput();

			UpdateMenuVisibilityNearWorkspaces();
			UpdateMenuVisibilities();
		}

		void UpdateMenuVisibilityNearWorkspaces()
		{
			ForEachRayOrigin((proxy, pair, device, deviceData) =>
			{
				var menus = new List<IMenu>(deviceData.menuHideFlags.Keys);
				foreach (var menu in menus)
				{
					// AE 12/7/16 - Disabling main menu hiding near workspaces for now because it confuses people; Needs improvement
					if (menu is IMainMenu)
						continue;

					var menuSizes = deviceData.menuSizes;
					var menuBounds = U.Object.GetBounds(menu.menuContent);
					var menuBoundsSize = menuBounds.size;

					// Because menus can change size, store the maximum size to avoid ping ponging visibility
					float maxComponent;
					if (!menuSizes.TryGetValue(menu, out maxComponent))
					{
						maxComponent = menuBoundsSize.MaxComponent();
						menuSizes[menu] = maxComponent;
					}

					var menuHideFlags = deviceData.menuHideFlags;
					var flags = menuHideFlags[menu];
					var currentMaxComponent = menuBoundsSize.MaxComponent();
					if (currentMaxComponent > maxComponent && flags == 0)
					{
						maxComponent = currentMaxComponent;
						menuSizes[menu] = currentMaxComponent;
					}

					var intersection = false;

					foreach (var workspace in m_Workspaces)
					{
						var outerBounds = workspace.transform.TransformBounds(workspace.outerBounds);
						if (flags == 0)
							outerBounds.extents -= Vector3.one * maxComponent;

						if (menuBounds.Intersects(outerBounds))
						{
							intersection = true;
							break;
						}
					}

					menuHideFlags[menu] = intersection
						? flags | MenuHideFlags.NearWorkspace
						: flags & ~MenuHideFlags.NearWorkspace;
				}
			});
		}

		void ProcessInput()
		{
			// Maintain a consumed control, so that other AMIs don't pick up the input, until it's no longer used
			var removeList = new List<InputControl>();
			foreach (var lockedControl in m_LockedControls)
			{
				if (Mathf.Approximately(lockedControl.rawValue, lockedControl.provider.GetControlData(lockedControl.index).defaultValue))
					removeList.Add(lockedControl);
				else
					ConsumeControl(lockedControl);
			}

			// Remove separately, since we cannot remove while iterating
			foreach (var inputControl in removeList)
			{
				m_LockedControls.Remove(inputControl);
			}

			UpdateMiniWorlds();

			m_InputModule.ProcessInput(null, ConsumeControl);

			foreach (var deviceData in m_DeviceData.Values)
			{
				var mainMenu = deviceData.mainMenu;
				var menuInput = mainMenu as IProcessInput;
				if (menuInput != null && mainMenu.visible)
					menuInput.ProcessInput(deviceData.mainMenuInput, ConsumeControl);

				var altMenu = deviceData.alternateMenu;
				var altMenuInput = altMenu as IProcessInput;
				if (altMenuInput != null && altMenu.visible)
					altMenuInput.ProcessInput(deviceData.alternateMenuInput, ConsumeControl);

				foreach (var toolData in deviceData.toolData)
				{
					var process = toolData.tool as IProcessInput;
					if (process != null && ((MonoBehaviour)toolData.tool).enabled)
						process.ProcessInput(toolData.input, ConsumeControl);
				}
			}
		}

		void UpdateKeyboardMallets()
		{
			foreach (var proxy in m_Proxies)
			{
				proxy.hidden = !proxy.active;
				if (proxy.active)
				{
					foreach (var rayOrigin in proxy.rayOrigins.Values)
					{
						var malletVisible = true;
						var numericKeyboardNull = false;
						var standardKeyboardNull = false;

						if (m_NumericKeyboard != null)
							malletVisible = m_NumericKeyboard.ShouldShowMallet(rayOrigin);
						else
							numericKeyboardNull = true;

						if (m_StandardKeyboard != null)
							malletVisible = malletVisible || m_StandardKeyboard.ShouldShowMallet(rayOrigin);
						else
							standardKeyboardNull = true;

						if (numericKeyboardNull && standardKeyboardNull)
							malletVisible = false;

						var mallet = m_KeyboardMallets[rayOrigin];

						if (mallet.visible != malletVisible)
						{
							mallet.visible = malletVisible;
							var dpr = rayOrigin.GetComponentInChildren<DefaultProxyRay>();
							if (dpr)
							{
								if (malletVisible)
									dpr.Hide();
								else
									dpr.Show();
							}
						}

						// TODO remove this after physics are in
						mallet.CheckForKeyCollision();
					}
				}
			}
		}

		private void InitializePlayerHandle()
		{
			m_PlayerHandle = PlayerHandleManager.GetNewPlayerHandle();
			m_PlayerHandle.global = true;
			m_PlayerHandle.processAll = true;
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
			return typeof(ITransformer).IsAssignableFrom(type)
				|| typeof(SelectionTool).IsAssignableFrom(type)
				|| typeof(ILocomotor).IsAssignableFrom(type)
				|| typeof(VacuumTool).IsAssignableFrom(type);
		}

		private void SpawnDefaultTools()
		{
			// Spawn default tools
			HashSet<InputDevice> devices;
			ToolData toolData;

			var transformTool = SpawnTool(typeof(TransformTool), out devices);
			m_ObjectGrabber = transformTool.tool as IGrabObject;

			foreach (var deviceDataPair in m_DeviceData)
			{
				var inputDevice = deviceDataPair.Key;
				var deviceData = deviceDataPair.Value;

				// Skip keyboard, mouse, gamepads. Selection, blink, and vacuum tools should only be on left and right hands (tagged 0 and 1)
				if (inputDevice.tagIndex == -1)
					continue;

				toolData = SpawnTool(typeof(SelectionTool), out devices, inputDevice);
				AddToolToDeviceData(toolData, devices);
				var selectionTool = (SelectionTool)toolData.tool;
				selectionTool.selected += SetLastSelectionRayOrigin; // when a selection occurs in the selection tool, call show in the alternate menu, allowing it to show/hide itself.
				selectionTool.hovered += m_LockModule.OnHovered;
				selectionTool.isRayActive = IsRayActive;

				toolData = SpawnTool(typeof(VacuumTool), out devices, inputDevice);
				AddToolToDeviceData(toolData, devices);
				var vacuumTool = (VacuumTool)toolData.tool;
				vacuumTool.defaultOffset = kDefaultWorkspaceOffset;
				vacuumTool.vacuumables = m_Vacuumables;

				// Using a shared instance of the transform tool across all device tool stacks
				AddToolToStack(inputDevice, transformTool);

				toolData = SpawnTool(typeof(BlinkLocomotionTool), out devices, inputDevice);
				AddToolToDeviceData(toolData, devices);

				var mainMenuActivator = SpawnMainMenuActivator(inputDevice);
				deviceData.mainMenuActivator = mainMenuActivator;
				mainMenuActivator.selected += OnMainMenuActivatorSelected;
				mainMenuActivator.hoverStarted += OnMainMenuActivatorHoverStarted;
				mainMenuActivator.hoverEnded += OnMainMenuActivatorHoverEnded;

				var pinnedToolButton = SpawnPinnedToolButton(inputDevice);
				deviceData.previousToolButton = pinnedToolButton;
				var pinnedToolButtonTransform = pinnedToolButton.transform;
				pinnedToolButtonTransform.SetParent(mainMenuActivator.transform, false);
				pinnedToolButtonTransform.localPosition = new Vector3(0f, 0f, -0.035f); // Offset from the main menu activator

				var alternateMenu = SpawnAlternateMenu(typeof(RadialMenu), inputDevice, out deviceData.alternateMenuInput);
				deviceData.alternateMenu = alternateMenu;
				deviceData.menuHideFlags[alternateMenu] = MenuHideFlags.Hidden;
				alternateMenu.itemWasSelected += UpdateAlternateMenuOnSelectionChanged;

				UpdatePlayerHandleMaps();
			}
		}

		void UpdateAlternateMenuForDevice(DeviceData deviceData)
		{
			var alternateMenu = deviceData.alternateMenu;
			alternateMenu.visible = deviceData.menuHideFlags[alternateMenu] == 0 && !(deviceData.currentTool is IExclusiveMode);

			// Move the activator button to an alternate position if the alternate menu will be shown
			var mainMenuActivator = deviceData.mainMenuActivator;
			if (mainMenuActivator != null)
				mainMenuActivator.activatorButtonMoveAway = alternateMenu.visible;
		}

		void UpdateRayForDevice(DeviceData deviceData, Transform rayOrigin)
		{
			var mainMenu = deviceData.mainMenu;
			var customMenu = deviceData.customMenu;
			if (mainMenu.visible || (customMenu != null && customMenu.visible))
			{
				HideRay(rayOrigin);
				LockRay(rayOrigin, mainMenu);
			} else
			{
				UnlockRay(rayOrigin, mainMenu);
				ShowRay(rayOrigin);
			}
		}

		void UpdateMenuVisibilities()
		{
			var deviceDatas = new List<DeviceData>();
			ForEachRayOrigin((proxy, pair, device, deviceData) =>
			{
				deviceDatas.Add(deviceData);
			});

			// Reconcile conflicts because menus on the same device can visually overlay each other
			foreach (var deviceData in deviceDatas)
			{
				var alternateMenu = deviceData.alternateMenu;
				var mainMenu = deviceData.mainMenu;
				var customMenu = deviceData.customMenu;
				var menuHideFlags = deviceData.menuHideFlags;

				// Move alternate menu to another device if it conflicts with main or custom menu
				if (alternateMenu != null && (menuHideFlags[mainMenu] == 0 || (customMenu != null && menuHideFlags[customMenu] == 0)) && menuHideFlags[alternateMenu] == 0)
				{
					foreach (var otherDeviceData in deviceDatas)
					{
						if (otherDeviceData == deviceData)
							continue;

						var otherCustomMenu = otherDeviceData.customMenu;
						var otherHideFlags = otherDeviceData.menuHideFlags;
						otherHideFlags[otherDeviceData.alternateMenu] &= ~MenuHideFlags.Hidden;
						otherHideFlags[otherDeviceData.mainMenu] |= MenuHideFlags.Hidden;

						if (otherCustomMenu != null)
							otherHideFlags[otherCustomMenu] |= MenuHideFlags.Hidden;
					}

					menuHideFlags[alternateMenu] |= MenuHideFlags.Hidden;
				}

				if (customMenu != null && menuHideFlags[mainMenu] == 0 && menuHideFlags[customMenu] == 0)
				{
					menuHideFlags[customMenu] |= MenuHideFlags.Hidden;
				}
			}

			// Apply state to UI visibility
			ForEachRayOrigin((proxy, rayOriginPair, device, deviceData) =>
			{
				var mainMenu = deviceData.mainMenu;
				mainMenu.visible = deviceData.menuHideFlags[mainMenu] == 0;

				var customMenu = deviceData.customMenu;
				if (customMenu != null)
					customMenu.visible = deviceData.menuHideFlags[customMenu] == 0;

				UpdateAlternateMenuForDevice(deviceData);
				UpdateRayForDevice(deviceData, rayOriginPair.Value);
			});

			UpdatePlayerHandleMaps();
		}

		void OnMainMenuActivatorHoverStarted(Transform rayOrigin)
		{
			ForEachRayOrigin((p, rayOriginPair, device, deviceData) =>
			{
				if (rayOriginPair.Value == rayOrigin)
				{
					var menus = new List<IMenu>(deviceData.menuHideFlags.Keys);
					foreach (var menu in menus)
					{
						deviceData.menuHideFlags[menu] |= MenuHideFlags.OverActivator;
					}
				}
			});
		}

		void OnMainMenuActivatorHoverEnded(Transform rayOrigin)
		{
			ForEachRayOrigin((p, rayOriginPair, device, deviceData) =>
			{
				if (rayOriginPair.Value == rayOrigin)
				{
					var menus = new List<IMenu>(deviceData.menuHideFlags.Keys);
					foreach (var menu in menus)
					{
						deviceData.menuHideFlags[menu] &= ~MenuHideFlags.OverActivator;
					}
				}
			});
		}

		void SetLastSelectionRayOrigin(Transform rayOrigin)
		{
			m_LastSelectionRayOrigin = rayOrigin;
		}

		void UpdateAlternateMenuOnSelectionChanged(Transform rayOrigin)
		{
			SetAlternateMenuVisibility(rayOrigin, Selection.gameObjects.Length > 0);
		}

		void SetAlternateMenuVisibility(Transform rayOrigin, bool visible)
		{
			ForEachRayOrigin((proxy, rayOriginPair, rayOriginDevice, deviceData) =>
			{
				var alternateMenu = deviceData.alternateMenu;
				if (alternateMenu != null)
				{
					var flags = deviceData.menuHideFlags[alternateMenu];
					deviceData.menuHideFlags[alternateMenu] = (rayOriginPair.Value == rayOrigin) && visible
						? flags & ~MenuHideFlags.Hidden
						: flags | MenuHideFlags.Hidden;
				}
			});
		}

		void OnMainMenuActivatorSelected(Transform rayOrigin, Transform targetRayOrigin)
		{
			ForEachRayOrigin((proxy, rayOriginPair, rayOriginDevice, deviceData) =>
			{
				if (rayOriginPair.Value == rayOrigin)
				{
					var mainMenu = deviceData.mainMenu;
					if (mainMenu != null)
					{
						var menuHideFlags = deviceData.menuHideFlags;
						menuHideFlags[mainMenu] ^= MenuHideFlags.Hidden;

						var customMenu = deviceData.customMenu;
						if (customMenu != null)
							menuHideFlags[customMenu] &= ~MenuHideFlags.Hidden;

						mainMenu.targetRayOrigin = targetRayOrigin;
					}
				}
			});
		}

		private void SpawnActions()
		{
			IEnumerable<Type> actionTypes = U.Object.GetImplementationsOfInterface(typeof(IAction));
			m_Actions = new List<IAction>();
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

				m_Actions.Add(action);
			}

			m_MenuActions.Sort((x, y) => y.priority.CompareTo(x.priority));
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

				m_Proxies.Add(proxy);
			}
		}

		private void UpdateDefaultProxyRays()
		{
			// Set ray lengths based on renderer bounds
			foreach (var proxy in m_Proxies)
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
					} else
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

			if (m_CustomPreviewCamera != null)
				m_InputModule.layerMask |= m_CustomPreviewCamera.hmdOnlyLayerMask;

			m_EventCamera = U.Object.Instantiate(m_EventCameraPrefab.gameObject, transform).GetComponent<Camera>();
			m_EventCamera.enabled = false;
			m_InputModule.eventCamera = m_EventCamera;

			m_InputModule.rayEntered += m_DragAndDropModule.OnRayEntered;
			m_InputModule.rayExited += m_DragAndDropModule.OnRayExited;
			m_InputModule.dragStarted += m_DragAndDropModule.OnDragStarted;
			m_InputModule.dragEnded += m_DragAndDropModule.OnDragEnded;

			m_InputModule.preProcessRaycastSource = PreProcessRaycastSource;

			ForEachRayOrigin((proxy, rayOriginPair, device, deviceData) =>
			{
				// Create ui action map input for device.
				if (deviceData.uiInput == null)
				{
					deviceData.uiInput = CreateActionMapInput(m_InputModule.actionMap, device);
					deviceData.directSelectInput = CreateActionMapInput(m_DirectSelectActionMap, device);
				}

				// Add RayOrigin transform, proxy and ActionMapInput references to input module list of sources
				m_InputModule.AddRaycastSource(proxy, rayOriginPair.Key, deviceData.uiInput, rayOriginPair.Value, source =>
				{
					foreach (var miniWorld in m_MiniWorlds)
					{
						var targetObject = source.hoveredObject
							? source.hoveredObject
							: source.draggedObject;
						if (miniWorld.Contains(source.rayOrigin.position))
						{
							if (targetObject && !targetObject.transform.IsChildOf(miniWorld.miniWorldTransform.parent))
								return false;
						}
					}

					return true;
				});
			}, false);
		}

		void ForEachRayOrigin(ForEachRayOriginCallback callback, bool activeOnly = true)
		{
			foreach (var proxy in m_Proxies)
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
			}, false);
		}

		GameObject InstantiateUI(GameObject prefab, Transform parent = null, bool worldPositionStays = true)
		{
			var go = U.Object.Instantiate(prefab);
			go.transform.SetParent(parent
				? parent
				: transform, worldPositionStays);
			foreach (var canvas in go.GetComponentsInChildren<Canvas>())
				canvas.worldCamera = m_EventCamera;

			foreach (var inputField in go.GetComponentsInChildren<InputField>())
			{
				if (inputField is NumericInputField)
					inputField.spawnKeyboard = SpawnNumericKeyboard;
				else if (inputField is StandardInputField)
					inputField.spawnKeyboard = SpawnAlphaNumericKeyboard;
			}

			foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true))
				ConnectInterfaces(mb);

			return go;
		}

		private GameObject InstantiateMenuUI(Transform rayOrigin, IMenu prefab)
		{
			GameObject go = null;
			ForEachRayOrigin((proxy, rayOriginPair, device, deviceData) =>
			{
				if (proxy.rayOrigins.ContainsValue(rayOrigin) && rayOriginPair.Value != rayOrigin)
				{
					var otherRayOrigin = rayOriginPair.Value;
					Transform menuOrigin;
					if (proxy.menuOrigins.TryGetValue(otherRayOrigin, out menuOrigin))
					{
						if (deviceData.customMenu == null)
						{
							go = InstantiateUI(prefab.gameObject);

							go.transform.SetParent(menuOrigin);
							go.transform.localPosition = Vector3.zero;
							go.transform.localRotation = Quaternion.identity;

							var customMenu = go.GetComponent<IMenu>();
							deviceData.customMenu = customMenu;
							deviceData.menuHideFlags[customMenu] = 0;
						}
					}
				}
			});

			return go;
		}

		private KeyboardUI SpawnNumericKeyboard()
		{
			if (m_StandardKeyboard != null)
				m_StandardKeyboard.gameObject.SetActive(false);

			// Check if the prefab has already been instantiated
			if (m_NumericKeyboard == null)
				m_NumericKeyboard = U.Object.Instantiate(m_NumericKeyboardPrefab.gameObject, U.Camera.GetViewerPivot()).GetComponent<KeyboardUI>();

			return m_NumericKeyboard;
		}

		private KeyboardUI SpawnAlphaNumericKeyboard()
		{
			if (m_NumericKeyboard != null)
				m_NumericKeyboard.gameObject.SetActive(false);

			// Check if the prefab has already been instantiated
			if (m_StandardKeyboard == null)
				m_StandardKeyboard = U.Object.Instantiate(m_StandardKeyboardPrefab.gameObject, U.Camera.GetViewerPivot()).GetComponent<KeyboardUI>();

			return m_StandardKeyboard;
		}

		private ActionMapInput CreateActionMapInput(ActionMap map, InputDevice device)
		{
			// Check for improper use of action maps first
			if (device != null && !IsValidActionMapForDevice(map, device))
				return null;

			var devices = device == null
				? GetSystemDevices()
				: new[] { device };

			var actionMapInput = ActionMapInput.Create(map);

			// It's possible that there are no suitable control schemes for the device that is being initialized,
			// so ActionMapInput can't be marked active
			var successfulInitialization = false;
			if (actionMapInput.TryInitializeWithDevices(devices))
			{
				successfulInitialization = true;
			} else
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

			foreach (var deviceData in m_DeviceData.Values)
			{
				var mainMenu = deviceData.mainMenu;
				var mainMenuInput = deviceData.mainMenuInput;
				if (mainMenu != null && mainMenuInput != null)
				{
					mainMenuInput.active = mainMenu.visible;

					if (!maps.Contains(mainMenuInput))
						maps.Add(mainMenuInput);
				}

				var alternateMenu = deviceData.alternateMenu;
				var alternateMenuInput = deviceData.alternateMenuInput;
				if (alternateMenu != null && alternateMenuInput != null)
				{
					alternateMenuInput.active = alternateMenu.visible;

					if (!maps.Contains(alternateMenuInput))
						maps.Add(alternateMenuInput);
				}

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

			foreach (var deviceData in m_DeviceData.Values)
			{
				foreach (var td in deviceData.toolData)
				{
					if (td.input != null && !maps.Contains(td.input))
						maps.Add(td.input);
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
		private ToolData SpawnTool(Type toolType, out HashSet<InputDevice> usedDevices, InputDevice device = null)
		{
			usedDevices = new HashSet<InputDevice>();
			if (!typeof(ITool).IsAssignableFrom(toolType))
				return null;

			var deviceSlots = new HashSet<DeviceSlot>();
			var tool = U.Object.AddComponent(toolType, gameObject) as ITool;

			var actionMapInput = CreateActionMapForObject(tool, device);
			if (actionMapInput != null)
			{
				usedDevices.UnionWith(actionMapInput.GetCurrentlyUsedDevices());
				U.Input.CollectDeviceSlotsFromActionMapInput(actionMapInput, ref deviceSlots);
			}

			ConnectInterfaces(tool, device);

			return new ToolData { tool = tool, input = actionMapInput };
		}

		private void AddToolToDeviceData(ToolData toolData, HashSet<InputDevice> devices)
		{
			foreach (var dev in devices)
				AddToolToStack(dev, toolData);
		}

		private IMainMenu SpawnMainMenu(Type type, InputDevice device, bool visible, out ActionMapInput input)
		{
			input = null;

			if (!typeof(IMainMenu).IsAssignableFrom(type))
				return null;

			var mainMenu = U.Object.AddComponent(type, gameObject) as IMainMenu;
			input = CreateActionMapForObject(mainMenu, device);
			ConnectInterfaces(mainMenu, device);
			mainMenu.visible = visible;

			return mainMenu;
		}

		private IAlternateMenu SpawnAlternateMenu(Type type, InputDevice device, out ActionMapInput input)
		{
			input = null;

			if (!typeof(IAlternateMenu).IsAssignableFrom(type))
				return null;

			var alternateMenu = U.Object.AddComponent(type, gameObject) as IAlternateMenu;
			input = CreateActionMapForObject(alternateMenu, device);
			ConnectInterfaces(alternateMenu, device);
			alternateMenu.visible = false;

			return alternateMenu;
		}

		private MainMenuActivator SpawnMainMenuActivator(InputDevice device)
		{
			var mainMenuActivator = U.Object.Instantiate(m_MainMenuActivatorPrefab.gameObject).GetComponent<MainMenuActivator>();
			ConnectInterfaces(mainMenuActivator, device);

			return mainMenuActivator;
		}

		PinnedToolButton SpawnPinnedToolButton(InputDevice device)
		{
			var button = U.Object.Instantiate(m_PinnedToolButtonPrefab.gameObject).GetComponent<PinnedToolButton>();
			ConnectInterfaces(button, device);

			return button;
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

		private ActionMapInput CreateActionMapForObject(object obj, InputDevice device)
		{
			var customMap = obj as ICustomActionMap;
			if (customMap != null)
			{
				if (customMap is IStandardActionMap)
					Debug.LogWarning("Cannot use IStandardActionMap and ICustomActionMap together in " + obj.GetType());

				return CreateActionMapInput(customMap.actionMap, device);
			}

			var standardMap = obj as IStandardActionMap;
			if (standardMap != null)
				return CreateActionMapInput(m_StandardToolActionMap, device);

			return null;
		}

		void ConnectInterfaces(object obj, InputDevice device)
		{
			Transform rayOrigin = null;
			ForEachRayOrigin((proxy, rayOriginPair, rayOriginDevice, deviceData) =>
			{
				if (rayOriginDevice == device)
					rayOrigin = rayOriginPair.Value;
			});

			ConnectInterfaces(obj, rayOrigin);
		}

		void ConnectInterfaces(object obj, Transform rayOrigin = null)
		{
			if (!m_ConnectedInterfaces.Add(obj))
				return;

			var connectInterfaces = obj as IConnectInterfaces;
			if (connectInterfaces != null)
				connectInterfaces.connectInterfaces = ConnectInterfaces;

			if (rayOrigin)
			{
				var ray = obj as IUsesRayOrigin;
				if (ray != null)
					ray.rayOrigin = rayOrigin;

				var usesProxy = obj as IUsesProxyType;
				if (usesProxy != null)
				{
					ForEachRayOrigin((proxy, rayOriginPair, device, deviceData) =>
					{
						if (rayOrigin == rayOriginPair.Value)
							usesProxy.proxyType = proxy.GetType();
					});
				}

				var menuOrigins = obj as IMenuOrigins;
				if (menuOrigins != null)
				{
					Transform mainMenuOrigin;
					var proxy = GetProxyForRayOrigin(rayOrigin);
					if (proxy != null && proxy.menuOrigins.TryGetValue(rayOrigin, out mainMenuOrigin))
					{
						menuOrigins.menuOrigin = mainMenuOrigin;
						Transform alternateMenuOrigin;
						if (proxy.alternateMenuOrigins.TryGetValue(rayOrigin, out alternateMenuOrigin))
							menuOrigins.alternateMenuOrigin = alternateMenuOrigin;
					}
				}
			}

			// Specific proxy ray setting
			var customRay = obj as ICustomRay;
			if (customRay != null)
			{
				customRay.showDefaultRay = ShowRay;
				customRay.hideDefaultRay = HideRay;
			}

			var lockableRay = obj as IRayLocking;
			if (lockableRay != null)
			{
				lockableRay.lockRay = LockRay;
				lockableRay.unlockRay = UnlockRay;
			}

			var locomotion = obj as ILocomotor;
			if (locomotion != null)
				locomotion.viewerPivot = VRView.viewerPivot;

			var instantiateUI = obj as IInstantiateUI;
			if (instantiateUI != null)
				instantiateUI.instantiateUI = InstantiateUI;

			var createWorkspace = obj as ICreateWorkspace;
			if (createWorkspace != null)
				createWorkspace.createWorkspace = CreateWorkspace;

			var instantiateMenuUI = obj as IInstantiateMenuUI;
			if (instantiateMenuUI != null)
				instantiateMenuUI.instantiateMenuUI = InstantiateMenuUI;

			var raycaster = obj as IUsesRaycastResults;
			if (raycaster != null)
				raycaster.getFirstGameObject = GetFirstGameObject;

			var highlight = obj as ISetHighlight;
			if (highlight != null)
				highlight.setHighlight = m_HighlightModule.SetHighlight;

			var placeObjects = obj as IPlaceObject;
			if (placeObjects != null)
				placeObjects.placeObject = PlaceObject;

			var locking = obj as IGameObjectLocking;
			if (locking != null)
			{
				locking.setLocked = m_LockModule.SetLocked;
				locking.isLocked = m_LockModule.IsLocked;
			}

			var positionPreview = obj as IGetPreviewOrigin;
			if (positionPreview != null)
				positionPreview.getPreviewOriginForRayOrigin = GetPreviewOriginForRayOrigin;

			var selectionChanged = obj as ISelectionChanged;
			if (selectionChanged != null)
				m_SelectionChanged += selectionChanged.OnSelectionChanged;

			var toolActions = obj as IActions;
			if (toolActions != null)
			{
				var actions = toolActions.actions;
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

			var grabObjects = obj as IGrabObject;
			if (grabObjects != null)
			{
				grabObjects.canGrabObject = CanGrabObject;
				grabObjects.grabObject = GrabObject;
				grabObjects.dropObject = DropObject;
			}

			var spatialHash = obj as IUsesSpatialHash;
			if (spatialHash != null)
			{
				spatialHash.addToSpatialHash = m_SpatialHashModule.AddObject;
				spatialHash.removeFromSpatialHash = m_SpatialHashModule.RemoveObject;
			}

			var deleteSceneObjects = obj as IDeleteSceneObject;
			if (deleteSceneObjects != null)
				deleteSceneObjects.deleteSceneObject = DeleteSceneObject;

			var usesViewerBody = obj as IUsesViewerBody;
			if (usesViewerBody != null)
				usesViewerBody.isOverShoulder = IsOverShoulder;

			var mainMenu = obj as IMainMenu;
			if (mainMenu != null)
			{
				mainMenu.menuTools = m_MainMenuTools;
				mainMenu.menuWorkspaces = m_AllWorkspaceTypes.ToList();
				mainMenu.isToolActive = IsToolActive;
			}

			var alternateMenu = obj as IAlternateMenu;
			if (alternateMenu != null)
				alternateMenu.menuActions = m_MenuActions;

			var projectFolderList = obj as IUsesProjectFolderData;
			if (projectFolderList != null)
			{
				projectFolderList.folderData = GetFolderData();
				m_ProjectFolderLists.Add(projectFolderList);
			}

			var hierarchyList = obj as IUsesHierarchyData;
			if (hierarchyList != null)
			{
				hierarchyList.hierarchyData = GetHierarchyData();
				m_HierarchyLists.Add(hierarchyList);
			}

			var filterUI = obj as IFilterUI;
			if (filterUI != null)
			{
				filterUI.filterList = GetFilterList();
				m_FilterUIs.Add(filterUI);
			}

			// Tracked Object action maps shouldn't block each other so we share an instance
			var trackedObjectMap = obj as ITrackedObjectActionMap;
			if (trackedObjectMap != null)
				trackedObjectMap.trackedObjectInput = m_TrackedObjectInput;

			var selectTool = obj as ISelectTool;
			if (selectTool != null)
				selectTool.selectTool = SelectTool;

			var usesViewerPivot = obj as IUsesViewerPivot;
			if (usesViewerPivot != null)
				usesViewerPivot.viewerPivot = U.Camera.GetViewerPivot();

			var usesStencilRef = obj as IUsesStencilRef;
			if (usesStencilRef != null)
			{
				byte? stencilRef = null;

				var mb = obj as MonoBehaviour;
				if (mb)
				{
					var parent = mb.transform.parent;
					if (parent)
					{
						// For workspaces and tools, it's likely that the stencil ref should be shared internally
						var parentStencilRef = parent.GetComponentInParent<IUsesStencilRef>();
						if (parentStencilRef != null)
							stencilRef = parentStencilRef.stencilRef;
					}
				}

				usesStencilRef.stencilRef = stencilRef ?? this.stencilRef++;
			}
		}

		private void DisconnectInterfaces(object obj)
		{
			m_ConnectedInterfaces.Remove(obj);

			var selectionChanged = obj as ISelectionChanged;
			if (selectionChanged != null)
				m_SelectionChanged -= selectionChanged.OnSelectionChanged;

			var toolActions = obj as IActions;
			if (toolActions != null)
			{
				var actions = toolActions.actions;
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

		IProxy GetProxyForRayOrigin(Transform rayOrigin)
		{
			IProxy result = null;
			ForEachRayOrigin((proxy, rayOriginPair, device, deviceData) =>
			{
				if (rayOriginPair.Value == rayOrigin)
					result = proxy;
			});

			return result;
		}

		bool IsToolActive(Transform targetRayOrigin, Type toolType)
		{
			var result = false;

			ForEachRayOrigin((proxy, rayOriginPair, device, deviceData) =>
			{
				if (rayOriginPair.Value == targetRayOrigin)
					result = deviceData.currentTool.GetType() == toolType;
			});

			return result;
		}

		private bool SelectTool(Transform rayOrigin, Type toolType)
		{
			var result = false;
			ForEachRayOrigin((proxy, rayOriginPair, device, deviceData) =>
			{
				if (rayOriginPair.Value == rayOrigin)
				{
					var spawnTool = true;

					// If this tool was on the current device already, then simply remove it
					if (deviceData.currentTool != null && deviceData.currentTool.GetType() == toolType)
					{
						DespawnTool(deviceData, deviceData.currentTool);

						// Don't spawn a new tool, since we are only removing the old tool
						spawnTool = false;
					}

					if (spawnTool)
					{
						// Spawn tool and collect all devices that this tool will need
						HashSet<InputDevice> usedDevices;
						var newTool = SpawnTool(toolType, out usedDevices, device);

						// It's possible this tool uses no action maps, so at least include the device this tool was spawned on
						if (usedDevices.Count == 0)
							usedDevices.Add(device);

						// Exclusive mode tools always take over all tool stacks
						if (newTool is IExclusiveMode)
						{
							foreach (var dev in m_DeviceData.Keys)
							{
								usedDevices.Add(dev);
							}
						}

						foreach (var dev in usedDevices)
						{
							deviceData = m_DeviceData[dev];
							if (deviceData.currentTool != null) // Remove the current tool on all devices this tool will be spawned on
								DespawnTool(deviceData, deviceData.currentTool);

							AddToolToStack(dev, newTool);

							deviceData.previousToolButton.toolType = toolType; // assign the new current tool type to the active tool button
							deviceData.previousToolButton.rayOrigin = rayOrigin;
						}
					}

					UpdatePlayerHandleMaps();
					result = spawnTool;
				} else
				{
					deviceData.menuHideFlags[deviceData.mainMenu] |= MenuHideFlags.Hidden;
				}
			});

			return result;
		}

		private void DespawnTool(DeviceData deviceData, ITool tool)
		{
			if (!IsPermanentTool(tool.GetType()))
			{
				// Remove the tool if it is the current tool on this device tool stack
				if (deviceData.currentTool == tool)
				{
					var topTool = deviceData.toolData.Peek();
					if (topTool == null || topTool.tool != deviceData.currentTool)
					{
						Debug.LogError("Tool at top of stack is not current tool.");
						return;
					}

					deviceData.toolData.Pop();
					topTool = deviceData.toolData.Peek();
					deviceData.currentTool = topTool.tool;

					// Pop this tool of any other stack that references it (for single instance tools)
					foreach (var otherDeviceData in m_DeviceData.Values)
					{
						if (otherDeviceData != deviceData)
						{
							if (otherDeviceData.currentTool == tool)
							{
								otherDeviceData.toolData.Pop();
								var otherToolData = otherDeviceData.toolData.Peek();
								if (otherToolData != null)
									otherDeviceData.currentTool = otherToolData.tool;

								if (tool is IExclusiveMode)
									SetToolsEnabled(otherDeviceData, true);
							}

							// If the tool had a custom menu, the custom menu would spawn on the opposite device
							var customMenu = otherDeviceData.customMenu;
							if (customMenu != null)
							{
								otherDeviceData.menuHideFlags.Remove(customMenu);
								otherDeviceData.customMenu = null;
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
			foreach (var td in deviceData.toolData)
			{
				var mb = td.tool as MonoBehaviour;
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
					} else
					{
						untaggedDevicesFound++;
					}
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

		private void AddToolToStack(InputDevice device, ToolData toolData)
		{
			if (toolData != null)
			{
				var deviceData = m_DeviceData[device];

				// Exclusive tools render other tools disabled while they are on the stack
				if (toolData.tool is IExclusiveMode)
					SetToolsEnabled(deviceData, false);

				deviceData.toolData.Push(toolData);
				deviceData.currentTool = toolData.tool;
			}
		}

		void CreateWorkspace(Type t, Action<IWorkspace> createdCallback = null)
		{
			var cameraTransform = U.Camera.GetMainCamera().transform;

			var workspace = (IWorkspace)U.Object.CreateGameObjectWithComponent(t, U.Camera.GetViewerPivot());
			m_Workspaces.Add(workspace);
			workspace.destroyed += OnWorkspaceDestroyed;
			ConnectInterfaces(workspace);

			//Explicit setup call (instead of setting up in Awake) because we need interfaces to be hooked up first
			workspace.Setup();

			var offset = kDefaultWorkspaceOffset;
			offset.z += workspace.vacuumBounds.extents.z;

			var workspaceTransform = workspace.transform;
			workspaceTransform.position = cameraTransform.TransformPoint(offset);
			workspaceTransform.rotation *= Quaternion.LookRotation(cameraTransform.forward) * kDefaultWorkspaceTilt;

			m_Vacuumables.Add(workspace);

			if (createdCallback != null)
				createdCallback(workspace);

			// MiniWorld is a special case that we handle due to all of the mini world interactions
			var miniWorldWorkspace = workspace as MiniWorldWorkspace;
			if (!miniWorldWorkspace)
				return;

			var miniWorld = miniWorldWorkspace.miniWorld;
			m_MiniWorlds.Add(miniWorld);

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

// Use the mini world ray origin instead of the original ray origin
			m_InputModule.AddRaycastSource(proxy, rayOriginPair.Key, uiInput, miniWorldRayOrigin, (source) =>
			{
				if (!IsRayActive(source.rayOrigin))
					return false;

				if (source.hoveredObject)
					return !m_Workspaces.Any(w => source.hoveredObject.transform.IsChildOf(w.transform));

				return true;
			});
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
			}, false);

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

		private void OnWorkspaceDestroyed(IWorkspace workspace)
		{
			m_Workspaces.Remove(workspace);
			m_Vacuumables.Remove(workspace);

			DisconnectInterfaces(workspace);

			var projectFolderList = workspace as IUsesProjectFolderData;
			if (projectFolderList != null)
				m_ProjectFolderLists.Remove(projectFolderList);

			var filterUI = workspace as IFilterUI;
			if (filterUI != null)
				m_FilterUIs.Remove(filterUI);

			var miniWorldWorkspace = workspace as MiniWorldWorkspace;
			if (miniWorldWorkspace != null)
			{
				var miniWorld = miniWorldWorkspace.miniWorld;

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
					m_InputModule.RemoveRaycastSource(rayOrigin);
#endif
						maps.Remove(miniWorldRay.directSelectInput);
						m_MiniWorldRays.Remove(rayOrigin);
					}
				}
			}
		}

		void UpdateMiniWorldIgnoreList()
		{
			var renderers = new List<Renderer>(GetComponentsInChildren<Renderer>(true));
			var ignoreList = new List<Renderer>(renderers.Count);

			foreach (var r in renderers)
			{
				if (r.CompareTag(kVRPlayerTag))
					continue;

				if (r.gameObject.layer != LayerMask.NameToLayer("UI") && r.CompareTag(MiniWorldRenderer.kShowInMiniWorldTag))
					continue;

				ignoreList.Add(r);
			}

			foreach (var miniWorld in m_MiniWorlds)
			{
				miniWorld.ignoreList = ignoreList;
			}
		}

		private void UpdateMiniWorlds()
		{
			if (m_MiniWorldIgnoreListDirty)
			{
				UpdateMiniWorldIgnoreList();
				m_MiniWorldIgnoreListDirty = false;
			}

			var directSelection = m_ObjectGrabber;

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
				miniWorldRayOrigin.gameObject.SetActive(isContained);

				if (isContained && !miniWorldRay.wasContained)
				{
					HideRay(originalRayOrigin, true);
					LockRay(originalRayOrigin, this);
				}

				if (!isContained && miniWorldRay.wasContained)
				{
					UnlockRay(originalRayOrigin, this);
					ShowRay(originalRayOrigin, true);
				}

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
							var totalBounds = U.Object.GetBounds(dragObject);
							var maxSizeComponent = totalBounds.size.MaxComponent();
							if (!Mathf.Approximately(maxSizeComponent, 0f))
								miniWorldRay.dragObjectPreviewScale = dragObject.transform.localScale * (kPreviewScale / maxSizeComponent);
						}

						ConsumeControl(directSelectInput.select);
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
					} else
					{
						if (dragObjectTransform.CompareTag(kVRPlayerTag))
						{
							if (directSelection != null)
								directSelection.DropHeldObject(dragObjectTransform.transform);

							// Drop player at edge of MiniWorld
							miniWorldRay.dragObject = null;
						} else
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

							var previewOrigin = GetPreviewOriginForRayOrigin(originalRayOrigin);
							U.Math.LerpTransform(dragObjectTransform, previewOrigin.position, previewOrigin.rotation);
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
					{
						if (IsOverShoulder(originalRayOrigin))
							DeleteSceneObject(dragObjectTransform.gameObject);
						else
							PlaceObject(dragObjectTransform, miniWorldRay.dragObjectOriginalScale);
					}

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
				if (renderer && !renderer.gameObject.CompareTag(kVRPlayerTag))
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

		Dictionary<Transform, DirectSelectionData> GetDirectSelection()
		{
			var results = new Dictionary<Transform, DirectSelectionData>();

			ForEachRayOrigin((proxy, rayOriginPair, device, deviceData) =>
			{
				var rayOrigin = rayOriginPair.Value;
				var obj = GetDirectSelectionForRayOrigin(rayOrigin, deviceData.directSelectInput);
				if (obj && !obj.CompareTag(kVRPlayerTag))
				{
					results[rayOrigin] = new DirectSelectionData
					{
						gameObject = obj,
						node = rayOriginPair.Key,
						input = deviceData.directSelectInput
					};
				}
			});

			foreach (var ray in m_MiniWorldRays)
			{
				var rayOrigin = ray.Key;
				var miniWorldRay = ray.Value;
				var go = GetDirectSelectionForRayOrigin(rayOrigin, miniWorldRay.directSelectInput);
				if (go != null)
				{
					results[rayOrigin] = new DirectSelectionData
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
			var directSelection = m_ObjectGrabber;

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

		bool CanGrabObject(DirectSelectionData selection, Transform rayOrigin)
		{
			if (selection.gameObject.CompareTag(kVRPlayerTag) && !m_MiniWorldRays.ContainsKey(rayOrigin))
				return false;

			return true;
		}

		bool GrabObject(IGrabObject grabber, DirectSelectionData selection, Transform rayOrigin)
		{
			if (!CanGrabObject(selection, rayOrigin))
				return false;

			// Detach the player head model so that it is not affected by its parent transform
			if (selection.gameObject.CompareTag(kVRPlayerTag))
				selection.gameObject.transform.parent = null;

			return true;
		}

		void DropObject(IGrabObject grabber, Transform grabbedObject, Transform rayOrigin)
		{
			// Dropping the player head updates the viewer pivot
			if (grabbedObject.CompareTag(kVRPlayerTag))
				StartCoroutine(UpdateViewerPivot(grabbedObject));
			else if (IsOverShoulder(rayOrigin) && !m_MiniWorldRays.ContainsKey(rayOrigin))
				DeleteSceneObject(grabbedObject.gameObject);
		}

		IEnumerator UpdateViewerPivot(Transform playerHead)
		{
			var viewerPivot = U.Camera.GetViewerPivot();

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

		Transform GetPreviewOriginForRayOrigin(Transform rayOrigin)
		{
			foreach (var proxy in m_Proxies)
			{
				Transform previewOrigin;
				if (proxy.previewOrigins.TryGetValue(rayOrigin, out previewOrigin))
					return previewOrigin;
			}

			return null;
		}

		bool IsRayActive(Transform rayOrigin)
		{
			var dpr = rayOrigin.GetComponentInChildren<DefaultProxyRay>();
			return dpr == null || dpr.rayVisible;
		}

		static void ShowRay(Transform rayOrigin, bool rayOnly = false)
		{
			var dpr = rayOrigin.GetComponentInChildren<DefaultProxyRay>();
			if (dpr)
				dpr.Show(rayOnly);
		}

		static void HideRay(Transform rayOrigin, bool rayOnly = false)
		{
			var dpr = rayOrigin.GetComponentInChildren<DefaultProxyRay>();
			if (dpr)
				dpr.Hide(rayOnly);
		}

		static bool LockRay(Transform rayOrigin, object obj)
		{
			var dpr = rayOrigin.GetComponentInChildren<DefaultProxyRay>();
			return dpr && dpr.LockRay(obj);
		}

		static bool UnlockRay(Transform rayOrigin, object obj)
		{
			var dpr = rayOrigin.GetComponentInChildren<DefaultProxyRay>();
			return dpr && dpr.UnlockRay(obj);
		}

		void PreProcessRaycastSource(Transform rayOrigin)
		{
			var camera = U.Camera.GetMainCamera();
			var cameraPosition = camera.transform.position;
			var matrix = camera.worldToCameraMatrix;

#if ENABLE_MINIWORLD_RAY_SELECTION
		MiniWorldRay ray;
		if (m_MiniWorldRays.TryGetValue(rayOrigin, out ray))
			matrix = ray.miniWorld.getWorldToCameraMatrix(camera);
#endif

			if (!m_StandardManipulator)
				m_StandardManipulator = GetComponentInChildren<StandardManipulator>();

			if (m_StandardManipulator)
				m_StandardManipulator.AdjustScale(cameraPosition, matrix);

			if (!m_ScaleManipulator)
				m_ScaleManipulator = GetComponentInChildren<ScaleManipulator>();

			if (m_ScaleManipulator)
				m_ScaleManipulator.AdjustScale(cameraPosition, matrix);
		}

		void AddPlayerModel()
		{
			var playerModel = U.Object.Instantiate(m_PlayerModelPrefab, U.Camera.GetMainCamera().transform, false).GetComponent<Renderer>();
			m_SpatialHashModule.spatialHash.AddObject(playerModel, playerModel.bounds);
		}

		bool IsOverShoulder(Transform rayOrigin)
		{
			var radius = GetPointerLength(rayOrigin);
			var colliders = Physics.OverlapSphere(rayOrigin.position, radius, -1, QueryTriggerInteraction.Collide);
			foreach (var collider in colliders)
			{
				if (collider.CompareTag(kVRPlayerTag))
					return true;
			}
			return false;
		}

		void DeleteSceneObject(GameObject sceneObject)
		{
			var renderers = sceneObject.GetComponentsInChildren<Renderer>(true);
			foreach (var renderer in renderers)
			{
				m_SpatialHashModule.spatialHash.RemoveObject(renderer);
			}

			U.Object.Destroy(sceneObject);
		}

		List<string> GetFilterList()
		{
			return m_AssetTypes.ToList();
		}

		List<FolderData> GetFolderData()
		{
			if (m_FolderData == null)
				m_FolderData = new List<FolderData>();

			return m_FolderData;
		}

		void UpdateProjectFolders()
		{
			m_AssetTypes.Clear();

			StartCoroutine(CreateFolderData((folderData, hasNext) =>
			{
				m_FolderData = new List<FolderData> { folderData };

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
			}, m_AssetTypes));
		}

		IEnumerator CreateFolderData(Action<FolderData, bool> callback, HashSet<string> assetTypes, bool hasNext = true, HierarchyProperty hp = null)
		{
			if (hp == null)
			{
				hp = new HierarchyProperty(HierarchyType.Assets);
				hp.SetSearchFilter("t:object", 0);
			}
			var name = hp.name;
			var guid = hp.guid;
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
						yield return StartCoroutine(CreateFolderData((data, next) =>
						{
							folderList.Add(data);
							hasNext = next;
						}, assetTypes, hasNext, hp));
					} else if (hp.isMainRepresentation) // Ignore sub-assets (mixer children, terrain splats, etc.)
						assetList.Add(CreateAssetData(hp, assetTypes));

					if (hasNext)
						hasNext = hp.Next(null);

					// Spend a minimum amount of time in this function, and if we have extra time in the frame, use it
					if (Time.realtimeSinceStartup - m_ProjectFolderLoadYieldTime > kMaxFrameTime
						&& Time.realtimeSinceStartup - m_ProjectFolderLoadStartTime > kMinProjectFolderLoadTime)
					{
						m_ProjectFolderLoadYieldTime = Time.realtimeSinceStartup;
						yield return null;
						m_ProjectFolderLoadStartTime = Time.realtimeSinceStartup;
					}
				}

				if (hasNext)
					hp.Previous(null);
			}

			callback(new FolderData(name, folderList.Count > 0
				? folderList
				: null, assetList, guid), hasNext);
		}

		static AssetData CreateAssetData(HierarchyProperty hp, HashSet<string> assetTypes = null)
		{
			var type = string.Empty;
			if (assetTypes != null)
			{
				type = AssetDatabase.GetMainAssetTypeAtPath(AssetDatabase.GUIDToAssetPath(hp.guid)).Name;
				switch (type)
				{
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

			return new AssetData(hp.name, hp.guid, type);
		}

		List<HierarchyData> GetHierarchyData()
		{
			if (m_HierarchyData == null)
				return new List<HierarchyData>();

			return m_HierarchyData.children;
		}

		void UpdateHierarchyData()
		{
			if (m_HierarchyProperty == null)
			{
				m_HierarchyProperty = new HierarchyProperty(HierarchyType.GameObjects);
				m_HierarchyProperty.Next(null);
			} else
			{
				m_HierarchyProperty.Reset();
				m_HierarchyProperty.Next(null);
			}

			var hasNext = true;
			bool hasChanged = false;
			m_HierarchyData = CollectHierarchyData(ref hasNext, ref hasChanged, m_HierarchyData, m_HierarchyProperty);

			if (hasChanged)
			{
				foreach (var list in m_HierarchyLists)
				{
					list.hierarchyData = GetHierarchyData();
				}
			}
		}

		HierarchyData CollectHierarchyData(ref bool hasNext, ref bool hasChanged, HierarchyData hd, HierarchyProperty hp)
		{
			var depth = hp.depth;
			var name = hp.name;
			var instanceID = hp.instanceID;

			List<HierarchyData> list = null;
			list = (hd == null || hd.children == null)
				? new List<HierarchyData>()
				: hd.children;

			if (hp.hasChildren)
			{
				hasNext = hp.Next(null);
				var i = 0;
				while (hasNext && hp.depth > depth)
				{
					var go = EditorUtility.InstanceIDToObject(hp.instanceID);

					if (go == gameObject)
					{
						// skip children of EVR to prevent the display of EVR contents
						while (hp.Next(null) && hp.depth > depth + 1) { }
						name = hp.name;
						instanceID = hp.instanceID;
					}

					if (i >= list.Count)
					{
						list.Add(CollectHierarchyData(ref hasNext, ref hasChanged, null, hp));
						hasChanged = true;
					} else if (list[i].instanceID != hp.instanceID)
					{
						list[i] = CollectHierarchyData(ref hasNext, ref hasChanged, null, hp);
						hasChanged = true;
					} else
					{
						list[i] = CollectHierarchyData(ref hasNext, ref hasChanged, list[i], hp);
					}

					if (hasNext)
						hasNext = hp.Next(null);

					i++;
				}

				if (i != list.Count)
				{
					list.RemoveRange(i, list.Count - i);
					hasChanged = true;
				}

				if (hasNext)
					hp.Previous(null);
			} else
				list.Clear();

			List<HierarchyData> children = null;
			if (list.Count > 0)
				children = list;

			if (hd != null)
			{
				hd.children = children;
				hd.name = name;
				hd.instanceID = instanceID;
			}

			return hd ?? new HierarchyData(name, instanceID, children);
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

		[MenuItem("Window/EditorVR %e", true)]
		public static bool ShouldShowEditorVR()
		{
			return PlayerSettings.virtualRealitySupported;
		}

		static EditorVR()
		{
			VRView.onEnable += OnEVREnabled;
			VRView.onDisable += OnEVRDisabled;

			if (!PlayerSettings.virtualRealitySupported)
				Debug.Log("<color=orange>EditorVR requires VR support. Please check Virtual Reality Supported in Edit->Project Settings->Player->Other Settings</color>");

#if !ENABLE_OVR_INPUT && !ENABLE_STEAMVR_INPUT && !ENABLE_SIXENSE_INPUT
		Debug.Log("<color=orange>EditorVR requires at least one partner (e.g. Oculus, Vive) SDK to be installed for input. You can download these from the Asset Store or from the partner's website</color>");
#endif

			// Add EVR tags and layers if they don't exist
			var tags = new List<string>();
			var layers = new List<string>();
			U.Object.ForEachType(t =>
			{
				var tagAttributes = (RequiresTagAttribute[])t.GetCustomAttributes(typeof(RequiresTagAttribute), true);
				foreach (var attribute in tagAttributes)
					tags.Add(attribute.tag);

				var layerAttributes = (RequiresLayerAttribute[])t.GetCustomAttributes(typeof(RequiresLayerAttribute), true);
				foreach (var attribute in layerAttributes)
					layers.Add(attribute.layer);
			});

			foreach (var tag in tags)
				TagManager.AddTag(tag);

			foreach (var layer in layers)
				TagManager.AddLayer(layer);
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

			U.Object.Destroy(s_InputManager.GetComponent<JoystickInputToEvents>());
			U.Object.Destroy(s_InputManager.GetComponent<KeyboardInputToEvents>());
		}

		private static void OnEVRDisabled()
		{
			U.Object.Destroy(s_Instance.gameObject);
			U.Object.Destroy(s_InputManager.gameObject);
		}
#endif
#endif
	}
}
