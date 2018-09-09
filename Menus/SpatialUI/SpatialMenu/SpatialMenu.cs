#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Menus;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// The SpatialMenu controller
    /// A SpatialMenu controller is spawned in EditorVR.Tools SpawnDefaultTools() function, for each proxy/input-device
    /// There is a singule static SpatialUI(view) that all SpatialMenu controllers direct
    /// </summary>
    [ProcessInput(2)] // Process input after the ProxyAnimator, but before other IProcessInput implementors
    public sealed class SpatialMenu : SpatialUIController, IInstantiateUI, IUsesNode, IUsesRayOrigin,
        ISelectTool, IConnectInterfaces, IControlHaptics, IControlInputIntersection, ISetManipulatorsVisible,
        ILinkedObject, IRayVisibilitySettings, ICustomActionMap, IUsesViewerScale
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
            /// Bool denoting that this element is currently highlighted as either a section title or a sub-menu element
            /// </summary>
            public bool highlighted { get; set; }

            /// <summary>
            /// Collection of elements with which to populate the corresponding spatial UI table/list/view
            /// </summary>
            public List<SpatialMenu.SpatialMenuElementContainer> spatialMenuElements { get; private set; }

            public SpatialMenuData(string menuName, string menuDescription, List<SpatialMenu.SpatialMenuElementContainer> menuElements)
            {
                spatialMenuName = menuName;
                spatialMenuDescription = menuDescription;
                spatialMenuElements = menuElements;
            }
        }

        static SpatialMenu s_ControllingSpatialMenu;
        static SpatialMenuUI s_SpatialMenuUi;
        static readonly List<SpatialMenuData> s_SpatialMenuData = new List<SpatialMenuData>();
        static readonly List<Transform> allSpatialMenuRayOrigins = new List<Transform>();
        static int m_SubMenuElementCount;
        static SpatialMenuData m_SubMenuData;

        int subMenuElementCount { get { return m_SubMenuData.spatialMenuElements.Count; } }

        public static readonly List<ISpatialMenuProvider> s_SpatialMenuProviders = new List<ISpatialMenuProvider>();

        public enum SpatialMenuState
        {
            hidden,
            navigatingTopLevel,
            navigatingSubMenuContent,
        }

        [SerializeField]
        SpatialMenuUI m_SpatialMenuUiPrefab;

        [SerializeField]
        ActionMap m_ActionMap;

        [Header("Haptic Pulses")]
        [SerializeField]
        HapticPulse m_MenuOpenPulse;

        [SerializeField]
        HapticPulse m_MenuClosePulse;

        [SerializeField]
        HapticPulse m_NavigateBackPulse;

        static SpatialMenuState s_SpatialMenuState;

        bool m_Visible;

        SpatialMenuInput m_CurrentSpatialActionMapInput;

        // Bool denoting that the input necessary to keep the SpatialMenu visible is currently being maintained
        bool m_SpatialInputHold;

        // Duration denoting how long the input value has been at default/neutral and thus is in deadzone or lifted
        float m_DefaultValueTime;

        // "Rotate wrist to return" members
        float m_StartingWristXRotation;
        float m_WristReturnVelocity;

        string m_HighlightedSectionNameKey;
        int m_HighlightedTopLevelMenuElementPosition;
        int m_HighlightedSubLevelMenuElementPosition;
        List<SpatialMenuElementContainer> m_DisplayedMenuElements;

        // Trigger + continued/held circular-input related fields
        Vector2 m_OriginalShowMenuCircularInputDirection;
        Vector3 m_UpdatingShowMenuCircularInputDirection;
        bool m_ShowMenuCircularInputCrossedRotationThresholdForSelection;
        float m_TotalShowMenuCircularInputRotation;
        Coroutine m_CircularTriggerSelectionCyclingCoroutine;
        Transform m_RayOrigin;

        List<SpatialMenuElementContainer> highlightedMenuElements
        {
            set
            {
                if (m_DisplayedMenuElements == value)
                    return;

                m_DisplayedMenuElements = value;
                s_SpatialMenuUi.highlightedMenuElements = m_DisplayedMenuElements;
            }
        }

        bool visible
        {
            set
            {
                if (m_Visible == value)
                    return;

                m_Visible = value;

                if (m_Visible)
                {
                    RefreshProviderData();
                    spatialMenuState = SpatialMenuState.navigatingTopLevel;
                }
                else
                {
                    // Don't animate a return to the top menu level if closing
                    if (s_ControllingSpatialMenu != null)
                        ReturnToPreviousMenuLevel();

                    this.Pulse(Node.None, m_MenuClosePulse);
                    spatialMenuState = SpatialMenuState.hidden;
                }
            }
        }

        SpatialMenuState spatialMenuState
        {
            set
            {
                if (s_SpatialMenuState == value)
                    return;

                s_SpatialMenuState = value;
                s_SpatialMenuUi.spatialMenuState = s_SpatialMenuState;
                switch (s_SpatialMenuState)
                {
                    case SpatialMenuState.navigatingTopLevel:
                        m_HighlightedSubLevelMenuElementPosition = -1;
                        m_SubMenuData = null;
                        break;
                    case SpatialMenuState.navigatingSubMenuContent:
                        m_SubMenuData = s_SpatialMenuData.Where(x => x.highlighted).First();
                        this.Pulse(Node.None, m_MenuOpenPulse);
                        break;
                    case SpatialMenuState.hidden:
                        sceneViewGizmosVisible = true;
                        m_CircularTriggerSelectionCyclingCoroutine = null;
                        m_CurrentSpatialActionMapInput = null;
                        break;
                }
            }
        }

        public Transform rayOrigin
        {
            get { return m_RayOrigin; }
            set
            {
                if (m_RayOrigin == value) // TODO: This will block addition to allSpatialMenuRayOrigins FIX!
                    return;

                m_RayOrigin = value;

                // All rayOrigins/devices having spawned a spatial menu are added to this collection
                // The rayorigins in this collection have their pointing direction compared against the spatial UI's
                // forward vector, in order to see if a ray origins that ISN'T currently controlling the spatial UI
                // has begun pointing at the spatial UI, which will override the input typs to ray-based interaction
                // (taking the opposite hand, and pointing it at the menu)

                if (!allSpatialMenuRayOrigins.Contains(m_RayOrigin))
                    allSpatialMenuRayOrigins.Add(m_RayOrigin);
            }
        }

        public Node node { get; set; }

        // Action Map interface members
        public ActionMap actionMap { get { return m_ActionMap; } set { m_ActionMap = value; } }
        public bool ignoreActionMapInputLocking { get; private set; }
        public List<ILinkedObject> linkedObjects { private get; set; }

        public class SpatialMenuElementContainer
        {
            public SpatialMenuElementContainer(string name, string tooltipText, Action<Node> correspondingFunction)
            {
                this.name = name;
                this.tooltipText = tooltipText;
                this.correspondingFunction = correspondingFunction;
            }

            public string name { get; set; }

            public Action<Node> correspondingFunction { get; private set; }

            public string tooltipText { get; private set; }

            public SpatialMenuElement VisualElement { get; set; }
        }

        public void Setup()
        {
            CreateUI();
        }

        void OnDestroy()
        {
            // Reset the applicable selection gizmo (SceneView) states
            sceneViewGizmosVisible = true;
        }

        void CreateUI()
        {
            if (s_SpatialMenuUi == null)
            {
                s_SpatialMenuUi = this.InstantiateUI(m_SpatialMenuUiPrefab.gameObject, VRView.cameraRig, rayOrigin: rayOrigin).GetComponent<SpatialMenuUI>();
                s_SpatialMenuUi.spatialMenuData = s_SpatialMenuData; // set shared reference to menu name/type, elements, and highlighted state
                s_SpatialMenuUi.Setup();
                s_SpatialMenuUi.returnToPreviousMenuLevel = ReturnToPreviousMenuLevel;
                s_SpatialMenuUi.changeMenuState = ChangeMenuState;
            }

            visible = false;
        }

        // Delegate function assigned to SpatialMenuUI's changeMenuState action
        // This action is performed when selecting SpatialMenu elements/buttons
        void ChangeMenuState(SpatialMenuState state)
        {
            spatialMenuState = state;
        }

        void RefreshProviderData()
        {
            foreach (var provider in s_SpatialMenuProviders)
            {
                foreach (var menuData in provider.spatialMenuData)
                {
                    // Prevent menus/tools/etc that are instantiated multiple times from adding their contents to the Spatial Menu
                    if (!s_SpatialMenuData.Where(existingData => String.Equals(existingData.spatialMenuName, menuData.spatialMenuName)).Any())
                        s_SpatialMenuData.Add(menuData);
                }
            }
        }

        public static void AddProvider(ISpatialMenuProvider provider)
        {
            if (s_SpatialMenuProviders.Contains(provider))
                return;

            s_SpatialMenuProviders.Add(provider);

            foreach (var menuElementSet in provider.spatialMenuData)
                s_SpatialMenuData.Add(menuElementSet);
        }

        void ReturnToPreviousMenuLevel()
        {
            if (s_SpatialMenuState == SpatialMenuState.navigatingSubMenuContent)
                this.Pulse(Node.None, m_NavigateBackPulse); // Only perform haptic pulse when not at the top-level of the UI

            spatialMenuState = SpatialMenuState.navigatingTopLevel;
            m_HighlightedTopLevelMenuElementPosition = -1;
        }

        bool IsAimingAtUI(Transform deviceTransform)
        {
            const float divergenceThreshold = 45f; // Allowed angular deviation of the device and UI
            var divergenceThresholdConvertedToDot = Mathf.Sin(Mathf.Deg2Rad * divergenceThreshold);
            var testVector = s_SpatialMenuUi.adaptiveTransform.position - deviceTransform.position; // Test device to UI source vector
            testVector.Normalize(); // Normalize, in order to retain expected dot values
            var inputDeviceForwardDirection = deviceTransform.forward;
            var angularComparison = Vector3.Dot(testVector, inputDeviceForwardDirection);

            // Circularly expand/inflate outward from the center, the allowed target/intersection area of the device ray & the UI on the += X-axis
            // This expanded target area will allow a device ray to enable external-ray-mode, with greater tolerance on the +- X-axis, but not the Y-axis
            // This retains the ability of the ray to be more easily pointed upward/downward in order to deactivate this mode, and go into other modes (SpatialSelect, etc)
            // During testing, this allowed for easier targeting of the UI via ray at expected times, better accommodating the expectations of testers)
            var deviceXOffsetInlocalSpace = Mathf.Abs(deviceTransform.InverseTransformVector(testVector).x - deviceTransform.localPosition.x);
            const float additiveXPositionOffsetShapingScalar = 3f; // Apply less when near the center of the UI, more towards the outer reach of an extended arm on the X
            const float xPositionOffsetFromCenterAdditiveScalar = 0.8f; // Lessen the amount added for better ergonomic shaping
            angularComparison += Mathf.Pow(deviceXOffsetInlocalSpace, additiveXPositionOffsetShapingScalar) * xPositionOffsetFromCenterAdditiveScalar;

            var isAimingAtUi = angularComparison > divergenceThresholdConvertedToDot;
            return isAimingAtUi;
        }

        public void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl)
        {
            m_CurrentSpatialActionMapInput = (SpatialMenuInput)input;
            if (s_ControllingSpatialMenu != null && s_ControllingSpatialMenu != this)
            {
                this.SetRayOriginEnabled(m_RayOrigin, true);

                // Perform custom logic for proxies (driving a SpatialMenuController) that didn't initiate the display of the SpatialMenu
                // Though they may need their input actions to drive certain SpatialMenu functionality
                var cancelJustPressed = CancelWasJustPressedTest(consumeControl);
                if (!cancelJustPressed) // Only process selection testing if cancel was not just pressed
                    SelectJustPressedTest(consumeControl);

                return;
            }

            var showMenuInputAction = m_CurrentSpatialActionMapInput.showMenu;
            var showMenuInputActionVector2 = showMenuInputAction.vector2;
            var showMenuInputActionVector2Normalized = showMenuInputAction.vector2.normalized;
            var positiveYInputAction = showMenuInputAction.positiveY;

            // count how long the input value has been default and thus is in deadzone or lifted
            if (showMenuInputAction.vector2 == default(Vector2))
                m_DefaultValueTime += Time.deltaTime;
            else
                m_DefaultValueTime = 0f;

            // release after the input has been default for a few frames.  This almost entirely prevents
            // the case where having your thumb in the middle of the pad causes default value and thus release
            if (m_DefaultValueTime >= 0.05f)
            {
                m_SpatialInputHold = false;
                EndDisplayOfMenu();
            }

            // Detect the initial activation of the relevant Spatial input, in order to display menu and own control with this SpatialMenuController
            if (positiveYInputAction.wasJustPressed && m_TotalShowMenuCircularInputRotation == 0)
            {
                s_ControllingSpatialMenu = this;
                m_OriginalShowMenuCircularInputDirection = showMenuInputActionVector2Normalized;
                m_UpdatingShowMenuCircularInputDirection = m_OriginalShowMenuCircularInputDirection;
                m_ShowMenuCircularInputCrossedRotationThresholdForSelection = false;

                m_SpatialInputHold = true;
                ConsumeControls(m_CurrentSpatialActionMapInput, consumeControl); // Select should only be consumed upon activation, so other UI can receive select events

                // Hide the scene view Gizmo UI that draws SpatialMenu outlines and
                sceneViewGizmosVisible = false;

                // Alternatively, SpatialMenu's state could be a single static state
                // As opposed to passing the SpatialMenu instance's delegate when a new SpatialMenu instance initiates display of the menu
                s_SpatialMenuUi.changeMenuState = ChangeMenuState;

                spatialMenuState = SpatialMenuState.navigatingTopLevel;
                s_SpatialMenuUi.spatialInterfaceInputMode = SpatialMenuUI.SpatialInterfaceInputMode.Translation;

                foreach (var data in s_SpatialMenuData)
                {
                    foreach (var element in data.spatialMenuElements)
                    {
                        // When the SpatialMenu is displayed, assign its node as the controllerNode to all SpatialMenuElements
                        // This node is used as a fallback, if the element isn't currently being hovered by a proxy/ray
                        // This allows for a separate proxy/device, other than that which is the active SpatialMenuController,
                        // to have node-specific operations performed on the hovering node (adding tools, etc), rather than the controlling Menu node
                        if (element.VisualElement != null)
                            element.VisualElement.spatialMenuActiveControllerNode = node;
                    }
                }

                return;
            }

            // Trigger + continued/held circular-input, when beyond a threshold, allow for element selection cycling
            // If the magnitude of showMenu is below 0.5, consider the finger to have left the thumbstick/trackpad outer edge; close the menu
            if (positiveYInputAction.wasJustPressed || showMenuInputActionVector2.magnitude > 0.5f)
            {
                Vector3 facing = showMenuInputActionVector2Normalized;
                if (!m_ShowMenuCircularInputCrossedRotationThresholdForSelection)
                {
                    // Calculate rotation difference only in cases where the threshold has yet to be crossed
                    var rotationDifference = Vector2.Dot(m_OriginalShowMenuCircularInputDirection, showMenuInputActionVector2Normalized);
                    if (rotationDifference < -0.5)
                    {
                        // Show Menu Rotation Input can now be cycled forward/backward to select menu elements
                        m_ShowMenuCircularInputCrossedRotationThresholdForSelection = true;
                    }
                }
                else
                {
                    if (m_CurrentSpatialActionMapInput.select.wasJustPressed)
                    {
                        s_SpatialMenuUi.SelectCurrentlyHighlightedElement(node);
                    }
                    else if (m_CircularTriggerSelectionCyclingCoroutine == null)
                    {
                        // Only allow selection if there has been a suitable amount of time since the previous selection
                        // Show menu input rotation is held, and has crossed the necessary threshold to allow for menu element cycling
                        // Positive is rotating to the right circularly, Negative is rotating to the left circularly
                        var circularRotationDirection = Vector3.Cross(facing, m_UpdatingShowMenuCircularInputDirection).z;
                        if (circularRotationDirection > 0.05f) // rotating to the right circularly
                            this.RestartCoroutine(ref m_CircularTriggerSelectionCyclingCoroutine, TimedCircularTriggerSelection());
                        else if (circularRotationDirection < -0.05f) // rotating to the left circularly
                            this.RestartCoroutine(ref m_CircularTriggerSelectionCyclingCoroutine, TimedCircularTriggerSelection(false));    
                    }
                }

                var angle = Vector3.Angle(m_UpdatingShowMenuCircularInputDirection, facing);
                m_TotalShowMenuCircularInputRotation += angle;
                m_UpdatingShowMenuCircularInputDirection = facing;
            }

            // isHeld goes false right when you go below 0.5.  this is the check for 'up-click' on the pad / stick
            if ((positiveYInputAction.isHeld || m_SpatialInputHold) && s_SpatialMenuState != SpatialMenuState.hidden)
            {
                var atLeastOneInputDeviceIsAimingAtSpatialMenu = false;
                foreach (var origin in allSpatialMenuRayOrigins)
                {
                    if (origin == null)
                        continue;

                    // If BELOW the threshold, thus a ray IS pointing at the spatialMenu, then set the mode to reflect external ray input
                    if (IsAimingAtUI(origin))
                    {
                        atLeastOneInputDeviceIsAimingAtSpatialMenu = true;
                        break;
                    }
                }

                if (atLeastOneInputDeviceIsAimingAtSpatialMenu) // Ray-based interaction takes precedence over other input types
                    s_SpatialMenuUi.spatialInterfaceInputMode = SpatialMenuUI.SpatialInterfaceInputMode.Ray;
                else if (s_SpatialMenuUi.spatialInterfaceInputMode == SpatialUIView.SpatialInterfaceInputMode.Ray)
                    s_SpatialMenuUi.ReturnToPreviousInputMode();

                this.SetRayOriginEnabled(m_RayOrigin, false);
                this.SetManipulatorsVisible(this, false);
                visible = true;

                if (s_SpatialMenuState == SpatialMenuState.navigatingSubMenuContent)
                {
                    var cancelJustPressed = CancelWasJustPressedTest(consumeControl);
                    if (cancelJustPressed)
                        return;
                }

                return;
            }

            if (!positiveYInputAction.isHeld && !m_SpatialInputHold)
                EndDisplayOfMenu();
        }

        bool CancelWasJustPressedTest(ConsumeControlDelegate consumeControl)
        {
            var cancelJustPressed = false;
            if (m_CurrentSpatialActionMapInput.cancel.wasJustPressed || m_CurrentSpatialActionMapInput.grip.wasJustPressed)
            {
                cancelJustPressed = true;
                ConsumeControls(m_CurrentSpatialActionMapInput, consumeControl);
                m_HighlightedTopLevelMenuElementPosition = -1;
                s_ControllingSpatialMenu.ReturnToPreviousMenuLevel();
            }

            return cancelJustPressed;
        }

        bool SelectJustPressedTest(ConsumeControlDelegate consumeControl)
        {
            var selectJustPressed = false;
            if (m_CurrentSpatialActionMapInput.select.wasJustPressed)
            {
                selectJustPressed = true;

                if (s_SpatialMenuState == SpatialMenuState.navigatingTopLevel)
                    s_SpatialMenuUi.SectionTitleButtonSelected(node);
                else if (s_SpatialMenuState == SpatialMenuState.navigatingSubMenuContent)
                    s_SpatialMenuUi.SelectCurrentlyHighlightedElement(node);

                ConsumeControls(m_CurrentSpatialActionMapInput, consumeControl);
            }

            return selectJustPressed;
        }

        void EndDisplayOfMenu()
        {
            s_ControllingSpatialMenu = null; // Allow another SpatialMenu to own control of the SpatialMenuUI
            m_CurrentSpatialActionMapInput = null;
            m_TotalShowMenuCircularInputRotation = 0;
            m_HighlightedTopLevelMenuElementPosition = -1;
            m_HighlightedSubLevelMenuElementPosition = -1;
            this.SetManipulatorsVisible(this, true);
            this.SetRayOriginEnabled(m_RayOrigin, true);
            visible = false;
        }

        IEnumerator TimedCircularTriggerSelection(bool selectNextItem = true)
        {
            var elementPositionOffset = selectNextItem ? 1 : -1;
            if (s_SpatialMenuState == SpatialMenuState.navigatingTopLevel)
            {
                // User should return to the previously highligted position at this depth of the SpatialMenu
                var menuElementCount = s_SpatialMenuData.Count;
                m_HighlightedTopLevelMenuElementPosition = (int)Mathf.Repeat(m_HighlightedTopLevelMenuElementPosition + elementPositionOffset, menuElementCount);
                s_SpatialMenuUi.HighlightElementInCurrentlyDisplayedMenuSection(m_HighlightedTopLevelMenuElementPosition);
            }
            else if (s_SpatialMenuState == SpatialMenuState.navigatingSubMenuContent)
            {
                // User should return to the previously highligted position at this depth of the SpatialMenu
                m_HighlightedSubLevelMenuElementPosition = (int)Mathf.Repeat(m_HighlightedSubLevelMenuElementPosition + elementPositionOffset, subMenuElementCount);
                s_SpatialMenuUi.HighlightElementInCurrentlyDisplayedMenuSection(m_HighlightedSubLevelMenuElementPosition);
            }

            // Prevent the cycling to another element by keeping the coroutine reference from being null for a period of time
            // The coroutine reference is tested against in ProcessInput(), only allowing the cycling to previous/next element if null
            const float selectionTimingBuffer = 0.2f;
            var duration = 0f;
            while (duration < selectionTimingBuffer)
            {
                duration += Time.unscaledDeltaTime;
                yield return null;
            }

            m_CircularTriggerSelectionCyclingCoroutine = null;
            yield return null;
        }
    }
}
#endif
