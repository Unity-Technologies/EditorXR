#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Helpers;
using UnityEditor.Experimental.EditorVR.Menus;
using UnityEditor.Experimental.EditorVR.Modules;
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
    public sealed class SpatialMenu : SpatialUIController, IInstantiateUI, IUsesNode, IUsesRayOrigin, ISelectTool,
        IConnectInterfaces, IControlHaptics, INodeToRay, IDetectGazeDivergence, IControlInputIntersection,
        ISetManipulatorsVisible, ILinkedObject, IRayVisibilitySettings, ICustomActionMap
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
        const float k_SpatialScrollVectorLength = 0.25f;  // was 0.125, though felt too short a distance for the Spatial Menu (was better suited for the tools menu implementation)

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

        HapticPulse m_HighlightMenuElementPulse; // Fetched from SpatialUICore(SpatialMenuUI)

        SpatialMenuState m_SpatialMenuState;

        bool m_Visible;
        bool m_Transitioning;

        SpatialMenuInput m_CurrentSpatialActionMapInput;

        // Bool denoting that the input necessary to keep the SpatialMenu visible is currently being maintained
        bool m_SpatialInputHold;

        // Duration denoting how long the input value has been at default/neutral and thus is in deadzone or lifted
        float m_DefaultValueTime;

        // "Rotate wrist to return" members
        float m_StartingWristXRotation;
        float m_WristReturnVelocity;

        // Menu entrance start time
        float m_MenuEntranceStartTime;

        string m_HighlightedSectionNameKey;
        int m_HighlightedTopLevelMenuElementPosition;
        int m_HighlightedSubLevelMenuElementPosition;
        List<SpatialMenuElement> m_DisplayedMenuElements;

        // Trigger + continued/held circular-input related fields
        Vector2 m_OriginalShowMenuCircularInputDirection;
        Vector3 m_UpdatingShowMenuCircularInputDirection;
        bool m_ShowMenuCircularInputCrossedRotationThresholdForSelection;
        float m_TotalShowMenuCircularInputRotation;
        Coroutine m_CircularTriggerSelectionCyclingCoroutine;
        Transform m_RayOrigin;

        RotationVelocityTracker m_RotationVelocityTracker = new RotationVelocityTracker();

        List<SpatialMenuElement> highlightedMenuElements
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
                    ReturnToPreviousMenuLevel(); // TODO: verify that this needs to be called, or can be replaced by a core set of referenced functionality
                    this.Pulse(Node.None, m_MenuClosePulse);
                    spatialMenuState = SpatialMenuState.hidden;
                }
            }
        }

        private SpatialMenuState spatialMenuState
        {
            set
            {
                if (m_SpatialMenuState == value)
                    return;

                m_SpatialMenuState = value;
                s_SpatialMenuUi.spatialMenuState = m_SpatialMenuState;
                m_RotationVelocityTracker.Initialize(m_CurrentSpatialActionMapInput.localRotationQuaternion.quaternion);
                switch (m_SpatialMenuState)
                {
                    case SpatialMenuState.navigatingTopLevel:
                        m_HighlightedSubLevelMenuElementPosition = -1;
                        m_SubMenuData = null;
                        break;
                    case SpatialMenuState.navigatingSubMenuContent:
                        m_SubMenuData = s_SpatialMenuData.Where(x => x.highlighted).First();
                        this.Pulse(Node.None, m_MenuOpenPulse);
                        m_MenuEntranceStartTime = Time.realtimeSinceStartup;
                        break;
                    case SpatialMenuState.hidden:
                        sceneViewGizmosVisible = true;
                        m_CircularTriggerSelectionCyclingCoroutine = null;
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

        //IMenu interface members
        public MenuHideFlags menuHideFlags { get; set; }
        public GameObject menuContent { get; private set; }
        public Bounds localBounds { get; private set; }
        public int priority { get; private set; }

        // Action Map interface members
        public ActionMap actionMap { get { return m_ActionMap; } set { m_ActionMap = value; } }
        public bool ignoreActionMapInputLocking { get; private set; }

        public SpatialMenuUI spatialMenuUI { get { return s_SpatialMenuUi; } }
        public List<ILinkedObject> linkedObjects { private get; set; }
        public class SpatialMenuElement
        {
            public SpatialMenuElement(string name, string tooltipText, Action<Node> correspondingFunction)
            {
                this.name = name;
                this.tooltipText = tooltipText;
                this.correspondingFunction = correspondingFunction;
            }

            public string name { get; set; }

            public Action<Node> correspondingFunction { get; private set; }

            public string tooltipText { get; private set; }

            public ISpatialMenuElement VisualElement { get; set; }
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
                // this.ConnectInterfaces(m_SpatialMenuUi);
                s_SpatialMenuUi.spatialMenuData = s_SpatialMenuData; // set shared reference to menu name/type, elements, and highlighted state
                s_SpatialMenuUi.Setup();
                s_SpatialMenuUi.returnToPreviousMenuLevel = ReturnToPreviousMenuLevel;
                s_SpatialMenuUi.changeMenuState = ChangeMenuState;
                SpatialMenuUI.spatialMenuProviders = s_SpatialMenuProviders;

                // Certain core/common SpatialUICore elements are retrieved from SpatialMenuUI(deriving from Core)
                m_HighlightMenuElementPulse = s_SpatialMenuUi.highlightUIElementPulse;
            }

            visible = false;
        }

        // Delegate function assigned to SpatialMenuUI's changeMenuState action
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
                    {
                        Debug.Log("Adding a spatial menu collection of type : " + menuData.spatialMenuName);
                        s_SpatialMenuData.Add(menuData);
                    }
                }
            }
        }

        public static void AddProvider(ISpatialMenuProvider provider)
        {
            if (s_SpatialMenuProviders.Contains(provider))
            {
                Debug.LogWarning("Cannot add multiple menus of the same type to the SpatialUI");
                return;
            }

            s_SpatialMenuProviders.Add(provider);

            foreach (var menuElementSet in provider.spatialMenuData)
            {
                Debug.LogWarning("Adding a spatial menu collection of type : " + menuElementSet.spatialMenuName);
                s_SpatialMenuData.Add(menuElementSet);
            }
        }

        void Reset()
        {
            return;

            spatialMenuState = SpatialMenuState.navigatingTopLevel;

            this.Pulse(Node.None, m_MenuOpenPulse);

            m_RotationVelocityTracker.Initialize(this.RequestRayOriginFromNode(Node.LeftHand).localRotation);
        }

        void ReturnToPreviousMenuLevel()
        {
            if (m_SpatialMenuState == SpatialMenuState.navigatingSubMenuContent)
                this.Pulse(Node.None, m_NavigateBackPulse); // Only perform haptic pulse when not at the top-level of the UI

            m_MenuEntranceStartTime = Time.realtimeSinceStartup;
            spatialMenuState = SpatialMenuState.navigatingTopLevel;
            m_HighlightedTopLevelMenuElementPosition = -1;

            Debug.LogWarning("SpatialMenu : <color=green>Above wrist return threshold</color>");
        }

        public bool IsAboveDivergenceThreshold(Transform firstTransform, Transform secondTransform, float divergenceThreshold)
        {
            var isAbove = false;
            var gazeDirection = firstTransform.forward;
            var testVector = secondTransform.position - firstTransform.position; // Test object to gaze source vector
            testVector.Normalize(); // Normalize, in order to retain expected dot values

            var divergenceThresholdConvertedToDot = Mathf.Sin(Mathf.Deg2Rad * divergenceThreshold);
            var angularComparison = Mathf.Abs(Vector3.Dot(testVector, gazeDirection));
            isAbove = angularComparison < divergenceThresholdConvertedToDot;

            return isAbove;
        }

        public void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl)
        {
            if (s_ControllingSpatialMenu != null && s_ControllingSpatialMenu != this)
            {
                // Perform custom logic for proxies (driving a SpatialMenuController) that didn't initiate the display of the SpatialMenu
                // Though they may need their input actions to drive certain SpatialMenu functionality
                var cancelJustPressed = CancelWasJustPressedTest(consumeControl);
                if (!cancelJustPressed) // Only process selection testing if cancel was not just pressed
                    SelectJustPressedTest(consumeControl);

                return;
            }

            m_CurrentSpatialActionMapInput = (SpatialMenuInput)input;
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

                m_MenuEntranceStartTime = Time.realtimeSinceStartup;
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
                        element.VisualElement.spatialMenuActiveControllerNode = node;
                    }
                }
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
                        s_SpatialMenuUi.spatialInterfaceInputMode = SpatialUIView.SpatialInterfaceInputMode.TriggerRotation;

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
            // TODO - we also need to invent the definition of 'released'.  some combo of Isheld = false & below minimum x/y deadzone for a time
            if ((positiveYInputAction.isHeld || m_SpatialInputHold) && m_SpatialMenuState != SpatialMenuState.hidden)
            {
                var atLeastOneInputDeviceIsAimingAtSpatialMenu = false;
                m_RotationVelocityTracker.Update(m_CurrentSpatialActionMapInput.localRotationQuaternion.quaternion, Time.deltaTime);
                foreach (var origin in allSpatialMenuRayOrigins)
                {
                    if (origin == null || origin == m_RayOrigin) // Don't compare against the rayOrigin that is currently processing input for the Spatial UI
                        continue;

                    // Compare the angular differnce between the spatialUI's transform, and ANY spatial menu ray origin
                    var isAboveDivergenceThreshold = IsAboveDivergenceThreshold(origin, s_SpatialMenuUi.adaptiveTransform, 45);

                    Debug.Log(origin.name + "<color=green> opposite ray origin divergence value : </color>" + isAboveDivergenceThreshold);

                    // If BELOW the threshold, thus a ray IS pointing at the spatialMenu, then set the mode to reflect external ray input
                    if (!isAboveDivergenceThreshold)
                    {
                        atLeastOneInputDeviceIsAimingAtSpatialMenu = true;
                        //this.AddRayVisibilitySettings(directRayOrigin, this, false, true); // This will also disable ray selection
                    }
                    else if (s_SpatialMenuUi.spatialInterfaceInputMode == SpatialMenuUI.SpatialInterfaceInputMode.Ray)
                    {
                        //s_SpatialMenuUi.ReturnToPreviousInputMode();
                        //this.RemoveRayVisibilitySettings(rayOrigin, this);
                    }
                }

                if (atLeastOneInputDeviceIsAimingAtSpatialMenu) // Ray-based interaction takes precedence over other input types
                    s_SpatialMenuUi.spatialInterfaceInputMode = SpatialMenuUI.SpatialInterfaceInputMode.Ray;
                else
                    s_SpatialMenuUi.ReturnToPreviousInputMode();

                ConsumeControls(m_CurrentSpatialActionMapInput, consumeControl, false);

                m_Transitioning = Time.realtimeSinceStartup - m_MenuEntranceStartTime > k_MenuSectionBlockedTransitionTimeWindow; // duration for which input is not taken into account when menu swapping
                this.SetRayOriginEnabled(m_RayOrigin, false);
                this.SetManipulatorsVisible(this, false);
                visible = true;

                if (m_Transitioning && m_SpatialMenuState == SpatialMenuState.navigatingSubMenuContent)
                {
                    //Debug.LogWarning("<color=green>" + Mathf.DeltaAngle(m_InitialSpatialLocalRotation.z, actionMapInput.localRotationQuaternion.quaternion.z) + "</color>");
                    ReturnToPreviousMenuLevel();
                    return;
                }

                if (m_SpatialMenuState == SpatialMenuState.navigatingSubMenuContent)
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
                Debug.LogWarning("Cancel button was just pressed on node : " + node + " : " + m_RayOrigin.name);
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
                Debug.LogWarning("<color=green>SELECT button was just pressed on node : </color>" + node + " : " + m_RayOrigin.name);
                selectJustPressed = true;
                s_SpatialMenuUi.SectionTitleButtonSelected(node);
                ConsumeControls(m_CurrentSpatialActionMapInput, consumeControl);
            }

            return selectJustPressed;
        }

        void EndDisplayOfMenu()
        {
            s_ControllingSpatialMenu = null; // Allow another SpatialMenu to own control of the SpatialMenuUI
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
            if (m_SpatialMenuState == SpatialMenuState.navigatingTopLevel)
            {
                Debug.LogError("circular selection of TOP LEVEL elements");
                // User should return to the previously highligted position at this depth of the SpatialMenu
                var menuElementCount = s_SpatialMenuData.Count;
                m_HighlightedTopLevelMenuElementPosition = (int)Mathf.Repeat(m_HighlightedTopLevelMenuElementPosition + elementPositionOffset, menuElementCount);
                s_SpatialMenuUi.HighlightElementInCurrentlyDisplayedMenuSection(m_HighlightedTopLevelMenuElementPosition);
            }
            else if (m_SpatialMenuState == SpatialMenuState.navigatingSubMenuContent)
            {
                // User should return to the previously highligted position at this depth of the SpatialMenu
                Debug.LogError("circular selection of sub menu elements : " + subMenuElementCount);
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
