#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Helpers;
using UnityEditor.Experimental.EditorVR.Menus;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.InputNew;
using Random = UnityEngine.Random;

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
        //bool m_BeingMoved;
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

        public SpatialMenuUI spatialMenuUI { get { return s_SpatialMenuUi; } }
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
                s_SpatialMenuUi.highlightedMenuElements = m_HighlightedMenuElements;
            }
        }

        bool visible
        {
            get { return m_Visible; }

            set
            {
                if (m_Visible == value)
                    return;

                m_Visible = value;
                //m_SpatialMenuUi.visible = m_Visible;
                pollingSpatialInputType = m_Visible;

                if (m_Visible)
                {
                    RefreshProviderData();
                    spatialMenuState = SpatialMenuState.navigatingTopLevel;
                    //gameObject.SetActive(true);  MOVED TO SPATIAL UI View

                    /*
                    var sceneViewCameras = SceneView.GetAllSceneCameras();
                    var sceneView = SceneView.currentDrawingSceneView;
                    SceneView.SceneViewState outlinesDisabledState = new SceneView.SceneViewState(sceneView.sceneViewState);
                    Shader.SetGlobalFloatArray("_BlurDirection", new float[]{0, 100});
                    Shader.SetGlobalColor("_OutlineColor", Color.clear);
                    Shader.SetGlobalFloatArray("_MainTex_TexelSize", new float[]{0,0,0,0});
                    Shader.SetGlobalFloat("_ObjectId", -1);
                    */
                    //outlinesDisabledState.
                    //AnnotationInput.
                    //sceneView.sceneViewState = new SceneView.SceneViewState();
                    
                    //foreach (var sceneView in sceneViews)
                    //{
                        //sceneview
                    //}
                    //SceneView.SceneViewState = new 
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
                    /*
                    Shader.SetGlobalFloatArray("_BlurDirection", new float[]{10, 0});
                    Shader.SetGlobalColor("_OutlineColor", Color.red);
                    Shader.SetGlobalFloatArray("_MainTex_TexelSize", new float[]{1,1,1,1});
                    */
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

                switch (m_SpatialMenuState)
                {
                    case SpatialMenuState.navigatingTopLevel:
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

                if (value != Node.None && s_SpatialMenuUi != null)
                {
                    removeControllingNode(m_Node);
                    addControllingNode(value);
                }

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

        // Ray-based members
        public Transform rayBasedInteractionSource
        {
            get
            {
                Transform source = null;
                return s_SpatialMenuUi != null ? s_SpatialMenuUi.rayBasedInteractionSource : null;
            }
        }

        public static readonly List<ISpatialMenuProvider> s_SpatialMenuProviders = new List<ISpatialMenuProvider>();

        /*
        public bool beingMoved
        {
            set
            {
                if (m_BeingMoved != value)
                    return;

                m_BeingMoved = value;
                m_SpatialMenuUi.beingMoved = value;

                if (m_BeingMoved)
                    this.Pulse(Node.None, m_MenuOpenPulse);
            }
        }
        */

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

        void SetSceneState()
        {
            /*
            HashSet<int> classIdsToToggle = new HashSet<int>
            {
                4, // Transform
                20, // Camera
                54, // rigidbody
                82, // AudioSource
                108 // light
            };

            k_ClassIconEnabledStates.Clear();
            var Annotation = Type.GetType("UnityEditor.Annotation, UnityEditor");
            var ClassId = Annotation.GetField("classID");
            var ScriptClass = Annotation.GetField("scriptClass");
            var asm = Assembly.GetAssembly(typeof(Editor));
            var type = asm.GetType("UnityEditor.AnnotationUtility");
            if (type != null)
            {
                MethodInfo getAnnotations =
                    type.GetMethod("GetAnnotations", BindingFlags.Static | BindingFlags.NonPublic);
                MethodInfo setIconEnabled =
                    type.GetMethod("SetIconEnabled", BindingFlags.Static | BindingFlags.NonPublic);
                PropertyInfo setSelectionOutlinesEnabled = type.GetProperty("showSelectionOutline", BindingFlags.Static | BindingFlags.NonPublic);


                var annotations = (Array)getAnnotations.Invoke(null, null);
                foreach (var a in annotations)
                {
                    int classId = (int)ClassId.GetValue(a);
                    if (classIdsToToggle.Contains(classId))
                    {
                        var iconEnabledField = Annotation.GetField("iconEnabled");
                        var iconEnabled = (int)iconEnabledField.GetValue(a);
                        string scriptClass = (string)ScriptClass.GetValue(a);
                        k_ClassIconEnabledStates.Add(classId, new KeyValuePair<string, bool>(scriptClass, iconEnabled == 1));
                        setIconEnabled.Invoke(null, new object[] { classId, scriptClass, 0 });

                        var gizmoEnabledField = Annotation.GetField("gizmoEnabled");
                        var gizmoEnabled = (int)gizmoEnabledField.GetValue(a);
                        scriptClass = (string)ScriptClass.GetValue(a);
                        k_ClassIconEnabledStates.Add(classId, new KeyValuePair<string, bool>(scriptClass, iconEnabled == 1));
                        setIconEnabled.Invoke(null, new object[] { classId, scriptClass, 0 });

                        setSelectionOutlinesEnabled.SetValue(null, new object[] { classId, scriptClass, 0 }, null);
                    }
                }

                //var asm = Assembly.GetAssembly(typeof(Editor));
                //var type = asm.GetType("UnityEditor.AnnotationUtility");
                if (type != null)
                {
                    foreach (var kvp in k_ClassIconEnabledStates)
                    {
                        var classId = kvp.Key;
                        var innerKvp = kvp.Value;
                        var scriptClass = innerKvp.Key;
                        var enabled = innerKvp.Value;
                        //setIconEnabled.Invoke(null, new object[] { classId, scriptClass, enabled ? 1 : 0 });
                        //setSelectionOutlinesEnabled.SetValue(null, new object[] { classId, scriptClass, 0 }, 0);
                    }
                }
            }
            */
        }

        void Update()
        {
            

            return;

            /*
            foreach (var property in properties)
            {
            }

            property.SetValue(obj, iterator.floatValue, null);
            */

            //var executingAssembly = Assembly.GetExecutingAssembly();
            //var annotationUtilType = executingAssembly.GetType("UnityEditor.AnnotationUtility", true, false);

            //var typeByStringName = Type.GetType("AnnotationUtility");
            //PropertyInfo method = typeByStringName.GetProperty("showSelectionOutline", BindingFlags.Static);

            var innerType = Assembly.GetExecutingAssembly().GetTypes().First(t => t.Name == "UnityEditor.AnnotationUtility");

            var innerObject = Activator.CreateInstance(innerType);
            innerType.GetMethod("InnerTest", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(innerObject, new object[] { });

            //var namedTyped = typeof(UnityEditor.AnnotationUtility).FullName;

            /*
            var assemblyTypes = Assembly.GetExecutingAssembly().GetTypes();//.Where(t => t.Name == "AnnotationUtility");
            var type = assemblyTypes.First(t => t.Name == "AnnotationUtility");
            var staticMethodInfo = type.GetProperties();// .GetProperty("showSelectionOutline");
            foreach (var prop in staticMethodInfo)
            {
                Debug.Log(prop.Name);
            }
            */

            //staticMethodInfo.SetValue(innerType, false, null);

            Shader.SetGlobalFloatArray("_BlurDirection", new float[]{0, 100});
            Shader.SetGlobalColor("_OutlineColor", Color.clear);
            Shader.SetGlobalFloatArray("_MainTex_TexelSize", new float[]{0,0,0,0});
            Shader.SetGlobalFloat("_ObjectId", -1);

            return;
            if (s_SpatialMenuUi != null && s_SpatialMenuUi.directorBeyondHomeSectionDuration)
            {
                //StopAllCoroutines();
                //HideSubMenu();
                //allowAdaptivePositioning = false;
                //gameObject.SetActive(m_Visible);
            }
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

            //Debug.LogWarning("Adding a provider");
            s_SpatialMenuProviders.Add(provider);

            //if (provider.spatialMenuData.Count == 0)
                //Debug.LogWarning("No spatial menu data was found when adding a new Spatial Menu provider");

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
            //m_HomeTextBackgroundTransform.localScale = new Vector3(m_HomeTextBackgroundOriginalLocalScale.x, m_HomeTextBackgroundOriginalLocalScale.y * 6, 1f);

           //m_SpatialMenuUi.DisplayHighlightedSubMenuContents();

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

            // This block is only processed after a frame with both trigger buttons held has been detected
            if (spatialScrollData != null && m_CurrentSpatialActionMapInput.cancel.wasJustPressed)
            {
                ConsumeControls(m_CurrentSpatialActionMapInput, consumeControl);

                //consumeControl(actionMapInput.localPosition);
                //consumeControl(actionMapInput.localRotationQuaternion);

                /*
                //OnButtonClick();
                //CloseMenu(); // Also ends spatial scroll
                //m_ToolsMenuUI.allButtonsVisible = false;
                */
            }

            /* Handled in the SpatialMenuUI now
            // Prevent input processing while moving
            if (m_BeingMoved)
            {
                consumeControl(m_CurrentSpatialActionMapInput.show);
                consumeControl(m_CurrentSpatialActionMapInput.select);

                //TODO restore this functionality.  It resets the starting position when being moved, but currently breaks when initially opening the menu
                if (m_SpatialMenuState != SpatialMenuState.hidden && Vector3.Magnitude(m_HomeSectionSpatialScrollStartLocalPosition - m_CurrentSpatialActionMapInput.localPosition.vector3) > kSubMenuNavigationTranslationTriggerThreshold)
                    m_HomeSectionSpatialScrollStartLocalPosition = m_CurrentSpatialActionMapInput.localPosition.vector3;
            }
            */

            // Detect the initial activation of the relevant Spatial input
            if (m_CurrentSpatialActionMapInput.showMenu.positive.wasJustPressed)
            {
                ConsumeControls(m_CurrentSpatialActionMapInput, consumeControl); // Select should only be consumed upon activation, so other UI can receive select events

                // Hide the scene view Gizmo UI that draws SpatialMenu outlines and 
                sceneViewGizmosVisible = false;

                spatialMenuState = SpatialMenuState.navigatingTopLevel;
                s_SpatialMenuUi.spatialInterfaceInputMode = SpatialMenuUI.SpatialInterfaceInputMode.Translation;
                //Reset();
            }

            if (m_CurrentSpatialActionMapInput.showMenu.positive.isHeld && m_SpatialMenuState != SpatialMenuState.hidden)
            {
                m_RotationVelocityTracker.Update(m_CurrentSpatialActionMapInput.localRotationQuaternion.quaternion, Time.deltaTime);
                if (!s_SpatialMenuUi.transitioningInputModes)
                {
                    foreach (var origin in allSpatialMenuRayOrigins)
                    {
                        if (origin == null || origin == m_RayOrigin) // Don't compare against the rayOrigin that is currently processing input for the Spatial UI
                            continue;

                        // Compare the angular differnce between the spatialUI's transform, and ANY spatial menu ray origin
                        var isAboveDivergenceThreshold = IsAboveDivergenceThreshold(origin, s_SpatialMenuUi.adaptiveTransform, 45);

                        Debug.Log(origin.name + "<color=green> opposite ray origin divergence value : </color>" + isAboveDivergenceThreshold);

                        // If BELOW the threshold, thus a ray IS pointing at the spatialMenu, then set the mode to reflect external ray input
                        if (!isAboveDivergenceThreshold)
                            s_SpatialMenuUi.spatialInterfaceInputMode = SpatialMenuUI.SpatialInterfaceInputMode.ExternalInputRay;
                        else if (s_SpatialMenuUi.spatialInterfaceInputMode == SpatialMenuUI.SpatialInterfaceInputMode.ExternalInputRay)
                            s_SpatialMenuUi.ReturnToPreviousInputMode();
                    }

                    if (s_SpatialMenuUi.spatialInterfaceInputMode != SpatialMenuUI.SpatialInterfaceInputMode.GhostRay && m_RotationVelocityTracker.rotationStrength > 600)
                    {
                        spatialScrollOrigin = this.RequestRayOriginFromNode(Node.LeftHand);
                        spatialScrollStartPosition = spatialScrollOrigin.position;
                        m_ContinuousDirectionalVelocityTracker.Initialize(this.RequestRayOriginFromNode(Node.LeftHand).position);
                        s_SpatialMenuUi.spatialInterfaceInputMode = SpatialMenuUI.SpatialInterfaceInputMode.GhostRay;
                    }
                    else if (s_SpatialMenuUi.spatialInterfaceInputMode == SpatialMenuUI.SpatialInterfaceInputMode.GhostRay)
                    {
                        // Transition back to spatial translation mode

                        //if ((spatialScrollStartPosition - m_CurrentSpatialActionMapInput.localPosition.vector3).magnitude > 0.25f)
                        if (m_ContinuousDirectionalVelocityTracker.directionalDivergence > 0.08f)
                        {
                            s_SpatialMenuUi.spatialInterfaceInputMode = SpatialMenuUI.SpatialInterfaceInputMode.Translation;
                            //SetSpatialScrollStartingConditions(m_CurrentSpatialActionMapInput.localPosition.vector3, m_CurrentSpatialActionMapInput.localRotationQuaternion.quaternion, SpatialInputModule.SpatialCardinalScrollDirection.LocalX, 3);
                        }
                        //*/
                    }
                }

                m_ContinuousDirectionalVelocityTracker.Update(m_CurrentSpatialActionMapInput.localPosition.vector3, Time.unscaledDeltaTime);
                //Debug.Log("<color=green>Continuous Direction strength " + m_ContinuousDirectionalVelocityTracker.directionalDivergence + "</color>");

                ConsumeControls(m_CurrentSpatialActionMapInput, consumeControl, false);
                //consumeControl(m_CurrentSpatialActionMapInput.select);

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
                //var spatialInputType = this.GetSpatialInputTypeForNode(Node.LeftHand);
                //Debug.LogWarning("SpatialUI current input type : " + spatialInputType);

                //if (spatialInputType == SpatialInputType.StateChangedThisFrame)
                    //Debug.Log("<color=green>SpatialUI state changed this frame!!</color>");

                var inputLocalRotation = m_CurrentSpatialActionMapInput.localRotationQuaternion.quaternion;
                var ghostDeviceRotation = inputLocalRotation * Quaternion.Inverse(m_InitialSpatialLocalRotation);
                s_SpatialMenuUi.UpdateGhostDeviceRotation(ghostDeviceRotation);

                /*
                if (m_Transitioning && m_State == State.navigatingSubMenuContent && Mathf.Abs(Mathf.DeltaAngle(m_InitialSpatialLocalRotation.x, actionMapInput.localRotationQuaternion.quaternion.x)) > k_WristReturnRotationThreshold)
                {
                    //Debug.LogWarning("<color=green>" + Mathf.DeltaAngle(m_InitialSpatialLocalRotation.z, actionMapInput.localRotationQuaternion.quaternion.z) + "</color>");
                    SetSpatialScrollStartingConditions(actionMapInput.localPosition.vector3, actionMapInput.localRotationQuaternion.quaternion);
                    ReturnToPreviousMenuLevel();
                    return;
                }
                */

                if (m_Transitioning && m_SpatialMenuState == SpatialMenuState.navigatingSubMenuContent && m_ContinuousDirectionalVelocityTracker.directionalDivergence > 10.08f) // TODO: return to 0.08f
                {
                    //Debug.LogWarning("<color=green>" + Mathf.DeltaAngle(m_InitialSpatialLocalRotation.z, actionMapInput.localRotationQuaternion.quaternion.z) + "</color>");
                    ReturnToPreviousMenuLevel();
                    return;
                }

                 //TODO : enable after constrained horizontal scrolling is functioning
                var currentSpatialActionMapLocalPosition = m_CurrentSpatialActionMapInput.localPosition.vector3;
                //var currentInputMovingForward = Vector3.Dot(currentSpatialActionMapLocalPosition, m_HomeSectionSpatialScrollStartLocalPosition) > 0.5f; // validate the the current position has move away from the user, in a forward direction
                //Debug.LogError("<color=green>"+ Vector3.Dot(currentSpatialActionMapLocalPosition.normalized, m_HomeSectionSpatialScrollStartLocalPosition.normalized) + "</color>");
                //if (m_SpatialinterfaceState == SpatialinterfaceState.navigatingTopLevel && currentInputMovingForward && Vector3.Magnitude(m_HomeSectionSpatialScrollStartLocalPosition - currentSpatialActionMapLocalPosition) > kSubMenuNavigationTranslationTriggerThreshold)
                if (m_SpatialMenuState == SpatialMenuState.navigatingTopLevel && Vector3.Magnitude(m_HomeSectionSpatialScrollStartLocalPosition - currentSpatialActionMapLocalPosition) > kSubMenuNavigationTranslationTriggerThreshold)
                {
                    /*
                    if (m_Transitioning)
                    {
                        var x = Vector3.Magnitude(m_HomeSectionSpatialScrollStartLocalPosition - m_CurrentSpatialActionMapInput.localPosition.vector3);
                        //Debug.LogError("<color=green>"+ x + "</color>");
                        Debug.Log("Crossed translation threshold");
                        spatialinterfaceState = SpatialinterfaceState.navigatingSubMenuContent;
                    }

                    return;
                    */
                }

                // utilize the YAW rotation of the input device to cycle through menu items
                // Scale the cycling speed based on the dot-base divergence from the initial starting angle
                // Will need to consider how to handle a user starting at a steep angle initially, baesd upon how far they scroll in the opposite direction.
                // In other words, if the user rotates beyond the max estimated threshold, we offset the initial starting angle by that amount, so when returning their rotation to the original extreme angle
                // They will have offset their "neutral" rotation position, and have newfound room to rotate/advance in the original "extreme" rotation direction
                if (m_SpatialMenuState != SpatialMenuState.navigatingSubMenuContent)
                {
                    //highlightedMenuElements = s_SpatialMenuData[highlightedPosition].spatialMenuElements;
                    if (true && !m_Transitioning)
                    {
                        s_SpatialMenuUi.HighlightSingleElementInHomeMenu(0);
                        highlightedMenuElements = s_SpatialMenuData[0].spatialMenuElements;
                    }
                    else if (s_SpatialMenuUi.spatialInterfaceInputMode != SpatialMenuUI.SpatialInterfaceInputMode.GhostRay && m_HighlightedMenuElements != null)
                    {
                        var menuElementCount = s_SpatialMenuData.Count;
                        if (menuElementCount == 0)
                            return;

                        spatialScrollData = this.PerformLocalCardinallyConstrainedSpatialScroll(SpatialInputModule.SpatialCardinalScrollDirection.LocalX, node, spatialScrollStartPosition, spatialScrollOrigin.position, k_SpatialScrollVectorLength, SpatialInputModule.ScrollRepeatType.Clamped, menuElementCount, menuElementCount);
                        var normalizedRepeatingPosition = spatialScrollData.normalizedLoopingPositionUnconstrained;
                        if (!Mathf.Approximately(normalizedRepeatingPosition, 0f))
                        {
                            if (spatialScrollData.highlightedMenuElementPositionUnconstrained != m_HighlightedMenuElementPosition)
                            {
                                Debug.LogWarning("<color=purple>changing home menu element highlighted position to : </color>" + spatialScrollData.highlightedMenuElementPositionUnconstrained);
                                //ebug.LogWarning("<color=green>Performing spatial scrolling of TOP LEVEL / HOME contents</color>");
                                m_HighlightedMenuElementPosition = spatialScrollData.highlightedMenuElementPositionUnconstrained;
                                s_SpatialMenuUi.HighlightSingleElementInHomeMenu(spatialScrollData.loopingHighlightedMenuElementPositionXConstrained);
                            }
                        }
                    }
                }
/*
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
*/
                else if (m_SpatialMenuState == SpatialMenuState.navigatingSubMenuContent)
                {
                    if (m_CurrentSpatialActionMapInput.cancel.isHeld)
                    {
                        Debug.LogWarning("Cancel button was just presset on node : " + node);
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
                            m_HighlightedMenuElementPosition = spatialScrollData.highlightedMenuElementPositionUnconstrained;//(int) (menuElementCount * normalizedRepeatingPosition);
                            s_SpatialMenuUi.HighlightSingleElementInCurrentMenu(spatialScrollData.loopingHighlightedMenuElementPositionYConstrained);
                            //m_SpatialMenuUi.HighlightSingleElementInCurrentMenu(m_HighlightedMenuElementPosition);
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

            if (!m_CurrentSpatialActionMapInput.showMenu.positive.isHeld)
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
