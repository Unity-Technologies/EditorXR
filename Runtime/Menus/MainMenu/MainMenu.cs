using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Labs.EditorXR.Interfaces;
using Unity.Labs.Utils;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Proxies;
using UnityEditor.Experimental.EditorVR.Tools;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEditor.Experimental.EditorVR.Workspaces;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Menus
{
    sealed class MainMenu : MonoBehaviour, IMainMenu, IConnectInterfaces, IInstantiateUI, ICreateWorkspace,
        ICustomActionMap, IUsesMenuOrigins, IUsesDeviceType, IControlHaptics, IUsesNode, IRayToNode, IUsesRayOrigin,
        IRequestFeedback, INodeToRay, ISpatialMenuProvider
    {
        const string k_SettingsMenuSectionName = "Settings";
        const float k_MaxFlickDuration = 0.3f;

#pragma warning disable 649
        [SerializeField]
        ActionMap m_ActionMap;

        [SerializeField]
        HapticPulse m_FaceRotationPulse;

        [SerializeField]
        HapticPulse m_ShowPulse;

        [SerializeField]
        HapticPulse m_HidePulse;

        [SerializeField]
        MainMenuUI m_MainMenuPrefab;

        [SerializeField]
        HapticPulse m_ButtonClickPulse;

        [SerializeField]
        HapticPulse m_ButtonHoverPulse;
#pragma warning restore 649

        Transform m_AlternateMenuOrigin;
        Transform m_MenuOrigin;
        MainMenuUI m_MainMenuUI;
        float m_LastRotationInput;
        MenuHideFlags m_MenuHideFlags = MenuHideFlags.Hidden;
        float m_RotationInputStartValue;
        float m_RotationInputStartTime;
        readonly Dictionary<Type, MainMenuButton> m_ToolButtons = new Dictionary<Type, MainMenuButton>();
        readonly Dictionary<ISettingsMenuProvider, GameObject> m_SettingsMenus = new Dictionary<ISettingsMenuProvider, GameObject>();
        readonly Dictionary<ISettingsMenuItemProvider, GameObject> m_SettingsMenuItems = new Dictionary<ISettingsMenuItemProvider, GameObject>();
        readonly List<SpatialMenu.SpatialMenuData> m_SpatialMenuData = new List<SpatialMenu.SpatialMenuData>();
        readonly List<SpatialMenu.SpatialMenuElementContainer> m_WorkspaceSpatialMenuElements = new List<SpatialMenu.SpatialMenuElementContainer>();
        readonly List<SpatialMenu.SpatialMenuElementContainer> m_ToolsSpatialMenuElements = new List<SpatialMenu.SpatialMenuElementContainer>();

        readonly BindingDictionary m_Controls = new BindingDictionary();

        public List<Type> menuTools { internal get; set; }
        public List<Type> menuWorkspaces { internal get; set; }
        public Dictionary<KeyValuePair<Type, Transform>, ISettingsMenuProvider> settingsMenuProviders { get; set; }
        public Dictionary<KeyValuePair<Type, Transform>, ISettingsMenuItemProvider> settingsMenuItemProviders { get; set; }
        public Transform targetRayOrigin { private get; set; }
        public Node node { get; set; }

        public GameObject menuContent { get { return m_MainMenuUI.gameObject; } }

        public Transform rayOrigin { internal get; set; }

        public Bounds localBounds { get { return m_MainMenuUI.localBounds; } }
        public int priority { get { return 0; } }

        public bool focus { get { return m_MainMenuUI.hovering; } }

        public ActionMap actionMap { get { return m_ActionMap; } }
        public bool ignoreActionMapInputLocking { get { return false; } }

        public List<SpatialMenu.SpatialMenuData> spatialMenuData { get { return m_SpatialMenuData; } }

        public Transform menuOrigin
        {
            get { return m_MenuOrigin; }
            set
            {
                m_MenuOrigin = value;
                if (m_MainMenuUI)
                    m_MainMenuUI.menuOrigin = value;
            }
        }

        public Transform alternateMenuOrigin
        {
            get { return m_AlternateMenuOrigin; }
            set
            {
                m_AlternateMenuOrigin = value;
                if (m_MainMenuUI)
                    m_MainMenuUI.alternateMenuOrigin = value;
            }
        }

        public MenuHideFlags menuHideFlags
        {
            get { return m_MenuHideFlags; }
            set
            {
                var wasVisible = m_MenuHideFlags == 0;
                var wasPermanent = (m_MenuHideFlags & MenuHideFlags.Hidden) != 0;
                if (m_MenuHideFlags != value)
                {
                    m_MenuHideFlags = value;
                    var visible = value == 0;
                    if (m_MainMenuUI)
                    {
                        var isPermanent = (value & MenuHideFlags.Hidden) != 0;
                        m_MainMenuUI.visible = visible;
                        if (wasPermanent && visible || wasVisible && isPermanent)
                            SendVisibilityPulse();
                    }

                    if (visible)
                        ShowFeedback();
                    else
                        this.ClearFeedbackRequests();
                }
            }
        }

        void OnEnable()
        {
            m_MainMenuUI = this.InstantiateUI(m_MainMenuPrefab.gameObject).GetComponent<MainMenuUI>();
            this.ConnectInterfaces(m_MainMenuUI);
            m_MainMenuUI.alternateMenuOrigin = alternateMenuOrigin;
            m_MainMenuUI.menuOrigin = menuOrigin;
            m_MainMenuUI.Setup();

            InputUtils.GetBindingDictionaryFromActionMap(m_ActionMap, m_Controls);
        }

        void Start()
        {
            CreateFaceButtons();
            UpdateToolButtons();
            RegisterSpatialMenuElements();
        }

        void RegisterSpatialMenuElements()
        {
            spatialMenuData.Add(new SpatialMenu.SpatialMenuData("Workspaces", "Open a workspace", m_WorkspaceSpatialMenuElements));
            spatialMenuData.Add(new SpatialMenu.SpatialMenuData("Tools", "Select a tool", m_ToolsSpatialMenuElements));

            m_ToolsSpatialMenuElements.Add(new SpatialMenu.SpatialMenuElementContainer("Selection Tool", "Perform standard object selection & manipulation", node =>
            {
                this.SelectTool(this.RequestRayOriginFromNode(node), typeof(SelectionTool), hideMenu: true);
            }));
        }

        public void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl)
        {
            if (!m_MainMenuUI.visible)
                return;

            var mainMenuInput = (MainMenuInput)input;
            var rotationInput = -mainMenuInput.rotate.rawValue;

            const float kFlickDeltaThreshold = 0.5f;

            if (this.GetDeviceType() == DeviceType.Vive)
            {
                if (!Mathf.Approximately(rotationInput, 0f))
                {
                    var time = Time.time;
                    if (Mathf.Approximately(m_LastRotationInput, 0f))
                    {
                        // Touch began
                        m_RotationInputStartValue = rotationInput;
                        m_RotationInputStartTime = time;
                    }
                    else
                    {
                        // Touch held
                        var distance = rotationInput - m_RotationInputStartValue;
                        var lastDistance = m_LastRotationInput - m_RotationInputStartValue;
                        if (Mathf.Abs(distance) >= kFlickDeltaThreshold
                            && Mathf.Abs(lastDistance) < kFlickDeltaThreshold
                            && time - m_RotationInputStartTime < k_MaxFlickDuration)
                        {
                            m_RotationInputStartValue = rotationInput;
                            m_RotationInputStartTime = time;
                            if (!m_MainMenuUI.rotating)
                            {
                                FlickMenu(distance);
                            }
                        }
                    }
                }
            }
            else if (Mathf.Abs(rotationInput) >= kFlickDeltaThreshold
                && Mathf.Abs(m_LastRotationInput) < kFlickDeltaThreshold)
            {
                FlickMenu(rotationInput);
            }

            m_LastRotationInput = rotationInput;
        }

        void FlickMenu(float rotationInput)
        {
            m_MainMenuUI.targetFaceIndex += (int)Mathf.Sign(rotationInput);
            this.Pulse(node, m_FaceRotationPulse);
        }

        void OnDestroy()
        {
            if (m_MainMenuUI)
                UnityObjectUtils.Destroy(m_MainMenuUI.gameObject);
        }

        void CreateFaceButtons()
        {
            var types = new HashSet<Type>();
            types.UnionWith(menuTools);
            types.UnionWith(menuWorkspaces);
            types.UnionWith(settingsMenuProviders.Keys.Select(provider => provider.Key));
            types.UnionWith(settingsMenuItemProviders.Keys.Select(provider => provider.Key));

            if (Application.isPlaying)
                types.RemoveWhere(type => type.GetCustomAttributes(true).OfType<EditorOnlyWorkspaceAttribute>().Any());

            foreach (var type in types)
            {
                var customMenuAttribute = (MainMenuItemAttribute)type.GetCustomAttributes(typeof(MainMenuItemAttribute), false).FirstOrDefault();
                if (customMenuAttribute != null && !customMenuAttribute.shown)
                    continue;

                var isTool = typeof(ITool).IsAssignableFrom(type) && menuTools.Contains(type);
                var isWorkspace = typeof(Workspace).IsAssignableFrom(type);
                var isSettingsProvider = typeof(ISettingsMenuProvider).IsAssignableFrom(type);
                var isSettingsItemProvider = typeof(ISettingsMenuItemProvider).IsAssignableFrom(type);

                ITooltip tooltip = null;
                MainMenuUI.ButtonData buttonData = null;

                var selectedType = type; // Local variable for closure
                if (customMenuAttribute != null && customMenuAttribute.shown)
                {
                    tooltip = customMenuAttribute.tooltip;

                    buttonData = new MainMenuUI.ButtonData(customMenuAttribute.name)
                    {
                        sectionName = customMenuAttribute.sectionName,
                        description = customMenuAttribute.description
                    };
                }

                if (isTool)
                {
                    if (buttonData == null)
                        buttonData = new MainMenuUI.ButtonData(type.Name);

                    var mainMenuButton = CreateFaceButton(buttonData, tooltip, () =>
                    {
                        if (targetRayOrigin)
                        {
                            this.SelectTool(targetRayOrigin, selectedType,
                                hideMenu: typeof(IInstantiateMenuUI).IsAssignableFrom(selectedType));
                            UpdateToolButtons();
                        }
                    });

                    m_ToolButtons[type] = mainMenuButton;

                    // Assign Tools Menu button preview properties
                    if (mainMenuButton != null)
                        mainMenuButton.toolType = selectedType;

                    m_ToolsSpatialMenuElements.Add(new SpatialMenu.SpatialMenuElementContainer(buttonData.name, buttonData.description, node =>
                    {
                        this.SelectTool(this.RequestRayOriginFromNode(node), selectedType,
                            hideMenu: typeof(IInstantiateMenuUI).IsAssignableFrom(selectedType));
                    }));

                    UpdateToolButtons();
                }

                if (isWorkspace)
                {
                    // For workspaces that haven't specified a custom attribute, do some menu categorization automatically
                    if (buttonData == null)
                        buttonData = new MainMenuUI.ButtonData(type.Name) { sectionName = "Workspaces" };

                    CreateFaceButton(buttonData, tooltip, () => { this.CreateWorkspace(selectedType); });

                    m_WorkspaceSpatialMenuElements.Add(new SpatialMenu.SpatialMenuElementContainer(buttonData.name, buttonData.description, node => this.CreateWorkspace(selectedType)));
                }

                if (isSettingsProvider)
                {
                    foreach (var providerPair in settingsMenuProviders)
                    {
                        var kvp = providerPair.Key;
                        if (kvp.Key == type && (kvp.Value == null || kvp.Value == rayOrigin))
                            AddSettingsMenu(providerPair.Value, buttonData, tooltip);
                    }
                }

                if (isSettingsItemProvider)
                {
                    foreach (var providerPair in settingsMenuItemProviders)
                    {
                        var kvp = providerPair.Key;
                        if (kvp.Key == type && (kvp.Value == null || kvp.Value == rayOrigin))
                            AddSettingsMenuItem(providerPair.Value);
                    }
                }
            }
        }

        MainMenuButton CreateFaceButton(MainMenuUI.ButtonData buttonData, ITooltip tooltip, Action buttonClickCallback)
        {
            var mainMenuButton = m_MainMenuUI.CreateFaceButton(buttonData);
            if (mainMenuButton == null)
                return null;

            var button = mainMenuButton.button;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                if (m_MenuHideFlags == 0)
                    buttonClickCallback();
            });

            mainMenuButton.hovered += OnButtonHovered;
            mainMenuButton.clicked += OnButtonClicked;
            mainMenuButton.tooltip = tooltip;

            return mainMenuButton;
        }

        void UpdateToolButtons()
        {
            foreach (var kvp in m_ToolButtons)
            {
                kvp.Value.selected = this.IsToolActive(targetRayOrigin, kvp.Key);
            }
        }

        void OnButtonClicked(Transform rayOrigin)
        {
            this.Pulse(this.RequestNodeFromRayOrigin(rayOrigin), m_ButtonClickPulse);
            this.ClearToolMenuButtonPreview();
        }

        void OnButtonHovered(Transform rayOrigin, Type buttonType, string buttonDescription)
        {
            this.Pulse(this.RequestNodeFromRayOrigin(rayOrigin), m_ButtonHoverPulse);

            // Pass the pointer which is over us, so this information can supply context (e.g. selecting a tool for a different hand)
            // Enable preview-mode on a Tools Menu button; Display on the opposite proxy device by evaluating the entering RayOrigin
            // Disable any existing previews being displayed in ToolsMenus
            this.ClearToolMenuButtonPreview();

            if (buttonType != null && rayOrigin != null)
                this.PreviewInToolMenuButton(rayOrigin, buttonType, buttonDescription);
        }

        void OnToggleHovered(Transform rayOrigin)
        {
            this.Pulse(this.RequestNodeFromRayOrigin(rayOrigin), m_ButtonHoverPulse);
        }

        void SendVisibilityPulse()
        {
            this.Pulse(node, m_MenuHideFlags == 0 ? m_HidePulse : m_ShowPulse);
        }

        public void AddSettingsMenu(ISettingsMenuProvider provider)
        {
            var type = provider.GetType();
            var customMenuAttribute = (MainMenuItemAttribute)type.GetCustomAttributes(typeof(MainMenuItemAttribute), false).FirstOrDefault();

            ITooltip tooltip = null;
            MainMenuUI.ButtonData buttonData;
            if (customMenuAttribute != null && customMenuAttribute.shown)
            {
                tooltip = customMenuAttribute.tooltip;

                buttonData = new MainMenuUI.ButtonData(customMenuAttribute.name)
                {
                    sectionName = customMenuAttribute.sectionName,
                    description = customMenuAttribute.description
                };
            }
            else
            {
                buttonData = new MainMenuUI.ButtonData(type.Name);
            }

            AddSettingsMenu(provider, buttonData, tooltip);
        }

        void AddSettingsMenu(ISettingsMenuProvider provider, MainMenuUI.ButtonData buttonData, ITooltip tooltip)
        {
            buttonData.sectionName = k_SettingsMenuSectionName;

            var button = CreateFaceButton(buttonData, tooltip, () =>
            {
                var instance = m_MainMenuUI.AddSubmenu(k_SettingsMenuSectionName, provider.settingsMenuPrefab);
                m_SettingsMenus[provider] = instance;
                provider.settingsMenuInstance = instance;
                AddToggleHaptics(instance);
            });

            button.hovered += OnButtonHovered;
            button.clicked += OnButtonClicked;
        }

        void AddToggleHaptics(GameObject menuInstance)
        {
            var toggles = menuInstance.GetComponentsInChildren<MainMenuToggle>();
            if (toggles != null && toggles.Length > 0)
            {
                foreach (var toggle in toggles)
                {
                    toggle.hovered += OnToggleHovered;
                    toggle.clicked += OnButtonClicked;
                }
            }
        }

        public void RemoveSettingsMenu(ISettingsMenuProvider provider)
        {
            GameObject instance;
            if (m_SettingsMenus.TryGetValue(provider, out instance))
            {
                if (instance)
                    UnityObjectUtils.Destroy(instance);

                m_SettingsMenus.Remove(provider);
            }

            provider.settingsMenuInstance = null;
        }

        public void AddSettingsMenuItem(ISettingsMenuItemProvider provider)
        {
            var instance = m_MainMenuUI.CreateCustomButton(provider.settingsMenuItemPrefab, k_SettingsMenuSectionName);
            m_SettingsMenuItems[provider] = instance;
            provider.settingsMenuItemInstance = instance;
            AddToggleHaptics(instance);
        }

        public void RemoveSettingsMenuItem(ISettingsMenuItemProvider provider)
        {
            GameObject instance;
            if (m_SettingsMenuItems.TryGetValue(provider, out instance))
            {
                if (instance)
                    UnityObjectUtils.Destroy(instance);

                m_SettingsMenuItems.Remove(provider);
            }

            provider.settingsMenuItemInstance = null;
        }

        void ShowFeedback()
        {
            var tooltipText = this.GetDeviceType() == DeviceType.Vive ? "Swipe to Rotate Menu" : "Rotate Menu";
            List<VRInputDevice.VRControl> controls;
            if (m_Controls.TryGetValue("FlickFace", out controls))
            {
                foreach (var id in controls)
                {
                    var request = (ProxyFeedbackRequest)this.GetFeedbackRequestObject(typeof(ProxyFeedbackRequest));
                    request.control = id;
                    request.node = node;
                    request.tooltipText = tooltipText;
                    this.AddFeedbackRequest(request);
                }
            }
        }
    }
}
