#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Proxies;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Menus
{
    sealed class RadialMenu : MonoBehaviour, IInstantiateUI, IAlternateMenu, IUsesMenuOrigins, ICustomActionMap,
        IControlHaptics, IUsesNode, IConnectInterfaces, IRequestFeedback, IActionsMenu, ISpatialMenuProvider
    {
        const float k_ActivationThreshold = 0.5f; // Do not consume thumbstick or activate menu if the control vector's magnitude is below this threshold
        const string k_SpatialDisplayName = "Actions";
        const string k_SpatialDescription = "Perform actions based on selected-object context";

        [SerializeField]
        ActionMap m_ActionMap;

        [SerializeField]
        RadialMenuUI m_RadialMenuPrefab;

        [SerializeField]
        HapticPulse m_ReleasePulse;

        [SerializeField]
        HapticPulse m_ButtonHoverPulse;

        [SerializeField]
        HapticPulse m_ButtonClickedPulse;

        RadialMenuUI m_RadialMenuUI;
        List<ActionMenuData> m_MenuActions;
        Transform m_AlternateMenuOrigin;
        MenuHideFlags m_MenuHideFlags = MenuHideFlags.Hidden;

        readonly BindingDictionary m_Controls = new BindingDictionary();
        readonly List<SpatialMenu.SpatialMenuData> m_SpatialMenuData = new List<SpatialMenu.SpatialMenuData>();

        public event Action<Transform> itemWasSelected;

        public Transform rayOrigin { private get; set; }

        public Transform menuOrigin { get; set; }

        public GameObject menuContent { get { return m_RadialMenuUI.gameObject; } }

        public Node node { get; set; }

        public Bounds localBounds { get { return default(Bounds); } }
        public int priority { get { return 1; } }

        public ActionMap actionMap { get { return m_ActionMap; } }
        public bool ignoreActionMapInputLocking { get { return false; } }

        // Spatial UI support
        public string spatialMenuName { get { return k_SpatialDisplayName; } }
        public string spatialMenuDescription { get { return k_SpatialDescription; } }
        public bool displayingSpatially { get; set; }
        public List<SpatialMenu.SpatialMenuData> spatialMenuData { get { return m_SpatialMenuData; } }

        public List<ActionMenuData> menuActions
        {
            get { return m_MenuActions; }
            set
            {
                m_MenuActions = value;/*
                    .Where(a => a.sectionName != null && a.sectionName == ActionMenuItemAttribute.DefaultActionSectionName)
                    .OrderBy(a => a.priority)
                    .ToList();*/

                m_SpatialMenuData.Clear();
                var spatialMenuActions = new List<SpatialMenu.SpatialMenuElement>();
                var spatialMenuData = new SpatialMenu.SpatialMenuData(k_SpatialDisplayName, k_SpatialDescription, spatialMenuActions);
                m_SpatialMenuData.Add(spatialMenuData);
                foreach (var action in m_MenuActions)
                {
                    if (action.addToSpatialMenu)
                        spatialMenuActions.Add(new SpatialMenu.SpatialMenuElement(action.name, null, action.tooltipText, action.action.ExecuteAction));
                }

                if (m_RadialMenuUI)
                    m_RadialMenuUI.actions = value;
            }
        }

        public Transform alternateMenuOrigin
        {
            get { return m_AlternateMenuOrigin; }
            set
            {
                m_AlternateMenuOrigin = value;

                if (m_RadialMenuUI != null)
                    m_RadialMenuUI.alternateMenuOrigin = value;
            }
        }

        public MenuHideFlags menuHideFlags
        {
            get { return m_MenuHideFlags; }
            set
            {
                if (m_MenuHideFlags != value)
                {
                    m_MenuHideFlags = value;
                    var visible = value == 0;
                    if (m_RadialMenuUI)
                        m_RadialMenuUI.visible = visible;

                    if (visible)
                        ShowFeedback();
                    else
                        this.ClearFeedbackRequests();
                }
            }
        }

        void Start()
        {
            m_RadialMenuUI = this.InstantiateUI(m_RadialMenuPrefab.gameObject).GetComponent<RadialMenuUI>();
            m_RadialMenuUI.alternateMenuOrigin = alternateMenuOrigin;
            m_RadialMenuUI.actions = menuActions;
            this.ConnectInterfaces(m_RadialMenuUI); // Connect interfaces before performing setup on the UI
            m_RadialMenuUI.Setup();
            m_RadialMenuUI.buttonHovered += OnButtonHovered;
            m_RadialMenuUI.buttonClicked += OnButtonClicked;

            InputUtils.GetBindingDictionaryFromActionMap(m_ActionMap, m_Controls);
        }

        // TODO - REMEMBER TO WORK IN HERE 
        public void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl)
        {
            var radialMenuInput = (RadialMenuInput)input;
            if (radialMenuInput == null || m_MenuHideFlags != 0)
            {
                this.ClearFeedbackRequests();
                return;
            }

            var inputDirection = radialMenuInput.navigate.vector2;

            if (inputDirection.magnitude > k_ActivationThreshold)
            {
                // Composite controls need to be consumed separately
                consumeControl(radialMenuInput.navigateX);
                consumeControl(radialMenuInput.navigateY);
                m_RadialMenuUI.buttonInputDirection = inputDirection;
            }
            else
            {
                m_RadialMenuUI.buttonInputDirection = Vector2.zero;
            }

            var selectControl = radialMenuInput.selectItem;
            m_RadialMenuUI.pressedDown = selectControl.wasJustPressed;
            if (m_RadialMenuUI.pressedDown)
                consumeControl(selectControl);

            if (selectControl.wasJustReleased)
            {
                this.Pulse(node, m_ReleasePulse);

                m_RadialMenuUI.SelectionOccurred();

                if (itemWasSelected != null)
                    itemWasSelected(rayOrigin);

                consumeControl(selectControl);
            }
        }

        void OnButtonClicked()
        {
            this.Pulse(node, m_ButtonClickedPulse);
        }

        void OnButtonHovered()
        {
            this.Pulse(node, m_ButtonHoverPulse);
        }

        void ShowFeedback()
        {
            List<VRInputDevice.VRControl> controls;
            if (m_Controls.TryGetValue("SelectItem", out controls))
            {
                foreach (var id in controls)
                {
                    var request = (ProxyFeedbackRequest)this.GetFeedbackRequestObject(typeof(ProxyFeedbackRequest));
                    request.control = id;
                    request.node = node;
                    request.tooltipText = "Select Action (Press to Execute)";
                    this.AddFeedbackRequest(request);
                }
            }
        }
    }
}
#endif
