#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using TMPro.Examples;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Helpers;
using UnityEditor.Experimental.EditorVR.Menus;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEditor.Experimental.EditorVR.Proxies;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;
using UnityEngine.Playables;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace UnityEditor.Experimental.EditorVR
{
    [ProcessInput(2)] // Process input after the ProxyAnimator, but before other IProcessInput implementors
    public class SpatialMenu : MonoBehaviour, IControlSpatialScrolling, IInstantiateUI,
        IUsesNode, IUsesRayOrigin, ISelectTool, IDetectSpatialInputType, IConnectInterfaces,
        IControlHaptics, INodeToRay, IMenu
    {
        public class SpatialMenuData
        {
            /// <summary>
            /// Name of the menu whose contents will be added to the menu
            /// </summary>
            public string spatialMenuName { get; private set; }

            /// <summary>
            /// Description of the menu whose contents will be added to the menu
            /// </summary>
            public string spatialMenuDescription { get; private set; }

            /// <summary>
            /// Bool denoting that this menu's contents are being displayed via the SpatialUI
            /// </summary>
            public bool displayingSpatially { get; set; }

            /// <summary>
            /// Bool denoting that this element is currently highlighted as either a section title or a sub-menu element
            /// </summary>
            public bool highlighted { get; set; }

            /// <summary>
            /// Collection of elements with which to populate the corresponding spatial UI table/list/view
            /// </summary>
            public List<SpatialMenu.SpatialMenuElement> spatialMenuElements { get; private set; }

            public SpatialMenuData(string menuName, string menuDescription, List<SpatialMenu.SpatialMenuElement> menuElements)
            {
                spatialMenuName = menuName;
                spatialMenuDescription = menuDescription;
                spatialMenuElements = menuElements;
            }
        }

        // TODO expose as a user preference, for spatial UI distance
        const float k_SpatialQuickToggleDuration = 0.25f;
        const float k_WristReturnRotationThreshold = 0.3f;
        const float k_MenuSectionBlockedTransitionTimeWindow = 1f;
        const float k_SpatialScrollVectorLength = 0.125f;

        public enum SpatialinterfaceState
        {
            hidden,
            navigatingTopLevel,
            navigatingSubMenuContent,
        }

        [SerializeField]
        SpatialMenuUI _mSpatialMenuUiPrefab;

        [SerializeField]
        ActionMap m_ActionMap;

        [Header("Haptic Pulses")]
        [SerializeField]
        HapticPulse m_MenuOpenPulse;

        [SerializeField]
        HapticPulse m_MenuClosePulse;

        [SerializeField]
        HapticPulse m_NavigateBackPulse;

        [SerializeField]
        HapticPulse m_HighlightMenuElementPulse;

        static SpatialMenuUI m_SpatialMenuUi;
        SpatialinterfaceState m_SpatialinterfaceState;

        bool m_Visible;
        bool m_BeingMoved;
        Vector3 m_HomeSectionSpatialScrollStartLocalPosition;
        bool m_Transitioning;
        //ISpatialMenuProvider m_HighlightedTopLevelMenuProvider;

        SpatialMenuInput m_CurrentSpatialActionMapInput;

        // "Rotate wrist to return" members
        float m_StartingWristXRotation;
        float m_WristReturnVelocity;

        // Menu entrance start time
        float m_MenuEntranceStartTime;

        // Spatial rotation members
        Quaternion m_InitialSpatialLocalRotation;

        // Section name string, corresponding element collection, currentlyHighlightedState
        static readonly List<SpatialMenuData> s_SpatialMenuData = new List<SpatialMenuData>();

        /*
        ISpatialMenuProvider highlightedTopLevelMenuProvider
        {
            get { return m_HighlightedTopLevelMenuProvider; }
            set
            {
                if (m_HighlightedTopLevelMenuProvider == value)
                    return;

                m_HighlightedTopLevelMenuProvider = value;
                m_SpatialMenuUi.highlightedTopLevelMenuProvider = value;
            }
        }
        */

        //static readonly Dictionary<ISpatialMenuProvider, SpatialMenuElement> m_ProviderToMenuElements = new Dictionary<ISpatialMenuProvider, SpatialMenuElement>();

        string m_HighlightedSectionNameKey;
        int m_HighlightedMenuElementPosition; // element position amidst the currentlyDisplayedMenuElements
        List<SpatialMenuElement> m_HighlightedMenuElements;

        RotationVelocityTracker m_RotationVelocityTracker = new RotationVelocityTracker();
        ContinuousDirectionalVelocityTracker m_ContinuousDirectionalVelocityTracker = new ContinuousDirectionalVelocityTracker();

        List<SpatialMenuElement> highlightedMenuElements
        {
            get { return m_HighlightedMenuElements; }
            set
            {
                if (m_HighlightedMenuElements == value)
                    return;

                m_HighlightedMenuElements = value;
                m_SpatialMenuUi.highlightedMenuElements = m_HighlightedMenuElements;
            }
        }

        bool visible
        {
            get { return m_Visible; }

            set
            {
                if (m_Visible == value)
                    return;

                RefreshProviderData();
                m_Visible = value;
                m_SpatialMenuUi.visible = m_Visible;
                pollingSpatialInputType = m_Visible;

                if (m_Visible)
                {
                    //gameObject.SetActive(true);  MOVED TO SPATIAL UI View
                }
                else
                {
                    if (m_HighlightedMenuElements != null &&
                        m_HighlightedMenuElements.Count > 0 &&
                        m_HighlightedMenuElements[m_HighlightedMenuElementPosition] != null &&
                        m_HighlightedMenuElements[m_HighlightedMenuElementPosition].correspondingFunction != null)
                    {
                        m_HighlightedMenuElements[m_HighlightedMenuElementPosition].correspondingFunction();
                        this.Pulse(Node.None, m_HighlightMenuElementPulse);
                    }

                    this.Pulse(Node.None, m_MenuClosePulse);
                    spatialinterfaceState = SpatialinterfaceState.hidden;
                }
            }
        }

        SpatialinterfaceState spatialinterfaceState
        {
            set
            {
                if (m_SpatialinterfaceState == value)
                    return;

                m_SpatialinterfaceState = value;
                m_SpatialMenuUi.spatialinterfaceState = value;

                switch (m_SpatialinterfaceState)
                {
                    case SpatialinterfaceState.navigatingTopLevel:
                        m_ContinuousDirectionalVelocityTracker.Initialize(this.RequestRayOriginFromNode(Node.LeftHand).position);
                        break;
                    case SpatialinterfaceState.navigatingSubMenuContent:
                        SetSpatialScrollStartingConditions(m_CurrentSpatialActionMapInput.localPosition.vector3, m_CurrentSpatialActionMapInput.localRotationQuaternion.quaternion);
                        DisplayHighlightedSubMenuContents();
                        break;
                }
            }
        }

        public Transform rayOrigin { get; set; }
        public Node node { get; set; }

        //IMenu interface members
        public MenuHideFlags menuHideFlags { get; set; }
        public GameObject menuContent { get; private set; }
        public Bounds localBounds { get; private set; }
        public int priority { get; private set; }

        // Action Map interface members
        public ActionMap actionMap { get { return m_ActionMap; } set { m_ActionMap = value; } }
        public bool ignoreActionMapInputLocking { get; private set; }

        // IDetectSpatialInput implementation
        public bool pollingSpatialInputType { get; set; }

        // Spatial scroll interface members
        public SpatialScrollModule.SpatialScrollData spatialScrollData { get; set; }
        public Transform spatialScrollOrigin { get; set; }
        public Vector3 spatialScrollStartPosition { get; set; }
        public float spatialQuickToggleDuration { get { return k_SpatialQuickToggleDuration; } }
        public float allowSpatialQuickToggleActionBeforeThisTime { get; set; }

        public static readonly List<ISpatialMenuProvider> s_SpatialMenuProviders = new List<ISpatialMenuProvider>();

        public bool beingMoved
        {
            set
            {
                if (m_BeingMoved != value)
                    return;

                m_BeingMoved = value;
                m_SpatialMenuUi.beingMoved = value;
            }
        }

        public class SpatialMenuElement
        {
            public SpatialMenuElement(string name, Sprite icon, string tooltipText, Action correspondingFunction)
            {
                this.name = name;
                this.icon = icon;
                this.tooltipText = tooltipText;
                this.correspondingFunction = correspondingFunction;
                //this.spatialUIMenuElement = element;
            }

            public string name { get; set; }

            public Sprite icon { get; set; }

            public Action correspondingFunction { get; private set; }

            public string tooltipText { get; private set; }

            public ISpatialMenuElement VisualElement { get; set; }
        }

        public void Setup()
        {
            CreateUI();
        }

        void Update()
        {
            return;
            if (m_SpatialMenuUi != null && m_SpatialMenuUi.directorBeyondHomeSectionDuration)
            {
                //StopAllCoroutines();
                //HideSubMenu();
                //allowAdaptivePositioning = false;
                //gameObject.SetActive(m_Visible);
            }
        }

        void CreateUI()
        {
            if (m_SpatialMenuUi == null)
            {
                var ui = ObjectUtils.Instantiate(_mSpatialMenuUiPrefab.gameObject, VRView.cameraRig);
                m_SpatialMenuUi = ui.GetComponent<SpatialMenuUI>();
                this.ConnectInterfaces(m_SpatialMenuUi);
                m_SpatialMenuUi.spatialMenuData = s_SpatialMenuData; // set shared reference to menu name/type, elements, and highlighted state
                m_SpatialMenuUi.Setup();
                SpatialMenuUI.spatialMenuProviders = s_SpatialMenuProviders;
            }

            visible = false;
        }

        void RefreshProviderData()
        {
            foreach (var provider in s_SpatialMenuProviders)
            {
                foreach (var menuData in provider.spatialMenuData)
                {
                    // Prevent menus/tools/etc that are instantiated multiple times from adding their contents to the Spatial Menu
                    if (!s_SpatialMenuData.Where(existingData => String.Equals(existingData.spatialMenuName, menuData.spatialMenuName)).Any())
                    {
                        Debug.LogWarning("Adding a spatial menu collection of type : " + menuData.spatialMenuName);
                        s_SpatialMenuData.Add(menuData);
                    }
                }
            }
        }

        public static void AddProvider(ISpatialMenuProvider provider)
        {
            //Type providerType = provider.GetType();

            if (s_SpatialMenuProviders.Contains(provider))
            {
                Debug.LogWarning("Cannot add multiple menus of the same type to the SpatialUI");
                return;
            }

            /*
            foreach (var collectionsProvider in s_SpatialMenuProviders)
            {
                var type = collectionsProvider.GetType();
                if (type == providerType)
                {
                    Debug.LogWarning("Cannot add multiple menus of the same type to the SpatialUI");
                    return;
                }
            }
            */

            Debug.LogWarning("Adding a provider");
            s_SpatialMenuProviders.Add(provider);

            if (provider.spatialMenuData.Count == 0)
                Debug.LogWarning("No spatial menu data was found when adding a new Spatial Menu provider");

            foreach (var menuElementSet in provider.spatialMenuData)
            {
                Debug.LogWarning("Adding a spatial menu collection of type : " + menuElementSet.spatialMenuName);
                //var sectionNameToMenuElementSetToHighlightedState = new Tuple<string, List<SpatialMenuElement>, bool>(menuElementSet.Key, menuElementSet.Value, false);
                s_SpatialMenuData.Add(menuElementSet);
            }

            //m_ProviderToMenuElements[provider] = null;

            //instantiatedPrefab.transform.SetParent(m_HomeMenuContainer);
            //instantiatedPrefab.localRotation = Quaternion.identity;
            //instantiatedPrefab.localPosition = Vector3.zero;
            //instantiatedPrefab.localScale = Vector3.one;

            //m_MenuTitleText.text = provider.spatialMenuName;
            //UpdateSectionNames();
        }

        /*
        public void RemoveProvider(ISpatialMenuProvider provider)
        {
            Debug.LogError("Removing a provider");
            if (m_spatialMenuProviders.Contains(provider))
            {
                Debug.LogWarning("Cannot add duplicates to the spatial menu provider collection.");
                m_spatialMenuProviders.Remove(provider);
            }
        }
        */

        void Reset()
        {
            return;

            DisplayHomeSectionContents();

            this.Pulse(Node.None, m_MenuOpenPulse);

            m_RotationVelocityTracker.Initialize(this.RequestRayOriginFromNode(Node.LeftHand).localRotation);
            m_ContinuousDirectionalVelocityTracker.Initialize(this.RequestRayOriginFromNode(Node.LeftHand).position);
        }

        void SetSpatialScrollStartingConditions(Vector3 localPosition, Quaternion localRotation)
        {
            node = Node.LeftHand; // TODO: fetch node that initiated the display of the spatial ui
            m_HomeSectionSpatialScrollStartLocalPosition = localPosition;
            m_InitialSpatialLocalRotation = localRotation; // Cache the current starting rotation, current deltaAngle will be calculated relative to this rotation

            if (m_HighlightedMenuElements != null)
            {
                // TODO: set the spatial scroll origin based on the node that initiates the display of the SpatialUI
                spatialScrollOrigin = this.RequestRayOriginFromNode(Node.LeftHand);
                spatialScrollStartPosition = spatialScrollOrigin.position;
                var elementCount = m_HighlightedMenuElements.Count;
                spatialScrollData = this.PerformSpatialScroll(node, spatialScrollStartPosition,
                spatialScrollOrigin.position, k_SpatialScrollVectorLength, elementCount, elementCount);
            }
        }

        void DisplayHomeSectionContents()
        {
            spatialinterfaceState = SpatialinterfaceState.navigatingTopLevel;
        }

        void DisplayHighlightedSubMenuContents()
        {
            m_ContinuousDirectionalVelocityTracker.Initialize(this.RequestRayOriginFromNode(Node.LeftHand).position);
            this.Pulse(Node.None, m_MenuOpenPulse);

            m_MenuEntranceStartTime = Time.realtimeSinceStartup;
            //m_HomeTextBackgroundTransform.localScale = new Vector3(m_HomeTextBackgroundOriginalLocalScale.x, m_HomeTextBackgroundOriginalLocalScale.y * 6, 1f);

            m_SpatialMenuUi.DisplayHighlightedSubMenuContents();

            // Spatial Scrolling setup
            //spatialScrollStartPosition = spatialScrollOrigin.position;
            //allowSpatialQuickToggleActionBeforeThisTime = Time.realtimeSinceStartup + spatialQuickToggleDuration;
            //this.SetSpatialHintControlNode(node);
            //m_ToolsMenuUI.spatiallyScrolling = true; // Triggers the display of the directional hint arrows
            //consumeControl(toolslMenuInput.show);
            //consumeControl(toolslMenuInput.select);

            // Assign initial SpatialScrollData; begin scroll
            //spatialScrollData = this.PerformSpatialScroll(node, spatialScrollStartPosition, spatialScrollOrigin.position, 0.325f, subMenuElementCount, subMenuElementCount);

            //HideScrollFeedback();
            //ShowMenuFeedback();
        }

        void ReturnToPreviousMenuLevel()
        {
            this.Pulse(Node.None, m_NavigateBackPulse);
            m_MenuEntranceStartTime = Time.realtimeSinceStartup;
            DisplayHomeSectionContents();

            Debug.LogWarning("SpatialMenu : <color=green>Above wrist return threshold</color>");
        }

        public void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl)
        {
            //Debug.Log("processing input in SpatialUI");

            const float kSubMenuNavigationTranslationTriggerThreshold = 0.075f;
            m_CurrentSpatialActionMapInput = (SpatialMenuInput)input;

            // This block is only processed after a frame with both trigger buttons held has been detected
            if (spatialScrollData != null && m_CurrentSpatialActionMapInput.cancel.wasJustPressed)
            {
                consumeControl(m_CurrentSpatialActionMapInput.cancel);
                consumeControl(m_CurrentSpatialActionMapInput.show);
                consumeControl(m_CurrentSpatialActionMapInput.select);
                //consumeControl(actionMapInput.localPosition);
                //consumeControl(actionMapInput.localRotationQuaternion);

                /*
                //OnButtonClick();
                //CloseMenu(); // Also ends spatial scroll
                //m_ToolsMenuUI.allButtonsVisible = false;
                */
            }

            // Prevent input processing while moving
            if (m_BeingMoved)
            {
                consumeControl(m_CurrentSpatialActionMapInput.show);
                consumeControl(m_CurrentSpatialActionMapInput.select);

                //TODO restore this functionality.  It resets the starting position when being moved, but currently breaks when initially opening the menu
                if (m_SpatialinterfaceState != SpatialinterfaceState.hidden && Vector3.Magnitude(m_HomeSectionSpatialScrollStartLocalPosition - m_CurrentSpatialActionMapInput.localPosition.vector3) > kSubMenuNavigationTranslationTriggerThreshold)
                    m_HomeSectionSpatialScrollStartLocalPosition = m_CurrentSpatialActionMapInput.localPosition.vector3;
            }

            // Detect the initial activation of the relevant Spatial input
            if ((m_CurrentSpatialActionMapInput.show.wasJustPressed && m_CurrentSpatialActionMapInput.select.wasJustPressed) ||
                (m_CurrentSpatialActionMapInput.show.wasJustPressed && m_CurrentSpatialActionMapInput.select.isHeld) ||
                (m_CurrentSpatialActionMapInput.show.isHeld && m_CurrentSpatialActionMapInput.select.wasJustPressed))
            {
                spatialinterfaceState = SpatialinterfaceState.navigatingTopLevel;
                SetSpatialScrollStartingConditions(m_CurrentSpatialActionMapInput.localPosition.vector3, m_CurrentSpatialActionMapInput.localRotationQuaternion.quaternion);
                //Reset();
            }

            if (m_CurrentSpatialActionMapInput.show.isHeld && m_SpatialinterfaceState != SpatialinterfaceState.hidden)
            {
                m_RotationVelocityTracker.Update(m_CurrentSpatialActionMapInput.localRotationQuaternion.quaternion, Time.deltaTime);
                if (m_RotationVelocityTracker.rotationStrength > 500)
                {
                    m_SpatialMenuUi.spatialInterfaceInputMode = SpatialMenuUI.SpatialInterfaceInputMode.Ray;
                }

                m_ContinuousDirectionalVelocityTracker.Update(m_CurrentSpatialActionMapInput.localPosition.vector3, Time.unscaledDeltaTime);
                //Debug.Log("<color=green>Continuous Direction strength " + m_ContinuousDirectionalVelocityTracker.directionalDivergence + "</color>");

                consumeControl(m_CurrentSpatialActionMapInput.cancel);
                consumeControl(m_CurrentSpatialActionMapInput.show);
                consumeControl(m_CurrentSpatialActionMapInput.select);

                m_Transitioning = Time.realtimeSinceStartup - m_MenuEntranceStartTime > k_MenuSectionBlockedTransitionTimeWindow; // duration for which input is not taken into account when menu swapping
                visible = true;

                /*
                if (!inFocus)
                {
                    Debug.LogWarning("<color=red>BLOCKING INPUT while out of focus</color>");
                    // Prevent input from changing the state of the menu while the menu is not in focus
                    spatialScrollStartPosition = actionMapInput.localPosition.vector3;
                    m_InitialSpatialLocalRotation = actionMapInput.localRotationQuaternion.quaternion;
                    return; // Don't process further input if the menu is not in focus
                }
                */

                // TODO: check the node currently controlling the spatial UI, don't hard set on left hand
                var spatialInputType = this.GetSpatialInputTypeForNode(Node.LeftHand);
                //Debug.LogWarning("SpatialUI current input type : " + spatialInputType);

                //if (spatialInputType == SpatialInputType.StateChangedThisFrame)
                    //Debug.Log("<color=green>SpatialUI state changed this frame!!</color>");

                var inputLocalRotation = m_CurrentSpatialActionMapInput.localRotationQuaternion.quaternion;
                var ghostDeviceRotation = inputLocalRotation * Quaternion.Inverse(m_InitialSpatialLocalRotation);
                m_SpatialMenuUi.UpdateGhostDeviceRotation(ghostDeviceRotation);

                /*
                if (m_Transitioning && m_State == State.navigatingSubMenuContent && Mathf.Abs(Mathf.DeltaAngle(m_InitialSpatialLocalRotation.x, actionMapInput.localRotationQuaternion.quaternion.x)) > k_WristReturnRotationThreshold)
                {
                    //Debug.LogWarning("<color=green>" + Mathf.DeltaAngle(m_InitialSpatialLocalRotation.z, actionMapInput.localRotationQuaternion.quaternion.z) + "</color>");
                    SetSpatialScrollStartingConditions(actionMapInput.localPosition.vector3, actionMapInput.localRotationQuaternion.quaternion);
                    ReturnToPreviousMenuLevel();
                    return;
                }
                */

                if (m_Transitioning && m_SpatialinterfaceState == SpatialinterfaceState.navigatingSubMenuContent && m_ContinuousDirectionalVelocityTracker.directionalDivergence > 0.08f)
                {
                    //Debug.LogWarning("<color=green>" + Mathf.DeltaAngle(m_InitialSpatialLocalRotation.z, actionMapInput.localRotationQuaternion.quaternion.z) + "</color>");
                    SetSpatialScrollStartingConditions(m_CurrentSpatialActionMapInput.localPosition.vector3, m_CurrentSpatialActionMapInput.localRotationQuaternion.quaternion);
                    ReturnToPreviousMenuLevel();
                    return;
                }

                if (m_SpatialinterfaceState == SpatialinterfaceState.navigatingTopLevel && Vector3.Magnitude(m_HomeSectionSpatialScrollStartLocalPosition - m_CurrentSpatialActionMapInput.localPosition.vector3) > kSubMenuNavigationTranslationTriggerThreshold)
                {
                    if (m_Transitioning)
                    {
                        var x = Vector3.Magnitude(m_HomeSectionSpatialScrollStartLocalPosition - m_CurrentSpatialActionMapInput.localPosition.vector3);
                        //Debug.LogError("<color=green>"+ x + "</color>");
                        Debug.Log("Crossed translation threshold");
                        spatialinterfaceState = SpatialinterfaceState.navigatingSubMenuContent;
                    }

                    return;
                }

                // utilize the YAW rotation of the input device to cycle through menu items
                // Scale the cycling speed based on the dot-base divergence from the initial starting angle
                // Will need to consider how to handle a user starting at a steep angle initially, baesd upon how far they scroll in the opposite direction.
                // In other words, if the user rotates beyond the max estimated threshold, we offset the initial starting angle by that amount, so when returning their rotation to the original extreme angle
                // They will have offset their "neutral" rotation position, and have newfound room to rotate/advance in the original "extreme" rotation direction
                if (m_SpatialinterfaceState != SpatialinterfaceState.navigatingSubMenuContent)
                {
                    // The "roll" rotation expected on the z is polled for via the X in the action map...???
                    const float kSectionSpacingBuffer = 0.05f;
                    var localZRotationDelta = Mathf.DeltaAngle(m_InitialSpatialLocalRotation.y, m_CurrentSpatialActionMapInput.localRotationQuaternion.quaternion.y);//Mathf.Abs(m_InitialSpatialLocalZRotation - currentLocalZRotation);// Mathf.Clamp((m_InitialSpatialLocalZRotation + 1) + currentLocalZRotation, 0f, 2f);
                    var highlightedPosition = 0;
                    //Debug.LogWarning("<color=green>" + Mathf.DeltaAngle(m_InitialSpatialLocalRotation.x, actionMapInput.localRotationQuaternion.quaternion.x) + "</color>");
                    if (localZRotationDelta > kSectionSpacingBuffer) // Rotating (relatively) leftward
                    {
                        highlightedPosition = 0;
                    }
                    else if (localZRotationDelta < -kSectionSpacingBuffer)
                    {
                        highlightedPosition = 1;
                    }

                    if (!s_SpatialMenuData[highlightedPosition].highlighted)
                    {
                        highlightedMenuElements = s_SpatialMenuData[highlightedPosition].spatialMenuElements;
                        this.Pulse(Node.None, m_HighlightMenuElementPulse);
                        for (int i = 0; i < s_SpatialMenuData.Count; ++i)
                        {
                            s_SpatialMenuData[i].highlighted = i == highlightedPosition;
                        }
                        m_SpatialMenuUi.HighlightHomeSectionMenuElement(highlightedPosition);
                    }
                }

                if (m_SpatialinterfaceState == SpatialinterfaceState.navigatingSubMenuContent)
                {
                    if (m_HighlightedMenuElements != null)
                    {
                        var menuElementCount = m_HighlightedMenuElements.Count;
                        spatialScrollData = this.PerformSpatialScroll(node, spatialScrollStartPosition, spatialScrollOrigin.position, k_SpatialScrollVectorLength, menuElementCount, menuElementCount);
                        var normalizedRepeatingPosition = spatialScrollData.normalizedLoopingPosition;
                        if (!Mathf.Approximately(normalizedRepeatingPosition, 0f))
                        {
                            /*
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
                            */

                            //Debug.LogWarning("Performing spatial scrolling of sub-menu contents");
                            m_HighlightedMenuElementPosition = (int) (menuElementCount * normalizedRepeatingPosition);
                            m_SpatialMenuUi.HighlightSingleElementInCurrentMenu(m_HighlightedMenuElementPosition);
                            //m_ToolsMenuUI.HighlightSingleButtonWithoutMenu((int)(buttonCount * normalizedRepeatingPosition) + 1);
                        }
                        }
                }

                /* Working Z-rotation based cycling through menu elements
                // Cycle through top-level sections, before opening a corresponding sub-menu
                if (m_State != State.navigatingSubMenuContent)
                {
                    // The "roll" rotation expected on the z is polled for via the X in the action map...???
                    const float kSectionSpacingBuffer = 0.05f;
                    var localZRotationDelta = Mathf.DeltaAngle(m_InitialSpatialLocalRotation.z, actionMapInput.localRotationQuaternion.quaternion.z);//Mathf.Abs(m_InitialSpatialLocalZRotation - currentLocalZRotation);// Mathf.Clamp((m_InitialSpatialLocalZRotation + 1) + currentLocalZRotation, 0f, 2f);
                    //Debug.LogWarning("<color=green>" + Mathf.DeltaAngle(m_InitialSpatialLocalRotation.x, actionMapInput.localRotationQuaternion.quaternion.x) + "</color>");
                    if (localZRotationDelta > kSectionSpacingBuffer) // Rotating (relatively) leftward
                    {
                        HighlightHomeSectionMenuElement(m_spatialMenuProviders[0]);
                    }
                    else if (localZRotationDelta < -kSectionSpacingBuffer)
                    {
                        HighlightHomeSectionMenuElement(m_spatialMenuProviders[1]);
                    }
                }
                */

                return;
            }

            if (!m_CurrentSpatialActionMapInput.show.isHeld && !m_CurrentSpatialActionMapInput.select.isHeld)
            {
                visible = false;
                return;
            }

            /*
            if (spatialScrollData == null && (actionMapInput.show.wasJustPressed || actionMapInput.show.isHeld) && actionMapInput.select.wasJustPressed)
            {
                spatialScrollStartPosition = spatialScrollOrigin.position;
                allowSpatialQuickToggleActionBeforeThisTime = Time.realtimeSinceStartup + spatialQuickToggleDuration;
                consumeControl(actionMapInput.show);
                consumeControl(actionMapInput.select);

                // Assign initial SpatialScrollData; begin scroll
                spatialScrollData = this.PerformSpatialScroll(node, spatialScrollStartPosition, spatialScrollOrigin.position, 0.325f, m_ToolsMenuUI.buttons.Count, m_ToolsMenuUI.maxButtonCount);

                HideScrollFeedback();
                ShowMenuFeedback();
            }
            else if (spatialScrollData != null && actionMapInput.show.isHeld)
            {
                consumeControl(actionMapInput.show);
                consumeControl(actionMapInput.select);

                // Attempt to close a button, if a scroll has passed the trigger threshold
                if (spatialScrollData != null && actionMapInput.select.wasJustPressed)
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
                spatialScrollData = this.PerformSpatialScroll(node, spatialScrollStartPosition, spatialScrollOrigin.position, 0.325f, m_ToolsMenuUI.buttons.Count, m_ToolsMenuUI.maxButtonCount);
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
            else if (spatialScrollData != null && !actionMapInput.show.isHeld && !actionMapInput.select.isHeld)
            {
                consumeControl(actionMapInput.show);
                consumeControl(actionMapInput.select);

                if (spatialScrollData != null && spatialScrollData.passedMinDragActivationThreshold)
                {
                    m_ToolsMenuUI.SelectHighlightedButton();
                }
                else if (Time.realtimeSinceStartup < allowSpatialQuickToggleActionBeforeThisTime)
                {
                    // Allow for single press+release to cycle through tools
                    m_ToolsMenuUI.SelectNextExistingToolButton();
                    OnButtonClick();
                }

                CloseMenu();
            }
            */
        }
    }
}
#endif
