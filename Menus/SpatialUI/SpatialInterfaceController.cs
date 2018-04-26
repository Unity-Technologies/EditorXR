#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
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
    public class SpatialInterfaceController : MonoBehaviour, IAdaptPosition, IControlSpatialScrolling, IInstantiateUI,
        IUsesNode, IUsesRayOrigin, ISelectTool, IDetectSpatialInputType,
        IControlHaptics, INodeToRay
    {
        // TODO expose as a user preference, for spatial UI distance
        const float k_DistanceOffset = 0.75f;
        const float k_AllowedGazeDivergence = 45f;
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
        SpatialInterfaceUI m_SpatialInterfaceUIPrefab;

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

        SpatialInterfaceUI m_SpatialInterfaceUI;
        SpatialinterfaceState m_SpatialinterfaceState;

        bool m_Visible;
        bool m_BeingMoved;
        bool m_InFocus;
        Vector3 m_HomeSectionSpatialScrollStartLocalPosition;
        bool m_Transitioning;

        // "Rotate wrist to return" members
        float m_StartingWristXRotation;
        float m_WristReturnVelocity;

        // Menu entrance start time
        float m_MenuEntranceStartTime;

        // Spatial rotation members
        Quaternion m_InitialSpatialLocalRotation;

        ISpatialMenuProvider m_HighlightedTopLevelMenuProvider;

        readonly Dictionary<ISpatialMenuProvider, SpatialUIMenuElement> m_ProviderToMenuElements = new Dictionary<ISpatialMenuProvider, SpatialUIMenuElement>();

        readonly List<SpatialUIMenuElement> currentlyDisplayedMenuElements = new List<SpatialUIMenuElement>();
        int m_HighlightedButtonPosition; // element position amidst the currentlyDisplayedMenuElements

        RotationVelocityTracker m_RotationVelocityTracker = new RotationVelocityTracker();
        ContinuousDirectionalVelocityTracker m_ContinuousDirectionalVelocityTracker = new ContinuousDirectionalVelocityTracker();

        private bool visible
        {
            get { return m_Visible; }

            set
            {
                if (m_Visible == value)
                    return;

                m_Visible = value;

                if (m_Visible)
                {
                    pollingSpatialInputType = true;
                    //gameObject.SetActive(true);  MOVED TO SPATIAL UI View
                }
                else
                {
                    if (m_HighlightedTopLevelMenuProvider != null &&
                        m_HighlightedTopLevelMenuProvider.spatialTableElements.Count > 0 &&
                        m_HighlightedTopLevelMenuProvider.spatialTableElements[m_HighlightedButtonPosition] != null &&
                        m_HighlightedTopLevelMenuProvider.spatialTableElements[m_HighlightedButtonPosition].correspondingFunction != null)
                    {
                        m_HighlightedTopLevelMenuProvider.spatialTableElements[m_HighlightedButtonPosition].correspondingFunction();
                        this.Pulse(Node.None, m_HighlightMenuElementPulse);
                    }

                    this.Pulse(Node.None, m_MenuClosePulse);
                    pollingSpatialInputType = false;
                    spatialinterfaceState = SpatialinterfaceState.hidden;
                }
            }
        }

        private SpatialinterfaceState spatialinterfaceState
        {
            set
            {
                if (m_SpatialinterfaceState == value)
                    return;

                m_SpatialinterfaceState = value;
                m_SpatialInterfaceUI.spatialinterfaceState = value;
            }
        }

        public Transform rayOrigin { get; set; }
        public Node node { get; set; }

        // Action Map interface members
        public ActionMap actionMap { get { return m_ActionMap; } }
        public bool ignoreActionMapInputLocking { get; private set; }

        // IDetectSpatialInput implementation
        public bool pollingSpatialInputType { get; set; }

        // Spatial scroll interface members
        public SpatialScrollModule.SpatialScrollData spatialScrollData { get; set; }
        public Transform spatialScrollOrigin { get; set; }
        public Vector3 spatialScrollStartPosition { get; set; }
        public float spatialQuickToggleDuration { get { return k_SpatialQuickToggleDuration; } }
        public float allowSpatialQuickToggleActionBeforeThisTime { get; set; }

        // Adaptive position related members
        public Transform adaptiveTransform { get { return transform; } }
        public float allowedDegreeOfGazeDivergence { get { return k_AllowedGazeDivergence; } }
        public float distanceOffset { get { return k_DistanceOffset; } }
        public AdaptivePositionModule.AdaptivePositionData adaptivePositionData { get; set; }
        public bool allowAdaptivePositioning { get; private set; }

        public readonly List<ISpatialMenuProvider> m_spatialMenuProviders = new List<ISpatialMenuProvider>();

        public bool inFocus
        {
            get { return m_InFocus; }
            set
            {
                if (m_InFocus == value)
                    return;

                //if (value != m_InFocus)
                    //this.RestartCoroutine(ref m_InFocusCoroutine, AnimateFocusVisuals());

                m_InFocus = value;
                m_SpatialInterfaceUI.inFocus = value;
            }
        }

        public bool beingMoved
        {
            set
            {
                if (m_BeingMoved != value)
                    return;

                m_BeingMoved = value;
                m_SpatialInterfaceUI.beingMoved = value;
            }
        }

        public class SpatialUITableElement
        {
            public SpatialUITableElement(string name, Sprite icon, Action correspondingFunction)
            {
                this.name = name;
                this.icon = icon;
                this.correspondingFunction = correspondingFunction;
                //this.spatialUIMenuElement = element;
            }

            public string name { get; set; }

            public Sprite icon { get; set; }

            public Action correspondingFunction { get; private set; }

            //public SpatialUIMenuElement spatialUIMenuElement { get; private set; }
        }

        void Start()
        {
            m_SpatialInterfaceUI = this.InstantiateUI(m_SpatialInterfaceUIPrefab.gameObject).GetComponent<SpatialInterfaceUI>();
            ConnectInterfaces(m_SpatialInterfaceUI);
            m_SpatialInterfaceUI.Setup();
        }

        private void ConnectInterfaces(SpatialInterfaceUI spatialInterfaceUI)
        {
            throw new NotImplementedException();
        }

        void Update()
        {
            if (m_SpatialInterfaceUI.directorBeyondHomeSectionDuration)
            {
                StopAllCoroutines();
                //HideSubMenu();
                allowAdaptivePositioning = false;
                //gameObject.SetActive(m_Visible);
            }
            else if (m_SpatialinterfaceState == SpatialinterfaceState.navigatingSubMenuContent)
            {
                // Scale background based on number of sub-menu elements
                var targetScale = m_HighlightedTopLevelMenuProvider != null ? m_HighlightedTopLevelMenuProvider.spatialTableElements.Count * 1.05f : 1f;
                var timeMultiplier = 24;
                if (m_HomeTextBackgroundInnerTransform.localScale.y < targetScale)
                {
                    if (m_HomeTextBackgroundInnerTransform.localScale.y + Time.unscaledDeltaTime * timeMultiplier > targetScale)
                    {
                        m_HomeTextBackgroundInnerTransform.localScale = new Vector3(1f, targetScale, 1f);
                        m_SubMenuContentsCanvasGroup.alpha = 1f;
                        return;
                    }

                    var newScale = new Vector3(m_HomeTextBackgroundInnerTransform.localScale.x, m_HomeTextBackgroundInnerTransform.localScale.y + Time.unscaledDeltaTime * timeMultiplier, m_HomeTextBackgroundInnerTransform.localScale.z);
                    m_HomeTextBackgroundInnerTransform.localScale = newScale;
                    m_SubMenuContentsCanvasGroup.alpha += Time.unscaledDeltaTime;
                }
                else
                {
                    return;
                    m_HomeTextBackgroundInnerTransform.localScale = new Vector3(1f, targetScale, 1f);
                    m_SubMenuContentsCanvasGroup.alpha = 1f;
                }
            }
            else if (m_SpatialinterfaceState == SpatialinterfaceState.navigatingTopLevel)
            {
                var targetScale = 1f;
                var timeMultiplier = 24;
                if (m_HomeTextBackgroundInnerTransform.localScale.y > targetScale)
                {
                    if (m_HomeTextBackgroundInnerTransform.localScale.y - Time.unscaledDeltaTime * timeMultiplier < targetScale)
                    {
                        m_HomeTextBackgroundInnerTransform.localScale = new Vector3(1f, targetScale, 1f);
                        m_SubMenuContentsCanvasGroup.alpha = 0f;
                        var deleteOldChildren = m_SubMenuContainer.GetComponentsInChildren<Transform>().Where((x) => x != m_SubMenuContainer);
                        if (deleteOldChildren.Count() > 0)
                        {
                            foreach (var child in deleteOldChildren)
                            {
                                if (child != null && child.gameObject != null)
                                    ObjectUtils.Destroy(child.gameObject);
                            }
                        }
                        return;
                    }

                    var newScale = new Vector3(m_HomeTextBackgroundInnerTransform.localScale.x, m_HomeTextBackgroundInnerTransform.localScale.y - Time.unscaledDeltaTime * timeMultiplier, m_HomeTextBackgroundInnerTransform.localScale.z);
                    m_HomeTextBackgroundInnerTransform.localScale = newScale;
                    m_SubMenuContentsCanvasGroup.alpha -= Time.unscaledDeltaTime * 10;
                }
                else
                {
                    //m_HomeTextBackgroundInnerTransform.localScale = new Vector3(1f, targetScale, 1f);
                    //m_SubMenuContentsCanvasGroup.alpha = 0f;


                }
            }
        }

        public void AddProvider(ISpatialMenuProvider provider)
        {
            Type providerType = provider.GetType();
            foreach (var collectionProvider in m_spatialMenuProviders)
            {
                var type = collectionProvider.GetType();
                if (type == providerType)
                {
                    Debug.LogWarning("Cannot add multiple menus of the same type to the SpatialUI");
                    return;
                }
            }

            Debug.LogWarning("Adding a provider : " + provider.spatialMenuName);
            m_spatialMenuProviders.Add(provider);

            var instantiatedPrefab = ObjectUtils.Instantiate(m_SpatialInterfaceUI.menuElementPrefab).transform as RectTransform;
            var providerMenuElement = instantiatedPrefab.GetComponent<SpatialUIMenuElement>();
            providerMenuElement.Setup(instantiatedPrefab, m_SpatialInterfaceUI.homeMenuContainer, () => Debug.LogWarning("Setting up : " + provider.spatialMenuName), provider.spatialMenuName);
            m_ProviderToMenuElements.Add(provider, providerMenuElement);

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

            if (m_HighlightedTopLevelMenuProvider != null)
            {
                // TODO: set the spatial scroll origin based on the node that initiates the display of the SpatialUI
                spatialScrollOrigin = this.RequestRayOriginFromNode(Node.LeftHand);
                spatialScrollStartPosition = spatialScrollOrigin.position;
                var elementCount = m_HighlightedTopLevelMenuProvider.spatialTableElements.Count;
                spatialScrollData = this.PerformSpatialScroll(node, spatialScrollStartPosition,
                spatialScrollOrigin.position, k_SpatialScrollVectorLength, elementCount, elementCount);
            }
        }

        void HighlightHomeSectionMenuElement(ISpatialMenuProvider provider)
        {
            this.Pulse(Node.None, m_HighlightMenuElementPulse);
            m_HomeSectionDescription.text = provider.spatialMenuDescription;
            m_HighlightedTopLevelMenuProvider = provider;

            foreach (var kvp in m_ProviderToMenuElements)
            {
                var key = kvp.Key;
                var targetSize = key == provider ? Vector3.one : Vector3.one * 0.5f;
                kvp.Value.transform.localScale = targetSize;
            }
        }

        void DisplayHomeSectionContents()
        {
            m_ContinuousDirectionalVelocityTracker.Initialize(this.RequestRayOriginFromNode(Node.LeftHand).position);
            m_SpatialUIGhostVisuals.SetPositionOffset(Vector3.zero);
            m_SpatialUIGhostVisuals.spatialInteractionType = SpatialUIGhostVisuals.SpatialInteractionType.touch;
            this.RestartCoroutine(ref m_HomeSectionTitlesBackgroundBordersTransitionCoroutine, AnimateTopAndBottomCenterBackgroundBorders(true));

            spatialinterfaceState = SpatialinterfaceState.navigatingTopLevel;

            // Proxy sub-menu/dynamicHUD menu element(s) display
            m_HomeTextBackgroundTransform.localScale = m_HomeTextBackgroundOriginalLocalScale;
            m_HomeSectionDescription.gameObject.SetActive(true);

            foreach (var kvp in m_ProviderToMenuElements)
            {
                var elementTransform = kvp.Value.transform;
                elementTransform.gameObject.SetActive(true);
                elementTransform.localScale = Vector3.one;
            }
        }

        void DisplayHighlightedSubMenuContents()
        {
            m_ContinuousDirectionalVelocityTracker.Initialize(this.RequestRayOriginFromNode(Node.LeftHand).position);
            this.Pulse(Node.None, m_MenuOpenPulse);
            this.RestartCoroutine(ref m_HomeSectionTitlesBackgroundBordersTransitionCoroutine, AnimateTopAndBottomCenterBackgroundBorders(false));
            m_MenuEntranceStartTime = Time.realtimeSinceStartup;
            spatialinterfaceState = SpatialinterfaceState.navigatingSubMenuContent;

            //m_HomeTextBackgroundTransform.localScale = new Vector3(m_HomeTextBackgroundOriginalLocalScale.x, m_HomeTextBackgroundOriginalLocalScale.y * 6, 1f);

            m_HomeSectionDescription.gameObject.SetActive(false);

            const float subMenuElementHeight = 0.022f; // TODO source height from individual sub-menu element height, not arbitrary value
            int subMenuElementCount = 0;
            foreach (var kvp in m_ProviderToMenuElements)
            {
                var key = kvp.Key;
                if (key == m_HighlightedTopLevelMenuProvider)
                {
                    // m_SubMenuText.text = m_HighlightedTopLevelMenuProvider.spatialTableElements[0].name;
                    // TODO display all sub menu contents here

                    currentlyDisplayedMenuElements.Clear();
                    var deleteOldChildren = m_SubMenuContainer.GetComponentsInChildren<Transform>().Where( (x) => x != m_SubMenuContainer);
                    foreach (var child in deleteOldChildren)
                    {
                        if (child != null && child.gameObject != null)
                            ObjectUtils.Destroy(child.gameObject);
                    }

                    foreach (var subMenuElement in m_HighlightedTopLevelMenuProvider.spatialTableElements)
                    {
                        ++subMenuElementCount;
                        var instantiatedPrefab = ObjectUtils.Instantiate(m_SpatialInterfaceUI.subMenuElementPrefab).transform as RectTransform;
                        var providerMenuElement = instantiatedPrefab.GetComponent<SpatialUIMenuElement>();
                        providerMenuElement.Setup(instantiatedPrefab, m_SpatialInterfaceUI.subMenuContainer, () => Debug.Log("Setting up SubMenu : " + subMenuElement.name), subMenuElement.name);
                        currentlyDisplayedMenuElements.Add(providerMenuElement);
                    }

                    //.Add(provider, providerMenuElement);
                    //instantiatedPrefab.transform.SetParent(m_SubMenuContainer);
                    //instantiatedPrefab.localRotation = Quaternion.identity;
                    //instantiatedPrefab.localPosition = Vector3.zero;
                    //instantiatedPrefab.localScale = Vector3.one;
                }

                kvp.Value.gameObject.SetActive(false);
            }

            var newGhostInputDevicePositionOffset = new Vector3(0f, subMenuElementHeight * subMenuElementCount, 0f);
            m_SpatialInterfaceUI.DisplayHighlightedSubMenuContents(newGhostInputDevicePositionOffset);

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

            Debug.LogWarning("SpatialUI : <color=green>Above wrist return threshold</color>");
        }

        public void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl)
        {
            //Debug.Log("processing input in SpatialUI");

            const float kSubMenuNavigationTranslationTriggerThreshold = 0.075f;
            var actionMapInput = (SpatialUIInput)input;

            // This block is only processed after a frame with both trigger buttons held has been detected
            if (spatialScrollData != null && actionMapInput.cancel.wasJustPressed)
            {
                consumeControl(actionMapInput.cancel);
                consumeControl(actionMapInput.show);
                consumeControl(actionMapInput.select);
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
                consumeControl(actionMapInput.show);
                consumeControl(actionMapInput.select);

                //TODO restore this functionality.  It resets the starting position when being moved, but currently breaks when initially opening the menu
                if (m_SpatialinterfaceState != SpatialinterfaceState.hidden && Vector3.Magnitude(m_HomeSectionSpatialScrollStartLocalPosition - actionMapInput.localPosition.vector3) > kSubMenuNavigationTranslationTriggerThreshold)
                    m_HomeSectionSpatialScrollStartLocalPosition = actionMapInput.localPosition.vector3;
            }

            // Detect the initial activation of the relevant Spatial input
            if ((actionMapInput.show.wasJustPressed && actionMapInput.select.wasJustPressed) ||
                (actionMapInput.show.wasJustPressed && actionMapInput.select.isHeld) ||
                (actionMapInput.show.isHeld && actionMapInput.select.wasJustPressed))
            {
                spatialinterfaceState = SpatialinterfaceState.navigatingTopLevel;
                SetSpatialScrollStartingConditions(actionMapInput.localPosition.vector3, actionMapInput.localRotationQuaternion.quaternion);
                Reset();
            }

            if (actionMapInput.show.isHeld && m_SpatialinterfaceState != SpatialinterfaceState.hidden)
            {
                m_RotationVelocityTracker.Update(actionMapInput.localRotationQuaternion.quaternion, Time.deltaTime);
                if (m_RotationVelocityTracker.rotationStrength > 500)
                {
                    m_SpatialInterfaceUI.spatialInterfaceInputMode = SpatialInterfaceUI.SpatialInterfaceInputMode.Ray;
                }

                m_ContinuousDirectionalVelocityTracker.Update(actionMapInput.localPosition.vector3, Time.unscaledDeltaTime);
                Debug.Log("<color=green>Continuous Direction strength " + m_ContinuousDirectionalVelocityTracker.directionalDivergence + "</color>");

                consumeControl(actionMapInput.cancel);
                consumeControl(actionMapInput.show);
                consumeControl(actionMapInput.select);

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
                Debug.LogWarning("SpatialUI current input type : " + spatialInputType);

                if (spatialInputType == SpatialInputType.StateChangedThisFrame)
                    Debug.Log("<color=green>SpatialUI state changed this frame!!</color>");

                var inputLocalRotation = actionMapInput.localRotationQuaternion.quaternion;
                var ghostDeviceRotation = inputLocalRotation * Quaternion.Inverse(m_InitialSpatialLocalRotation);
                m_SpatialInterfaceUI.UpdateGhostDeviceRotation(ghostDeviceRotation);

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
                    SetSpatialScrollStartingConditions(actionMapInput.localPosition.vector3, actionMapInput.localRotationQuaternion.quaternion);
                    ReturnToPreviousMenuLevel();
                    return;
                }

                if (m_SpatialinterfaceState == SpatialinterfaceState.navigatingTopLevel && Vector3.Magnitude(m_HomeSectionSpatialScrollStartLocalPosition - actionMapInput.localPosition.vector3) > kSubMenuNavigationTranslationTriggerThreshold)
                {
                    if (m_Transitioning)
                    {
                        var x = Vector3.Magnitude(m_HomeSectionSpatialScrollStartLocalPosition - actionMapInput.localPosition.vector3);
                        //Debug.LogError("<color=green>"+ x + "</color>");
                        Debug.Log("Crossed translation threshold");
                        SetSpatialScrollStartingConditions(actionMapInput.localPosition.vector3, actionMapInput.localRotationQuaternion.quaternion);
                        DisplayHighlightedSubMenuContents();
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
                    var localZRotationDelta = Mathf.DeltaAngle(m_InitialSpatialLocalRotation.y, actionMapInput.localRotationQuaternion.quaternion.y);//Mathf.Abs(m_InitialSpatialLocalZRotation - currentLocalZRotation);// Mathf.Clamp((m_InitialSpatialLocalZRotation + 1) + currentLocalZRotation, 0f, 2f);
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

                if (m_SpatialinterfaceState == SpatialinterfaceState.navigatingSubMenuContent)
                {
                    if (m_HighlightedTopLevelMenuProvider != null)
                    {
                        var menuElementCount = m_HighlightedTopLevelMenuProvider.spatialTableElements.Count;
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

                            m_HighlightedButtonPosition = (int) (menuElementCount * normalizedRepeatingPosition);
                            for (int i = 0; i < menuElementCount; ++i)
                            {
                                //var x = m_ProviderToMenuElements[m_HighlightedTopLevelMenuProvider];
                                currentlyDisplayedMenuElements[i].highlighted = i == m_HighlightedButtonPosition;
                                //m_HighlightedTopLevelMenuProvider.spatialTableElements[i].name = i == highlightedButtonPosition ? "Highlighted" : "Not";
                            }

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

            if (!actionMapInput.show.isHeld && !actionMapInput.select.isHeld)
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
