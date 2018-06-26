#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEditor.Experimental.EditorVR.Proxies;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Menus
{
    sealed class ToolsMenu : MonoBehaviour, IToolsMenu, IConnectInterfaces, IInstantiateUI, IControlHaptics,
        IUsesViewerScale, IControlSpatialScrolling, IControlSpatialHinting, IRayVisibilitySettings, IUsesRayOrigin,
        IRequestFeedback
    {
        const int k_ActiveToolOrderPosition = 1; // A active-tool button position used in this particular ToolButton implementation
        const int k_MaxButtonCount = 16;

        [SerializeField]
        Sprite m_MainMenuIcon;

        [SerializeField]
        ActionMap m_ActionMap;

        [SerializeField]
        ToolsMenuUI m_ToolsMenuPrefab;

        [SerializeField]
        ToolsMenuButton _mToolsMenuButtonTemplate;

        [SerializeField]
        HapticPulse m_ButtonClickPulse;

        [SerializeField]
        HapticPulse m_ButtonHoverPulse;

        [SerializeField]
        HapticPulse m_HidingPulse; // The pulse performed when ending a spatial selection

        float m_AllowToolToggleBeforeThisTime;
        Vector3 m_SpatialScrollStartPosition;
        ToolsMenuUI m_ToolsMenuUI;

        readonly BindingDictionary m_Controls = new BindingDictionary();
        readonly List<ProxyFeedbackRequest> m_ScrollFeedback = new List<ProxyFeedbackRequest>();
        readonly List<ProxyFeedbackRequest> m_MenuFeedback = new List<ProxyFeedbackRequest>();

        public Transform menuOrigin { get; set; }

        List<IToolsMenuButton> buttons { get { return m_ToolsMenuUI.buttons; } }

        public bool alternateMenuVisible { set { m_ToolsMenuUI.moveToAlternatePosition = value; } }

        public Action<Transform, int, bool> highlightSingleButton { get; set; }
        public Action<Transform> selectHighlightedButton { get; set; }
        public Action<Type, Sprite> setButtonForType { get; set; }
        public Action<Type, Type> deleteToolsMenuButton { get; set; }
        public Node node { get; set; }
        public IToolsMenuButton PreviewToolsMenuButton { get; private set; }
        public Transform alternateMenuOrigin { get; set; }
        public SpatialScrollModule.SpatialScrollData spatialScrollData { get; set; }

        public ActionMap actionMap { get { return m_ActionMap; } }
        public bool ignoreLocking { get { return false; } }

        public Transform rayOrigin { get; set; }

        public bool mainMenuActivatorInteractable
        {
            set { PreviewToolsMenuButton.interactable = value; }
        }

        void Awake()
        {
            setButtonForType = CreateToolsMenuButton;
            deleteToolsMenuButton = DeleteToolsMenuButton;
            InputUtils.GetBindingDictionaryFromActionMap(m_ActionMap, m_Controls);
        }

        void OnDestroy()
        {
            this.RemoveRayVisibilitySettings(rayOrigin, this);
        }

        void CreateToolsMenuUI()
        {
            m_ToolsMenuUI = this.InstantiateUI(m_ToolsMenuPrefab.gameObject, rayOrigin, true, rayOrigin).GetComponent<ToolsMenuUI>();
            m_ToolsMenuUI.maxButtonCount = k_MaxButtonCount;
            m_ToolsMenuUI.mainMenuActivatorSelected = this.MainMenuActivatorSelected;
            m_ToolsMenuUI.buttonHovered += OnButtonHover;
            m_ToolsMenuUI.buttonClicked += OnButtonClick;
            m_ToolsMenuUI.buttonSelected += OnButtonSelected;
            m_ToolsMenuUI.closeMenu += CloseMenu;

            // Alternate menu origin isn't set when awake or start run
            var toolsMenuUITransform = m_ToolsMenuUI.transform;
            toolsMenuUITransform.SetParent(alternateMenuOrigin);
            toolsMenuUITransform.localPosition = Vector3.zero;
            toolsMenuUITransform.localRotation = Quaternion.identity;
        }

        void CreateToolsMenuButton(Type toolType, Sprite buttonIcon)
        {
            // Verify first that the ToolsMenuUI exists
            // This is called in EditorVR.Tools before the UI can be created herein in Awake
            // The SelectionTool & MainMenu buttons are created immediately after instantiating the ToolsMenu
            if (m_ToolsMenuUI == null)
                CreateToolsMenuUI();

            // Select an existing ToolButton if the type is already present in a button
            if (buttons.Any(x => x.toolType == toolType))
            {
                m_ToolsMenuUI.SelectExistingToolType(toolType);
                return;
            }

            if (buttons.Count >= k_MaxButtonCount) // Return if tool type already occupies a tool button
                return;

            var buttonTransform = ObjectUtils.Instantiate(_mToolsMenuButtonTemplate.gameObject, m_ToolsMenuUI.buttonContainer, false).transform;
            var button = buttonTransform.GetComponent<ToolsMenuButton>();
            this.ConnectInterfaces(button);

            button.rayOrigin = rayOrigin;
            button.toolType = toolType; // Assign Tool Type before assigning order
            button.icon = toolType != typeof(IMainMenu) ? buttonIcon : m_MainMenuIcon;
            button.highlightSingleButton = highlightSingleButton;
            button.selectHighlightedButton = selectHighlightedButton;
            button.rayOrigin = rayOrigin;

            if (toolType == typeof(IMainMenu))
                PreviewToolsMenuButton = button;

            m_ToolsMenuUI.AddButton(button, buttonTransform);
        }

        void DeleteToolsMenuButton(Type toolTypeToDelete, Type toolTypeToSelectAfterDelete)
        {
            if (m_ToolsMenuUI.DeleteButtonOfType(toolTypeToDelete))
                m_ToolsMenuUI.SelectNextExistingToolButton();
        }

        public void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl)
        {
            var buttonCount = buttons.Count;
            if (buttonCount <= k_ActiveToolOrderPosition + 1)
                return;

            const float kAllowToggleDuration = 0.25f;

            var toolslMenuInput = (ToolsMenuInput)input;

            if (spatialScrollData != null && toolslMenuInput.cancel.wasJustPressed)
            {
                consumeControl(toolslMenuInput.cancel);
                consumeControl(toolslMenuInput.show);
                consumeControl(toolslMenuInput.select);
                OnButtonClick();
                CloseMenu(); // Also ends spatial scroll
                m_ToolsMenuUI.allButtonsVisible = false;
            }

            if (toolslMenuInput.show.wasJustPressed)
                ShowScrollFeedback();

            if (toolslMenuInput.show.wasJustReleased)
                HideScrollFeedback();

            if (spatialScrollData == null && (toolslMenuInput.show.wasJustPressed || toolslMenuInput.show.isHeld) && toolslMenuInput.select.wasJustPressed)
            {
                m_SpatialScrollStartPosition = alternateMenuOrigin.position;
                m_AllowToolToggleBeforeThisTime = Time.realtimeSinceStartup + kAllowToggleDuration;
                this.SetSpatialHintControlNode(node);
                m_ToolsMenuUI.spatiallyScrolling = true; // Triggers the display of the directional hint arrows
                consumeControl(toolslMenuInput.show);
                consumeControl(toolslMenuInput.select);

                // Assign initial SpatialScrollData; begin scroll
                spatialScrollData = this.PerformSpatialScroll(node, m_SpatialScrollStartPosition, alternateMenuOrigin.position, 0.325f, m_ToolsMenuUI.buttons.Count, m_ToolsMenuUI.maxButtonCount);

                HideScrollFeedback();
                ShowMenuFeedback();
            }
            else if (spatialScrollData != null && toolslMenuInput.show.isHeld)
            {
                consumeControl(toolslMenuInput.show);
                consumeControl(toolslMenuInput.select);

                // Attempt to close a button, if a scroll has passed the trigger threshold
                if (spatialScrollData != null && toolslMenuInput.select.wasJustPressed)
                {
                    if (m_ToolsMenuUI.DeleteHighlightedButton())
                        buttonCount = buttons.Count; // The MainMenu button will be hidden, subtract 1 from the activeButtonCount

                    if (buttonCount <= k_ActiveToolOrderPosition + 1)
                    {
                        if (spatialScrollData != null)
                            this.EndSpatialScroll();

                        return;
                    }
                }

                // normalized input should loop after reaching the 0.15f length
                buttonCount -= 1; // Decrement to disallow cycling through the main menu button
                spatialScrollData = this.PerformSpatialScroll(node, m_SpatialScrollStartPosition, alternateMenuOrigin.position, 0.325f, m_ToolsMenuUI.buttons.Count, m_ToolsMenuUI.maxButtonCount);
                var normalizedRepeatingPosition = spatialScrollData.normalizedLoopingPosition;
                if (!Mathf.Approximately(normalizedRepeatingPosition, 0f))
                {
                    if (!m_ToolsMenuUI.allButtonsVisible)
                    {
                        m_ToolsMenuUI.spatialDragDistance = spatialScrollData.dragDistance;
                        this.SetSpatialHintState(SpatialHintModule.SpatialHintStateFlags.CenteredScrolling);
                        m_ToolsMenuUI.allButtonsVisible = true;
                    }
                    else if (spatialScrollData.spatialDirection != null)
                    {
                        m_ToolsMenuUI.startingDragOrigin = spatialScrollData.spatialDirection;
                    }

                    m_ToolsMenuUI.HighlightSingleButtonWithoutMenu((int)(buttonCount * normalizedRepeatingPosition) + 1);
                }
            }
            else if (spatialScrollData != null && !toolslMenuInput.show.isHeld && !toolslMenuInput.select.isHeld)
            {
                consumeControl(toolslMenuInput.show);
                consumeControl(toolslMenuInput.select);

                if (spatialScrollData != null && spatialScrollData.passedMinDragActivationThreshold)
                {
                    m_ToolsMenuUI.SelectHighlightedButton();
                }
                else if (Time.realtimeSinceStartup < m_AllowToolToggleBeforeThisTime)
                {
                    // Allow for single press+release to cycle through tools
                    m_ToolsMenuUI.SelectNextExistingToolButton();
                    OnButtonClick();
                }

                CloseMenu();
            }
            else if (spatialScrollData == null && (toolslMenuInput.show.wasJustPressed || toolslMenuInput.show.isHeld))
            {
                // Consume the control to activate spatial scrolling - so nothing else fires accidentally when attempting to engage this feature
                if (toolslMenuInput.select.rawValue > 0.0f)
                {
                    consumeControl(toolslMenuInput.select);
                }
            }
        }

        void OnButtonClick()
        {
            this.Pulse(node, m_ButtonClickPulse);
            this.SetSpatialHintState(SpatialHintModule.SpatialHintStateFlags.Hidden);
        }

        void OnButtonHover()
        {
            this.Pulse(node, m_ButtonHoverPulse);
        }

        void OnButtonSelected(Transform rayOrigin, Type buttonType)
        {
            this.SelectTool(rayOrigin, buttonType, false);
        }

        void CloseMenu()
        {
            this.ClearFeedbackRequests();
            this.Pulse(node, m_HidingPulse);
            this.EndSpatialScroll(); // Free the spatial scroll data owned by this object
        }

        void ShowFeedback(List<ProxyFeedbackRequest> requests, string controlName, string tooltipText = null)
        {
            if (tooltipText == null)
                tooltipText = controlName;

            List<VRInputDevice.VRControl> ids;
            if (m_Controls.TryGetValue(controlName, out ids))
            {
                foreach (var id in ids)
                {
                    var request = (ProxyFeedbackRequest)this.GetFeedbackRequestObject(typeof(ProxyFeedbackRequest));
                    request.node = node;
                    request.control = id;
                    request.priority = 1;
                    request.tooltipText = tooltipText;
                    requests.Add(request);
                    this.AddFeedbackRequest(request);
                }
            }
        }

        void ShowScrollFeedback()
        {
            ShowFeedback(m_ScrollFeedback, "select", "Scroll to Change Tool");
        }

        void ShowMenuFeedback()
        {
            ShowFeedback(m_MenuFeedback, "select", "Remove Tool");
            ShowFeedback(m_MenuFeedback, "cancel", "Cancel Scrolling");
            ShowFeedback(m_MenuFeedback, "show", "Release to Select Tool");
        }

        void HideFeedback(List<ProxyFeedbackRequest> requests)
        {
            foreach (var request in requests)
            {
                this.RemoveFeedbackRequest(request);
            }
            requests.Clear();
        }

        void HideScrollFeedback()
        {
            HideFeedback(m_ScrollFeedback);
        }
    }
}
#endif
