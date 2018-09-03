#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR.Menus
{
    public sealed class SpatialMenuUI : SpatialUIView, IAdaptPosition, IConnectInterfaces, IUsesRaycastResults
    {
        const float k_DistanceOffset = 0.75f;
        const float k_AllowedGazeDivergence = 45f;

        readonly string k_TranslationInputModeName = "Spatial Input Mode";
        readonly string k_ExternalRayBasedInputModeName = "External Ray Input Mode";
        readonly string k_TriggerRotationInputModeName = "Trigger Rotation Input Mode";

        [Header("Common UI")]
        [SerializeField]
        CanvasGroup m_MainCanvasGroup;

        [SerializeField]
        Transform m_Background;

        //[SerializeField]
        //TextMeshProUGUI m_MenuTitleText;

        [SerializeField]
        TextMeshProUGUI m_InputModeText;

        [Header("Home Section")]
        [SerializeField]
        Transform m_HomeMenuContainer;

        [SerializeField]
        CanvasGroup m_HomeTextCanvasGroup;

        [SerializeField]
        HorizontalLayoutGroup m_HomeMenuLayoutGroup;

        [SerializeField]
        Transform m_HomeTextBackgroundTransform;

        [SerializeField]
        Transform m_HomeTextBackgroundInnerTransform;

        [SerializeField]
        CanvasGroup m_HomeSectionTitlesBackgroundBorderCanvasGroup;

        [SerializeField]
        CanvasGroup m_HomeTextBackgroundInnerCanvasGroup;

        [SerializeField]
        TextMeshProUGUI m_HomeSectionDescription;

        [SerializeField]
        CanvasGroup m_HomeSectionCanvasGroup;

        [Header("SubMenu Section")]
        [SerializeField]
        Transform m_SubMenuContainer;

        [SerializeField]
        CanvasGroup m_SubMenuContentsCanvasGroup;

        [Header("Prefabs")]
        [SerializeField]
        GameObject m_SectionTitleElementPrefab;

        [SerializeField]
        GameObject m_SubMenuElementPrefab;

        [Header("Animation")]
        [SerializeField]
        PlayableDirector m_Director;

        [SerializeField]
        PlayableAsset m_RevealTimelinePlayable;

        [Header("Secondary Visuals")]
        [SerializeField]
        Transform m_SurroundingArrowsContainer;

        [Header("SurroundingBorderButtons")]
        [SerializeField]
        SpatialMenuBackButton m_BackButton;

        [Header("Return To Previous Level Visuals")]
        [SerializeField]
        GameObject m_BackButtonVisualsContainer;

        [SerializeField]
        CanvasGroup m_BackButtonVisualsCanvasGroup;

        [SerializeField]
        Transform m_ReturnToPreviousLevelText;

        [SerializeField]
        Renderer m_ReturnToPreviousBackgroundRenderer;

        Transform m_ReturnToPreviousBackground;
        Material m_ReturnToPreviousBackgroundMaterial;

        bool m_Visible;
        SpatialInterfaceInputMode m_PreviousSpatialInterfaceInputMode;
        SpatialInterfaceInputMode m_SpatialInterfaceInputMode;
        SpatialMenu.SpatialMenuState m_SpatialMenuState;

        Vector3 m_HomeTextBackgroundOriginalLocalScale;
        Vector3 m_HomeBackgroundOriginalLocalScale;
        float m_HomeSectionTimelineDuration;
        float m_HomeSectionTimelineStoppingTime;
        float m_OriginalHomeSectionTitleTextSpacing;
        Vector3 m_OriginalSurroundingArrowsContainerLocalPosition;
        Vector3 m_originalBackButtonIconLocalScale;
        ISpatialMenuElement m_CurrentlyHighlightedMenuElement;

        // Adaptive Position related fields
        bool m_InFocus;
        bool m_BeingMoved;

        Coroutine m_VisibilityCoroutine;
        Coroutine m_InFocusCoroutine;
        Coroutine m_HomeSectionTitlesBackgroundBordersTransitionCoroutine;
        Coroutine m_ReturnToPreviousLevelTransitionCoroutine;

        // Secondary visuals
        int m_PreviouslyHighlightedElementOrderPosition;

        // Reference set by the controller in the Setup method
        //readonly Dictionary<ISpatialMenuProvider, ISpatialMenuElement> m_ProviderToHomeMenuElements = new Dictionary<ISpatialMenuProvider, ISpatialMenuElement>();

        readonly List<TextMeshProUGUI> m_SectionNameTexts = new List<TextMeshProUGUI>();

        readonly List<ISpatialMenuElement> currentlyDisplayedMenuElements = new List<ISpatialMenuElement>();

        public static List<ISpatialMenuProvider> spatialMenuProviders;

        // Core SpatialUI interface implementation
        // All SpatialUI elements, be it this SpatialMenu, or a SpatialContextUI popup, etc, will implement this core functionality
        // public SpatialUIToggle m_SpatialPinToggle { get; set; }

        // Adaptive position related members
        public Transform adaptiveTransform { get { return transform; } }
        public float allowedDegreeOfGazeDivergence { get { return k_AllowedGazeDivergence; } }
        public float distanceOffset { get { return k_DistanceOffset; } }
        public AdaptivePositionModule.AdaptivePositionData adaptivePositionData { get; set; }
        public bool allowAdaptivePositioning { get; private set; }
        public bool resetAdaptivePosition { get; set; }

        // Section name string, corresponding element collection, currentlyHighlightedState
        public List<SpatialMenu.SpatialMenuData> spatialMenuData { get; set; }
        public List<SpatialMenu.SpatialMenuElement> highlightedMenuElements;
        public string highlightedSectionName { get; set; }

        // SpatialMenu actions/delegates/funcs
        public Action returnToPreviousMenuLevel { get; set; }
        public Action<SpatialMenu.SpatialMenuState> changeMenuState { get; set; }

        bool visible
        {
            set
            {
                if (m_Visible == value)
                    return;

                m_Visible = value;
                gameObject.SetActive(m_Visible);
                allowAdaptivePositioning = m_Visible;
                resetAdaptivePosition = m_Visible;
                m_BackButtonVisualsContainer.SetActive(false);

                m_MainCanvasGroup.interactable = m_Visible;
                m_MainCanvasGroup.blocksRaycasts = m_Visible;

                if (!m_Visible)
                {
                    spatialMenuState = SpatialMenu.SpatialMenuState.hidden;
                }
            }
        }

        //public ISpatialMenuProvider highlightedTopLevelMenuProvider { private get; set; }
        public SpatialMenu.SpatialMenuState spatialMenuState
        {
            set
            {
                // If the previous state was hidden, reset the state of the UI
                if (m_SpatialMenuState == SpatialMenu.SpatialMenuState.hidden && value == SpatialMenu.SpatialMenuState.navigatingTopLevel)
                    Reset();

                if (m_SpatialMenuState == value)
                    return;

                // If the previous state was Hidden & this object was disabled, enable the UI gameobject
                //if (m_SpatialinterfaceState == SpatialinterfaceState.hidden && !gameObject.activeSelf)
                    //gameObject.SetActive(true);

                m_SpatialMenuState = value;
                m_CurrentlyHighlightedMenuElement = null;
                Debug.LogWarning("Switching spatial menu state to " + m_SpatialMenuState);

                switch (m_SpatialMenuState)
                {
                    case SpatialMenu.SpatialMenuState.navigatingTopLevel:
                        visible = true;
                        DisplayHomeSectionContents();
                        break;
                    case SpatialMenu.SpatialMenuState.navigatingSubMenuContent:
                        DisplayHighlightedSubMenuContents();
                        break;
                    case SpatialMenu.SpatialMenuState.hidden:
                        Debug.LogWarning("<color=orange>setting SpatialMenuUI state to hidden</color>");
                        m_HomeSectionDescription.text = "Awaiting Selection";
                        foreach (var element in currentlyDisplayedMenuElements)
                        {
                            // Perform animated hiding of elements
                            element.visible = false;
                        }
                        break;
                }
            }
        }

        public bool directorBeyondHomeSectionDuration
        {
            get { return m_Director.time > m_HomeSectionTimelineDuration; }
        }

        public SpatialInterfaceInputMode spatialInterfaceInputMode
        {
            get { return m_SpatialInterfaceInputMode; }
            set
            {
                if (m_SpatialInterfaceInputMode == value)
                    return;

                m_PreviousSpatialInterfaceInputMode = m_SpatialInterfaceInputMode;
                m_SpatialInterfaceInputMode = value;

                switch (m_SpatialInterfaceInputMode)
                {
                    case SpatialInterfaceInputMode.Translation:
                        m_InputModeText.text = k_TranslationInputModeName;
                        break;
                    case SpatialInterfaceInputMode.Ray:
                        m_InputModeText.text = k_ExternalRayBasedInputModeName;
                        break;
                    case SpatialInterfaceInputMode.TriggerRotation:
                        // Item highlighting via rotation of the trigger affordance beyond a threshold
                        m_InputModeText.text = k_TriggerRotationInputModeName;
                        break;
                }
            }
        }

        public bool inFocus
        {
            get { return m_InFocus; }
            set
            {
                if (m_InFocus == value)
                    return;

                m_InFocus = value;
            }
        }

        public bool beingMoved
        {
            set
            {
                if (m_BeingMoved == value)
                    return;

                m_BeingMoved = value;
                this.RestartCoroutine(ref m_VisibilityCoroutine, AnimateVisibility());
            }
        }

        public Transform homeMenuContainer { get { return m_HomeMenuContainer; } }

        public Transform subMenuContainer { get { return m_SubMenuContainer; } }

        void Start()
        {
            visible = false;
            m_ReturnToPreviousBackground = m_ReturnToPreviousBackgroundRenderer.transform;
            m_ReturnToPreviousBackgroundMaterial = MaterialUtils.GetMaterialClone(m_ReturnToPreviousBackgroundRenderer);
            m_ReturnToPreviousBackgroundRenderer.material = m_ReturnToPreviousBackgroundMaterial;
            m_ReturnToPreviousBackgroundMaterial.SetFloat("_Blur", 0);
        }

        void OnBackButtonHoverEnter()
        {
            this.RestartCoroutine(ref m_ReturnToPreviousLevelTransitionCoroutine, AnimateReturnToPreviousMenuLevelVisuals(true));
        }

        void OnBackButtonHoverExit()
        {
            this.RestartCoroutine(ref m_ReturnToPreviousLevelTransitionCoroutine, AnimateReturnToPreviousMenuLevelVisuals(false));
        }

        void OnBackButtonSelected()
        {
            this.RestartCoroutine(ref m_ReturnToPreviousLevelTransitionCoroutine, AnimateReturnToPreviousMenuLevelVisuals(false));
            ReturnToPreviousMenuLevel();
        }

        public void Setup()
        {
            m_HomeTextBackgroundOriginalLocalScale = m_HomeTextBackgroundTransform.localScale;
            m_HomeBackgroundOriginalLocalScale = m_Background.localScale;

            // TODO remove serialized inspector references for home menu section titles, use instantiated prefabs only
            //m_SectionNameTexts.Clear();

            m_OriginalHomeSectionTitleTextSpacing = m_HomeMenuLayoutGroup.spacing;
            m_OriginalSurroundingArrowsContainerLocalPosition = m_SurroundingArrowsContainer.localPosition;

            m_HomeSectionTimelineDuration = (float) m_RevealTimelinePlayable.duration;
            m_HomeSectionTimelineStoppingTime = m_HomeSectionTimelineDuration * 0.5f;
            Reset();

            m_BackButton.OnHoverEnter = OnBackButtonHoverEnter;
            m_BackButton.OnHoverExit = OnBackButtonHoverExit;
            m_BackButton.OnSelected = OnBackButtonSelected;

            m_originalBackButtonIconLocalScale = m_BackButton.transform.localScale;

            // When setting up the SpatialMenuUI the visibility will be defaulted to false
            // Manually set the backer bool to true, in order to perform a manual hiding of the menu in this case
            m_Visible = true;
            visible = false;
        }

        public void Reset()
        {
            Debug.Log("Resetting state in Spatial menu UI " + m_SpatialMenuState);
            ForceClearHomeMenuElements();
            ForceClearSubMenuElements();

            m_InputModeText.text = k_TranslationInputModeName;
            m_Director.playableAsset = m_RevealTimelinePlayable;
            m_HomeSectionCanvasGroup.alpha = 1f;
            m_HomeTextBackgroundInnerCanvasGroup.alpha = 1f;
            m_HomeSectionTitlesBackgroundBorderCanvasGroup.alpha = 1f;

            // Director related
            m_Director.time = 0f;
            m_Director.Evaluate();
        }

        void ReturnToPreviousMenuLevel()
        {
            Debug.LogWarning("Return to previous menu level");
            returnToPreviousMenuLevel();
            HideSubMenuElements();
        }

        void ForceClearHomeMenuElements()
        {
            var homeMenuElementParent = m_HomeMenuLayoutGroup.transform;
            var childrenToDelete = homeMenuElementParent.GetComponentsInChildren<Transform>().Where(x => x != homeMenuElementParent);
            var childCount = childrenToDelete.Count();
            if (childCount > 0)
            {
                foreach (var child in childrenToDelete)
                {
                    if (child != null && child.gameObject != null)
                        ObjectUtils.Destroy(child.gameObject);
                }
            }
        }

        void ForceClearSubMenuElements()
        {
            var childrenToDelete = m_SubMenuContainer.GetComponentsInChildren<Transform>().Where(x => x != m_SubMenuContainer);
            var childCount = childrenToDelete.Count();
            if (childCount > 0)
            {
                foreach (var child in childrenToDelete)
                {
                    if (child != null && child.gameObject != null)
                        ObjectUtils.Destroy(child.gameObject);
                }
            }
        }

        void HideSubMenuElements()
        {
            foreach (var element in currentlyDisplayedMenuElements)
            {
                element.visible = false;
            }
        }

        public void UpdateProviderMenuElements()
        {

        }

        public void UpdateDirector()
        {
            if (m_Director.time <= m_HomeSectionTimelineStoppingTime)
            {
                m_Director.time = m_Director.time += Time.unscaledDeltaTime;
                m_Director.Evaluate();
            }
        }

        void DisplayHomeSectionContents()
        {
            m_BackButton.allowInteraction = false;
            this.RestartCoroutine(ref m_HomeSectionTitlesBackgroundBordersTransitionCoroutine, AnimateTopAndBottomCenterBackgroundBorders(true));

            // Proxy sub-menu/dynamicHUD menu element(s) display
            m_HomeTextBackgroundTransform.localScale = m_HomeTextBackgroundOriginalLocalScale;
            m_HomeSectionDescription.gameObject.SetActive(true);

            //m_ProviderToHomeMenuElements.Clear();
            currentlyDisplayedMenuElements.Clear();
            var homeMenuElementParent = (RectTransform)m_HomeMenuLayoutGroup.transform;
            for (int i = 0; i < spatialMenuData.Count; ++i)
            {
                Debug.Log("<color=green>Displaying home section contents</color>");
                var instantiatedPrefabTransform = ObjectUtils.Instantiate(m_SectionTitleElementPrefab).transform as RectTransform;
                var providerMenuElement = instantiatedPrefabTransform.GetComponent<ISpatialMenuElement>();
                this.ConnectInterfaces(instantiatedPrefabTransform);
                providerMenuElement.Setup(homeMenuElementParent, () => { }, spatialMenuData[i].spatialMenuName, null);
                currentlyDisplayedMenuElements.Add(providerMenuElement);
                providerMenuElement.selected = SectionTitleButtonSelected;
                providerMenuElement.highlightedAction = OnButtonHighlighted;
                providerMenuElement.parentMenuData = spatialMenuData[i];
                //m_ProviderToHomeMenuElements[menuData] = providerMenuElement;
            }
        }

        public void SectionTitleButtonSelected()
        {
            changeMenuState(SpatialMenu.SpatialMenuState.navigatingSubMenuContent);
        }

        public void DisplayHighlightedSubMenuContents()
        {
            m_BackButton.allowInteraction = true;

            Debug.Log("Displaying sub-menu elements");
            ForceClearHomeMenuElements();
            const float subMenuElementHeight = 0.022f; // TODO source height from individual sub-menu element height, not arbitrary value
            foreach (var menuData in spatialMenuData)
            {
                if (menuData.highlighted)
                {
                    // m_SubMenuText.text = m_HighlightedTopLevelMenuProvider.spatialTableElements[0].name;
                    // TODO display all sub menu contents here

                    currentlyDisplayedMenuElements.Clear();
                    var deleteOldChildren = m_SubMenuContainer.GetComponentsInChildren<Transform>().Where( x => x != m_SubMenuContainer);
                    foreach (var child in deleteOldChildren)
                    {
                        if (child != null && child.gameObject != null)
                            ObjectUtils.Destroy(child.gameObject);
                    }

                    foreach (var subMenuElement in menuData.spatialMenuElements)
                    {
                        var instantiatedPrefab = ObjectUtils.Instantiate(m_SubMenuElementPrefab).transform as RectTransform;
                        var providerMenuElement = instantiatedPrefab.GetComponent<ISpatialMenuElement>();
                        this.ConnectInterfaces(providerMenuElement);
                        providerMenuElement.Setup(subMenuContainer, () => Debug.Log("Setting up SubMenu : " + subMenuElement.name), subMenuElement.name, subMenuElement.tooltipText);
                        currentlyDisplayedMenuElements.Add(providerMenuElement);
                        subMenuElement.VisualElement = providerMenuElement;
                        providerMenuElement.parentMenuData = menuData;
                        providerMenuElement.visible = true;
                        providerMenuElement.selected = subMenuElement.correspondingFunction;
                    }
                }
            }

            m_HomeSectionDescription.gameObject.SetActive(false);
            this.RestartCoroutine(ref m_HomeSectionTitlesBackgroundBordersTransitionCoroutine, AnimateTopAndBottomCenterBackgroundBorders(false));
        }

        void OnButtonHighlighted(SpatialMenu.SpatialMenuData menuData)
        {
            m_HomeSectionDescription.text = menuData.spatialMenuDescription;
            const string kNoMenuHighlightedText = "Select a menu option";
            string highlightedMenuDataText = kNoMenuHighlightedText;
            foreach (var data in spatialMenuData)
            {
                if (data.highlighted)
                {
                    highlightedMenuDataText = data.spatialMenuDescription;
                    break;
                }
            }

            m_HomeSectionDescription.text = highlightedMenuDataText;
        }

        public void HighlightHomeSectionMenuElement(int providerCollectionPosition)
        {
            var highlightedMenuData = spatialMenuData[providerCollectionPosition];
            m_HomeSectionDescription.text = highlightedMenuData.spatialMenuDescription;
            //highlightedTopLevelMenuProvider = provider;

            for (int i = 0; i < spatialMenuData.Count; ++i)
            {
                var targetSize = i == providerCollectionPosition ? Vector3.one : Vector3.one * 0.5f;
                // switch to highlighted bool set in ISpatialMenuElement
                //menuData.gameObject.transform.localScale = targetSize;
                if (currentlyDisplayedMenuElements.Count >= i && currentlyDisplayedMenuElements[i] != null)
                    currentlyDisplayedMenuElements[i].gameObject.transform.localScale = targetSize;
            }
        }

        public void HighlightSingleElementInHomeMenu(int elementOrderPosition)
        {
            //Debug.Log("<color=blue>HighlightSingleElementInCurrentMenu in SpatialMenuUI : " + elementOrderPosition + "</color>");
            var menuElementCount = 3;// highlightedMenuElements.Count;
            for (int i = 0; i < menuElementCount; ++i)
            {

                //var x = m_ProviderToMenuElements[m_HighlightedTopLevelMenuProvider];

                if (currentlyDisplayedMenuElements.Count > i && currentlyDisplayedMenuElements[i] != null)
                {
                    currentlyDisplayedMenuElements[i].highlighted = i == elementOrderPosition;

                    if (i == elementOrderPosition)
                    {
                        m_HomeSectionDescription.text = currentlyDisplayedMenuElements[i].parentMenuData.spatialMenuDescription;
                        Debug.LogWarning("Highlighting home level menu element : " + currentlyDisplayedMenuElements[i].gameObject.name);
                    }
                }

                //m_HighlightedTopLevelMenuProvider.spatialTableElements[i].name = i == highlightedButtonPosition ? "Highlighted" : "Not";
            }

            // TODO unify the spatialMenuData
            m_PreviouslyHighlightedElementOrderPosition = elementOrderPosition;
        }

        public void HighlightElementInCurrentlyDisplayedMenuSection(int elementOrderPosition)
        {
            //Debug.Log("<color=blue>HighlightSingleElementInCurrentMenu in SpatialMenuUI : " + elementOrderPosition + "</color>");
            var menuElementCount = 3;// highlightedMenuElements.Count;
            for (int i = 0; i < menuElementCount; ++i)
            {

                //var x = m_ProviderToMenuElements[m_HighlightedTopLevelMenuProvider];

                if (currentlyDisplayedMenuElements.Count > i && currentlyDisplayedMenuElements[i] != null)
                {
                    var element = currentlyDisplayedMenuElements[i];
                    element.highlighted = i == elementOrderPosition;

                    if (i == elementOrderPosition)
                    {
                        m_HomeSectionDescription.text = element.parentMenuData.spatialMenuDescription;
                        m_CurrentlyHighlightedMenuElement = element;
                        Debug.LogWarning("Highlighting home level menu element : " + element.gameObject.name);
                    }
                }

                //m_HighlightedTopLevelMenuProvider.spatialTableElements[i].name = i == highlightedButtonPosition ? "Highlighted" : "Not";
            }

            // TODO unify the spatialMenuData
            m_PreviouslyHighlightedElementOrderPosition = elementOrderPosition;
        }

        public void SelectCurrentlyHighlightedElement()
        {
            m_CurrentlyHighlightedMenuElement.selected();
        }

        public void HighlightSingleElementInCurrentMenu(int elementOrderPosition)
        {
            //Debug.Log("<color=blue>HighlightSingleElementInCurrentMenu in SpatialMenuUI : " + elementOrderPosition + "</color>");
            var menuElementCount = highlightedMenuElements.Count;
            for (int i = 0; i < menuElementCount; ++i)
            {

                //var x = m_ProviderToMenuElements[m_HighlightedTopLevelMenuProvider];

                if (currentlyDisplayedMenuElements.Count > i && currentlyDisplayedMenuElements[i] != null)
                    currentlyDisplayedMenuElements[i].highlighted = i == elementOrderPosition;
                
                //m_HighlightedTopLevelMenuProvider.spatialTableElements[i].name = i == highlightedButtonPosition ? "Highlighted" : "Not";
            }
        }

        public void HighlightNextElementInCurrentMenu()
        {
            Debug.LogError("Highlighting NEXT element via circular rotation");

            var newElementHighlightPosition = m_PreviouslyHighlightedElementOrderPosition + 1 < currentlyDisplayedMenuElements.Count ? m_PreviouslyHighlightedElementOrderPosition : 0;
            //currentlyDisplayedMenuElements[newElementHighlightPosition].highlighted = true;

            //HighlightSingleElementInCurrentMenu(newElementHighlightPosition);
            //return;

            for (int i = 0; i < currentlyDisplayedMenuElements.Count; ++i)
            {
                currentlyDisplayedMenuElements[i].highlighted = i == newElementHighlightPosition;
            }
        }

        public void HighlightPreviousElementInCurrentMenu()
        {
            Debug.LogError("Highlighting PREVIOUS element via circular rotation");

            var newElementHighlightPosition = m_PreviouslyHighlightedElementOrderPosition + 1 < currentlyDisplayedMenuElements.Count ? m_PreviouslyHighlightedElementOrderPosition : 0;
            //currentlyDisplayedMenuElements[newElementHighlightPosition].highlighted = true;

            //HighlightSingleElementInCurrentMenu(newElementHighlightPosition);
            //return;

            for (int i = 0; i < currentlyDisplayedMenuElements.Count; ++i)
            {
                currentlyDisplayedMenuElements[i].highlighted = i == newElementHighlightPosition;
            }
        }

        public void ReturnToPreviousInputMode()
        {
            // This is a convenience function that allows for a previous-non-override input state to be restored, if an override input state was previously set (ray-based alternate hand interaction, etc)
            spatialInterfaceInputMode = m_PreviousSpatialInterfaceInputMode;
        }

        void Update()
        {
            m_HomeMenuLayoutGroup.spacing = 1 % Time.unscaledDeltaTime * 0.01f; // Don't ask... horizontal layout group refused to play nicely without this... b'cause magic mysetery something
            if (m_SpatialMenuState == SpatialMenu.SpatialMenuState.hidden && m_Director.time <= m_HomeSectionTimelineDuration)
            {
                // Performed an animated hide of any currently displayed UI
                m_Director.time = m_Director.time += Time.unscaledDeltaTime;
                m_Director.Evaluate();

                m_SubMenuContentsCanvasGroup.alpha = Mathf.Clamp01(m_SubMenuContentsCanvasGroup.alpha - Time.unscaledDeltaTime * 4);
                var newHomeSectionAlpha = Mathf.Clamp01(m_HomeSectionCanvasGroup.alpha - Time.unscaledDeltaTime * 4);
                m_HomeSectionCanvasGroup.alpha = newHomeSectionAlpha;
                m_HomeTextBackgroundInnerCanvasGroup.alpha = newHomeSectionAlpha;
                m_HomeSectionTitlesBackgroundBorderCanvasGroup.alpha = newHomeSectionAlpha;
            }
            else if (m_Director.time > m_HomeSectionTimelineDuration)
            {
                // UI hiding animation has finished, perform final cleanup.  TODO: optimze for pooling and a lesser GC impact
                //m_Director.time = 0f;
                m_HomeTextBackgroundInnerTransform.localScale = new Vector3(1f, 1f, 1f);
                m_SubMenuContentsCanvasGroup.alpha = 0f;

                StopAllCoroutines();
                //HideSubMenu();
                m_Director.Evaluate();
                ForceClearHomeMenuElements();
                ForceClearSubMenuElements();
                visible = false;
            }
            else if (m_SpatialMenuState == SpatialMenu.SpatialMenuState.navigatingSubMenuContent)
            {
                m_SubMenuContentsCanvasGroup.alpha = 1f;
                // Scale background based on number of sub-menu elements
                var targetScale = highlightedMenuElements != null ? highlightedMenuElements.Count * 1.05f : 1f;
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
            else if (m_SpatialMenuState == SpatialMenu.SpatialMenuState.navigatingTopLevel)
            {
                m_SubMenuContentsCanvasGroup.alpha = 0f;
                //Debug.LogWarning("SpatialUI : <color=green>Navigating top level content</color>");
                var targetScale = 1f;
                var timeMultiplier = 24;
                if (m_HomeTextBackgroundInnerTransform.localScale.y > targetScale)
                {
                    if (m_HomeTextBackgroundInnerTransform.localScale.y - Time.unscaledDeltaTime * timeMultiplier < targetScale)
                    {
                        m_HomeTextBackgroundInnerTransform.localScale = new Vector3(1f, targetScale, 1f);
                        m_SubMenuContentsCanvasGroup.alpha = 0f;
                        ForceClearSubMenuElements();
                        return;
                    }

                    var newScale = new Vector3(m_HomeTextBackgroundInnerTransform.localScale.x, m_HomeTextBackgroundInnerTransform.localScale.y - Time.unscaledDeltaTime * timeMultiplier, m_HomeTextBackgroundInnerTransform.localScale.z);
                    m_HomeTextBackgroundInnerTransform.localScale = newScale;
                    m_SubMenuContentsCanvasGroup.alpha -= Time.unscaledDeltaTime * 10;
                }
                else
                {
                    UpdateDirector();
                }
            }
        }

        IEnumerator AnimateFocusVisuals()
        {
            var currentScale = transform.localScale;
            var targetScale = m_InFocus ? Vector3.one : Vector3.one * 0.5f;
            var transitionAmount = 0f; // this should account for the magnitude difference between the highlightedYPositionOffset, and the current magnitude difference between the local Y and the original Y
            var transitionSubtractMultiplier = 5f;
            while (transitionAmount < 1f)
            {
                var smoothTransition = MathUtilsExt.SmoothInOutLerpFloat(transitionAmount);
                transform.localScale = Vector3.Lerp(currentScale, targetScale, smoothTransition);
                transitionAmount += Time.deltaTime * transitionSubtractMultiplier;
                yield return null;
            }

            transform.localScale = targetScale;
            m_InFocusCoroutine = null;
        }

        IEnumerator AnimateVisibility()
        {
            var speedScalar = m_BeingMoved ? 2f : 4f;
            var currentAlpha = m_MainCanvasGroup.alpha;
            var targetMainCanvasAlpha = m_BeingMoved ? 0.25f : 1f;

            var currentHomeTextAlpha = m_HomeTextCanvasGroup.alpha;
            var targetHomeTextAlpha = m_BeingMoved ? 0f : 1f;

            //var currentBackgroundLocalScale = m_Background.localScale;
            //var targetBackgroundLocalScale = Vector3.one * (m_BeingMoved ? 0.75f : 1f);

            var currentHomeBackgroundLocalScale = m_HomeTextBackgroundTransform.localScale;
            var targetHomeBackgroundLocalScale = m_BeingMoved ? new Vector3(m_HomeTextBackgroundOriginalLocalScale.x, 0f, 1f) : m_HomeTextBackgroundOriginalLocalScale;
            //var targetPosition = show ? (moveToAlternatePosition ? m_AlternateLocalPosition : m_OriginalLocalPosition) : Vector3.zero;
            //var targetScale = show ? (moveToAlternatePosition ? m_OriginalLocalScale : m_OriginalLocalScale * k_AlternateLocalScaleMultiplier) : Vector3.zero;
            //var currentPosition = transform.localPosition;
            //var currentIconScale = m_IconContainer.localScale;
            //var targetIconContainerScale = show ? m_OriginalIconContainerLocalScale : Vector3.zero;
            var transitionAmount = 0f;
            //var currentScale = transform.localScale;

            if (!m_BeingMoved)
            {
                var delayBeforeReveal = 0.5f;
                while (delayBeforeReveal > 0)
                {
                    this.Pulse(Node.None, m_AdaptivePositionPulse);
                    // Pause before revealing
                    delayBeforeReveal -= Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            /*
            while (transitionAmount < 1)
            {
                var shapedAmount = MathUtilsExt.SmoothInOutLerpFloat(transitionAmount += Time.unscaledDeltaTime * speedScalar);
                //m_Director.time = shapedAmount;
                //m_IconContainer.localScale = Vector3.Lerp(currentIconScale, targetIconContainerScale, shapedAmount);
                //transform.localPosition = Vector3.Lerp(currentPosition, targetPosition, shapedAmount);
                //transform.localScale = Vector3.Lerp(currentScale, targetScale, shapedAmount);
                //m_Background.localScale = Vector3.Lerp(currentBackgroundLocalScale, targetBackgroundLocalScale, shapedAmount);

                m_MainCanvasGroup.alpha = Mathf.Lerp(currentAlpha, targetMainCanvasAlpha, shapedAmount);

                shapedAmount *= shapedAmount; // increase beginning & end anim emphasis
                m_HomeTextCanvasGroup.alpha = Mathf.Lerp(currentHomeTextAlpha, targetHomeTextAlpha, shapedAmount);

                m_HomeTextBackgroundTransform.localScale = Vector3.Lerp(currentHomeBackgroundLocalScale, targetHomeBackgroundLocalScale, shapedAmount);
                yield return null;
            }

            //m_IconContainer.localScale = targetIconContainerScale;
            //transform.localScale = targetScale;
            //transform.localPosition = targetPosition;

            m_MainCanvasGroup.alpha = targetMainCanvasAlpha;
            */
            //m_Background.localScale = targetBackgroundLocalScale;

            m_VisibilityCoroutine = null;
        }

        IEnumerator AnimateTopAndBottomCenterBackgroundBorders(bool visible)
        {
            var currentAlpha = m_HomeSectionCanvasGroup.alpha;
            var targetAlpha = visible ? 1f : 0f;
            var transitionAmount = 0f;
            var transitionSubtractMultiplier = 5f;
            //m_HomeMenuLayoutGroup.enabled = false;
            while (transitionAmount < 1f)
            {
                var smoothTransition = MathUtilsExt.SmoothInOutLerpFloat(transitionAmount);
                var newAlpha = Mathf.Lerp(currentAlpha, targetAlpha, smoothTransition);
                m_HomeSectionCanvasGroup.alpha = newAlpha;
                m_HomeTextBackgroundInnerCanvasGroup.alpha = newAlpha;
                m_HomeSectionTitlesBackgroundBorderCanvasGroup.alpha = newAlpha;
                //m_SurroundingArrowsContainer.localScale = Vector3.one + (Vector3.one * Mathf.Sin(transitionAmount * 2) * 0.1f);
                transitionAmount += Time.deltaTime * transitionSubtractMultiplier;
                yield return null;
                //LayoutRebuilder.ForceRebuildLayoutImmediate(m_HomeMenuLayoutGroup.transform as RectTransform);
            }

            //m_HomeMenuLayoutGroup.enabled = true;
            m_HomeSectionCanvasGroup.alpha = targetAlpha;
            m_HomeSectionTitlesBackgroundBordersTransitionCoroutine = null;
        }

        IEnumerator AnimateReturnToPreviousMenuLevelVisuals(bool visible)
        {
            if (visible)
                this.Pulse(Node.None, m_HighlightUIElementPulse);

            m_BackButton.highlighted = visible;
            m_BackButtonVisualsContainer.SetActive(true);

            var currentArrowsContainerLocalPosition = m_SurroundingArrowsContainer.localPosition;
            var targetArrowsContainerLocalPosition = visible ? new Vector3(0f, 0f, -0.02f) : m_OriginalSurroundingArrowsContainerLocalPosition;

            const float kHiddenTextLocalPosition = 0.125f;
            var currentAlpha = m_BackButtonVisualsCanvasGroup.alpha;
            var targetAlpha = visible ? 1f : 0f;
            var transitionAmount = 0f;
            var transitionSpeedMultiplier = visible ? 10f : 5f; // Faster when revealing, slower when hiding
            var currentTextLocalPosition = m_ReturnToPreviousLevelText.localPosition;
            var targetTextLocalPosition = visible ? Vector3.zero : new Vector3(0f, 0f, kHiddenTextLocalPosition); // recede when hiding
            while (transitionAmount < 1f)
            {
                var smoothTransition = MathUtilsExt.SmoothInOutLerpFloat(transitionAmount);
                var newAlpha = Mathf.Lerp(currentAlpha, targetAlpha, smoothTransition);
                m_BackButtonVisualsCanvasGroup.alpha = newAlpha;
                m_ReturnToPreviousLevelText.localPosition = Vector3.Lerp(currentTextLocalPosition, targetTextLocalPosition, smoothTransition);

                m_SurroundingArrowsContainer.localPosition = Vector3.Lerp(currentArrowsContainerLocalPosition, targetArrowsContainerLocalPosition, smoothTransition);

                //m_ReturnToPreviousBackground.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, smoothTransition);
                m_ReturnToPreviousBackgroundMaterial.SetFloat("_Blur", newAlpha * 10);

                transitionAmount += Time.deltaTime * transitionSpeedMultiplier;
                // Perform the sustained pulse here, in order to have a proper blending between the initial hover pulse, and the sustained (on hover) pulse
                this.Pulse(Node.None, m_SustainedHoverUIElementPulse);
                yield return null;
            }

            m_BackButtonVisualsContainer.SetActive(visible);
            m_BackButtonVisualsCanvasGroup.alpha = targetAlpha;

            while (visible)
            {
                // Maintain the sustained pulse while hovering the back button
                this.Pulse(Node.None, m_SustainedHoverUIElementPulse);
                yield return null;
            }

            m_ReturnToPreviousLevelTransitionCoroutine = null;
        }
    }
}
#endif
