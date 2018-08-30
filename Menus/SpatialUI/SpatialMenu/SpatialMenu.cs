#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Helpers;
using UnityEditor.Experimental.EditorVR.Menus;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR
{
    [ProcessInput(2)] // Process input after the ProxyAnimator, but before other IProcessInput implementors
    public sealed class SpatialMenu : SpatialUIController, IProcessSpatialInput, IInstantiateUI, IUsesNode,
        IUsesRayOrigin, ISelectTool, IConnectInterfaces, IControlHaptics, INodeToRay, IDetectGazeDivergence
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

        static SpatialMenuUI s_SpatialMenuUi;

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
        Vector3 m_HomeSectionSpatialScrollStartLocalPosition;
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

        // Spatial rotation members
        Quaternion m_InitialSpatialLocalRotation;

        // Section name string, corresponding element collection, currentlyHighlightedState
        static readonly List<SpatialMenuData> s_SpatialMenuData = new List<SpatialMenuData>();

        public SpatialMenuUI spatialMenuUI { get { return s_SpatialMenuUi; } }

        string m_HighlightedSectionNameKey;
        int m_HighlightedMenuElementPosition; // element position amidst the currentlyDisplayedMenuElements
        List<SpatialMenuElement> m_HighlightedMenuElements;

        RotationVelocityTracker m_RotationVelocityTracker = new RotationVelocityTracker();
        ContinuousDirectionalVelocityTracker m_ContinuousDirectionalVelocityTracker = new ContinuousDirectionalVelocityTracker();

        List<SpatialMenuElement> highlightedMenuElements
        {
            set
            {
                if (m_HighlightedMenuElements == value)
                    return;

                m_HighlightedMenuElements = value;
                s_SpatialMenuUi.highlightedMenuElements = m_HighlightedMenuElements;
            }
        }

        bool visible
        {
            set
            {
                if (m_Visible == value)
                    return;

                m_Visible = value;
                pollingSpatialInputType = m_Visible;

                if (m_Visible)
                {
                    RefreshProviderData();
                    spatialMenuState = SpatialMenuState.navigatingTopLevel;
                }
                else
                {
                    ReturnToPreviousMenuLevel(); // TODO: verify that this needs to be called, or can be replaced by a core set of referenced functionality

                    if (m_SpatialMenuState == SpatialMenuState.navigatingSubMenuContent &&
                        m_HighlightedMenuElements != null &&
                        m_HighlightedMenuElements.Count > 0 &&
                        m_HighlightedMenuElements[m_HighlightedMenuElementPosition] != null &&
                        m_HighlightedMenuElements[m_HighlightedMenuElementPosition].correspondingFunction != null)
                    {
                        m_HighlightedMenuElements[m_HighlightedMenuElementPosition].correspondingFunction();
                        this.Pulse(Node.None, m_HighlightMenuElementPulse);
                    }

                    this.Pulse(Node.None, m_MenuClosePulse);
                    spatialMenuState = SpatialMenuState.hidden;

                    spatialScrollOrigin = null;
                    node = Node.None;
                }
            }
        }

        SpatialMenuState spatialMenuState
        {
            set
            {
                if (m_SpatialMenuState == value)
                    return;

                m_SpatialMenuState = value;
                s_SpatialMenuUi.spatialMenuState = value;

                m_RotationVelocityTracker.Initialize(m_CurrentSpatialActionMapInput.localRotationQuaternion.quaternion);

                switch (m_SpatialMenuState)
                {
                    case SpatialMenuState.navigatingTopLevel:
                        s_SpatialMenuUi.spatialMenuState = SpatialMenuState.navigatingTopLevel;
                        m_ContinuousDirectionalVelocityTracker.Initialize(this.RequestRayOriginFromNode(Node.LeftHand).position);
                        SetSpatialScrollStartingConditions(m_CurrentSpatialActionMapInput.localPosition.vector3, m_CurrentSpatialActionMapInput.localRotationQuaternion.quaternion, SpatialInputModule.SpatialCardinalScrollDirection.LocalX, 3);
                        break;
                    case SpatialMenuState.navigatingSubMenuContent:
                        SetSpatialScrollStartingConditions(m_CurrentSpatialActionMapInput.localPosition.vector3, m_CurrentSpatialActionMapInput.localRotationQuaternion.quaternion, SpatialInputModule.SpatialCardinalScrollDirection.LocalY);
                        DisplayHighlightedSubMenuContents();
                        break;
                    case SpatialMenuState.hidden:
                        sceneViewGizmosVisible = true;
                        break;
                }
            }
        }

        Transform m_RayOrigin;

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

        Node m_Node;
        public Node node
        {
            get { return m_Node; }

            set
            {
                if (value == m_Node)
                    return;

                m_Node = value;
            }
        }

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
        public SpatialInputModule.SpatialScrollData spatialScrollData { get; set; }
        public Transform spatialScrollOrigin { get; set; }
        public Vector3 spatialScrollStartPosition { get; set; }
        public float spatialQuickToggleDuration { get { return k_SpatialQuickToggleDuration; } }
        public float allowSpatialQuickToggleActionBeforeThisTime { get; set; }

        // Angular Ray-based detection of all ray-origins having spawned a SpatialMenu controller
        static readonly List<Transform> allSpatialMenuRayOrigins = new List<Transform>();

        public static readonly List<ISpatialMenuProvider> s_SpatialMenuProviders = new List<ISpatialMenuProvider>();

        void ChangeMenuState(SpatialMenuState state)
        {
            spatialMenuState = state;
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
            m_ContinuousDirectionalVelocityTracker.Initialize(this.RequestRayOriginFromNode(Node.LeftHand).position);
        }

        void SetSpatialScrollStartingConditions(Vector3 localPosition, Quaternion localRotation, SpatialInputModule.SpatialCardinalScrollDirection direction, int menuElemenCountOverride = -1)
        {
            node = Node.LeftHand; // TODO: fetch node that initiated the display of the spatial ui
            m_HomeSectionSpatialScrollStartLocalPosition = localPosition;
            m_InitialSpatialLocalRotation = localRotation; // Cache the current starting rotation, current deltaAngle will be calculated relative to this rotation

            if (spatialScrollData != null)
                this.EndSpatialScroll();

            //if (m_HighlightedMenuElements != null)
            {
                // TODO: set the spatial scroll origin based on the node that initiates the display of the SpatialUI // No needed if we have single UI/view and per-device controllers with their own assigned nodes
                spatialScrollOrigin = this.RequestRayOriginFromNode(Node.LeftHand);
                spatialScrollStartPosition = spatialScrollOrigin.position;

                var highlightedMenuElementsCount = m_HighlightedMenuElements != null ? m_HighlightedMenuElements.Count : 0;
                var elementCount = menuElemenCountOverride != -1 ? menuElemenCountOverride : highlightedMenuElementsCount;
                spatialScrollData = this.PerformLocalCardinallyConstrainedSpatialScroll(direction, node, spatialScrollStartPosition, spatialScrollOrigin.position, k_SpatialScrollVectorLength, SpatialInputModule.ScrollRepeatType.Clamped, elementCount, elementCount);
            }
        }

        void DisplayHighlightedSubMenuContents()
        {
            m_ContinuousDirectionalVelocityTracker.Initialize(this.RequestRayOriginFromNode(Node.LeftHand).position);
            this.Pulse(Node.None, m_MenuOpenPulse);

            m_MenuEntranceStartTime = Time.realtimeSinceStartup;
        }

        void ReturnToPreviousMenuLevel()
        {
            if (m_SpatialMenuState == SpatialMenuState.navigatingSubMenuContent)
                SetSpatialScrollStartingConditions(m_CurrentSpatialActionMapInput.localPosition.vector3, m_CurrentSpatialActionMapInput.localRotationQuaternion.quaternion, SpatialInputModule.SpatialCardinalScrollDirection.LocalX, 3);

            this.Pulse(Node.None, m_NavigateBackPulse);
            m_MenuEntranceStartTime = Time.realtimeSinceStartup;
            spatialMenuState = SpatialMenuState.navigatingTopLevel;

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
            //Debug.Log("processing input in SpatialUI");
            const float kSubMenuNavigationTranslationTriggerThreshold = 0.075f;
            m_CurrentSpatialActionMapInput = (SpatialMenuInput)input;

            // count how long the input value has been default and thus is in deadzone or lifted
            if (m_CurrentSpatialActionMapInput.showMenu.vector2 == default(Vector2))
                m_DefaultValueTime += Time.deltaTime;
            else
                m_DefaultValueTime = 0f;

            // release after the input has been default for a few frames.  This almost entirely prevents
            // the case where having your thumb in the middle of the pad causes default value and thus release
            if (m_DefaultValueTime >= 0.05f)
            {
                m_SpatialInputHold = false;
            }

            // This block is only processed after a frame with both trigger buttons held has been detected
            if (spatialScrollData != null && m_CurrentSpatialActionMapInput.cancel.wasJustPressed)
            {
                m_SpatialInputHold = false;
                ConsumeControls(m_CurrentSpatialActionMapInput, consumeControl);
            }

            // Detect the initial activation of the relevant Spatial input
            // convert left thumbstick y
            if (m_CurrentSpatialActionMapInput.showMenu.positiveY.wasJustPressed)
            {
                m_SpatialInputHold = true;
                ConsumeControls(m_CurrentSpatialActionMapInput, consumeControl); // Select should only be consumed upon activation, so other UI can receive select events

                // Hide the scene view Gizmo UI that draws SpatialMenu outlines and
                sceneViewGizmosVisible = false;

                m_MenuEntranceStartTime = Time.realtimeSinceStartup;
                spatialMenuState = SpatialMenuState.navigatingTopLevel;
                s_SpatialMenuUi.spatialInterfaceInputMode = SpatialMenuUI.SpatialInterfaceInputMode.Translation;
                //Reset();
            }

            // isHeld goes false right when you go below 0.5.  this is the check for 'up-click' on the pad / stick
            // TODO - we also need to invent the definition of 'released'.  some combo of Isheld = false & below minimum x/y deadzone for a time
            if ((m_CurrentSpatialActionMapInput.showMenu.positiveY.isHeld || m_SpatialInputHold) && m_SpatialMenuState != SpatialMenuState.hidden )
            {
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
                        s_SpatialMenuUi.spatialInterfaceInputMode = SpatialMenuUI.SpatialInterfaceInputMode.Ray;
                    else if (s_SpatialMenuUi.spatialInterfaceInputMode == SpatialMenuUI.SpatialInterfaceInputMode.Ray)
                        s_SpatialMenuUi.ReturnToPreviousInputMode();
                }

                m_ContinuousDirectionalVelocityTracker.Update(m_CurrentSpatialActionMapInput.localPosition.vector3, Time.unscaledDeltaTime);
                //Debug.Log("<color=green>Continuous Direction strength " + m_ContinuousDirectionalVelocityTracker.directionalDivergence + "</color>");

                ConsumeControls(m_CurrentSpatialActionMapInput, consumeControl, false);
                //consumeControl(m_CurrentSpatialActionMapInput.select);

                m_Transitioning = Time.realtimeSinceStartup - m_MenuEntranceStartTime > k_MenuSectionBlockedTransitionTimeWindow; // duration for which input is not taken into account when menu swapping
                visible = true;

                var inputLocalRotation = m_CurrentSpatialActionMapInput.localRotationQuaternion.quaternion;

                if (m_Transitioning && m_SpatialMenuState == SpatialMenuState.navigatingSubMenuContent && m_ContinuousDirectionalVelocityTracker.directionalDivergence > 10.08f) // TODO: return to 0.08f
                {
                    //Debug.LogWarning("<color=green>" + Mathf.DeltaAngle(m_InitialSpatialLocalRotation.z, actionMapInput.localRotationQuaternion.quaternion.z) + "</color>");
                    ReturnToPreviousMenuLevel();
                    return;
                }

                if (m_SpatialMenuState == SpatialMenuState.navigatingSubMenuContent)
                {
                    if (m_CurrentSpatialActionMapInput.cancel.wasJustPressed || m_CurrentSpatialActionMapInput.grip.wasJustPressed)
                    {
                        Debug.LogWarning("Cancel button was just pressed on node : " + node);
                        ReturnToPreviousMenuLevel();
                        return;
                    }

                    if (m_HighlightedMenuElements != null)
                    {
                        var menuElementCount = m_HighlightedMenuElements.Count;
                        spatialScrollData = this.PerformLocalCardinallyConstrainedSpatialScroll(SpatialInputModule.SpatialCardinalScrollDirection.LocalY, node, spatialScrollStartPosition, spatialScrollOrigin.position, k_SpatialScrollVectorLength, SpatialInputModule.ScrollRepeatType.Clamped, menuElementCount, menuElementCount);
                        var normalizedRepeatingPosition = spatialScrollData.normalizedLoopingPositionUnconstrained;
                        if (!Mathf.Approximately(normalizedRepeatingPosition, 0f))
                        {
                            m_HighlightedMenuElementPosition = spatialScrollData.highlightedMenuElementPositionUnconstrained;//(int) (menuElementCount * normalizedRepeatingPosition);
                            s_SpatialMenuUi.HighlightSingleElementInCurrentMenu(spatialScrollData.loopingHighlightedMenuElementPositionYConstrained);
                        }
                    }
                }
                return;
            }

            if (!m_CurrentSpatialActionMapInput.showMenu.positiveY.isHeld && !m_SpatialInputHold)
            {
                visible = false;
            }
        }
    }
}
#endif
