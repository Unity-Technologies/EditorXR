﻿#if UNITY_EDITOR
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
    /// <summary>
    /// The SpatialMenu's UI/View-controller
    /// Drives the SpatialMenu visuals elements
    /// </summary>
    public sealed class SpatialMenuUI : SpatialUIView, IAdaptPosition, IConnectInterfaces, IUsesRaycastResults
    {
        const float k_AllowedGazeDivergence = 45f;
        const float k_AllowedMaxHMDDistanceDivergence = 0.95f; // Distance at which the menu will move towards
        const float k_AllowedMinHMDDistanceDivergence = 0.3f; // Distance at which the menu will move away
        const float k_TargetAdaptiveRestDistance = 0.75f; // Distance at which the menu will be re-positioned
        const bool k_onlyMoveWhenOutOfFocus = true;
        const bool k_alwaysRepositionIfOutOfFocus = true;

        readonly string k_TranslationInputModeName = "Spatial Input Mode";
        readonly string k_ExternalRayBasedInputModeName = "External Ray Input Mode";
        readonly string k_TriggerRotationInputModeName = "Trigger Rotation Input Mode";
        readonly List<SpatialMenuElement> currentlyDisplayedMenuElements = new List<SpatialMenuElement>();

        [Header("Common UI")]
        [SerializeField]
        CanvasGroup m_MainCanvasGroup;

        [SerializeField]
        Transform m_Background;

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

        Material m_ReturnToPreviousBackgroundMaterial;

        bool m_Visible;
        SpatialInterfaceInputMode m_PreviousSpatialInterfaceInputMode;
        SpatialInterfaceInputMode m_SpatialInterfaceInputMode;
        SpatialMenu.SpatialMenuState m_SpatialMenuState;

        Vector3 m_HomeTextBackgroundOriginalLocalScale;
        float m_HomeSectionTimelineDuration;
        float m_HomeSectionTimelineStoppingTime;
        Vector3 m_OriginalSurroundingArrowsContainerLocalPosition;
        SpatialMenuElement m_CurrentlyHighlightedMenuElement;

        // Adaptive Position related fields
        bool m_InFocus;
        bool m_BeingMoved;

        Coroutine m_InFocusCoroutine;
        Coroutine m_HomeSectionTitlesBackgroundBordersTransitionCoroutine;
        Coroutine m_ReturnToPreviousLevelTransitionCoroutine;

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
                    spatialMenuState = SpatialMenu.SpatialMenuState.hidden;
            }
        }

        // Adaptive position related members
        public Transform adaptiveTransform { get { return transform; } }
        public float allowedDegreeOfGazeDivergence { get { return k_AllowedGazeDivergence; } }
        public float allowedMinDistanceDivergence { get { return k_AllowedMinHMDDistanceDivergence; } }
        public float allowedMaxDistanceDivergence { get { return k_AllowedMaxHMDDistanceDivergence; } }
        public float adaptivePositionRestDistance { get { return k_TargetAdaptiveRestDistance; } }
        public bool allowAdaptivePositioning { get; private set; }
        public bool resetAdaptivePosition { get; set; }
        public Coroutine adaptiveElementRepositionCoroutine { get; set; }
        public bool onlyMoveWhenOutOfFocus { get { return k_onlyMoveWhenOutOfFocus; } }
        public bool repositionIfOutOfFocus { get { return k_alwaysRepositionIfOutOfFocus; } }

        // Section name string, corresponding element collection, currentlyHighlightedState
        public List<SpatialMenu.SpatialMenuData> spatialMenuData { private get; set; }
        public List<SpatialMenu.SpatialMenuElementContainer> highlightedMenuElements;

        // SpatialMenu actions/delegates/funcs
        public Action returnToPreviousMenuLevel { get; set; }
        public Action<SpatialMenu.SpatialMenuState> changeMenuState { get; set; }

        public Transform subMenuContainer { get { return m_SubMenuContainer; } }

        public SpatialMenu.SpatialMenuState spatialMenuState
        {
            set
            {
                // If the previous state was hidden, reset the state of the UI
                if (m_SpatialMenuState == SpatialMenu.SpatialMenuState.hidden && value == SpatialMenu.SpatialMenuState.navigatingTopLevel)
                    Reset();

                if (m_SpatialMenuState == value)
                    return;

                m_SpatialMenuState = value;
                m_CurrentlyHighlightedMenuElement = null;

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
                        const string kAwaktingText = "Awaiting Selection";
                        m_HomeSectionDescription.text = kAwaktingText;
                        foreach (var element in currentlyDisplayedMenuElements)
                        {
                            // Perform animated hiding of elements
                            element.visible = false;
                        }
                        break;
                }
            }
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
                    case SpatialInterfaceInputMode.TriggerAffordanceRotation:
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
            }
        }

        void Start()
        {
            visible = false;
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
            m_OriginalSurroundingArrowsContainerLocalPosition = m_SurroundingArrowsContainer.localPosition;

            m_HomeSectionTimelineDuration = (float) m_RevealTimelinePlayable.duration;
            m_HomeSectionTimelineStoppingTime = m_HomeSectionTimelineDuration * 0.5f;
            Reset();

            m_BackButton.OnHoverEnter = OnBackButtonHoverEnter;
            m_BackButton.OnHoverExit = OnBackButtonHoverExit;
            m_BackButton.OnSelected = OnBackButtonSelected;

            // When setting up the SpatialMenuUI the visibility will be defaulted to false
            // Manually set the backer bool to true, in order to perform a manual hiding of the menu in this case
            m_Visible = true;
            visible = false;
        }

        void Reset()
        {
            ForceClearHomeMenuElements();
            ForceClearSubMenuElements();

            m_InputModeText.text = k_TranslationInputModeName;
            m_Director.playableAsset = m_RevealTimelinePlayable;
            m_HomeSectionCanvasGroup.alpha = 1f;
            m_HomeTextBackgroundInnerCanvasGroup.alpha = 1f;
            m_HomeSectionTitlesBackgroundBorderCanvasGroup.alpha = 1f;

            m_Director.time = 0f;
            m_Director.Evaluate();
        }

        void ReturnToPreviousMenuLevel()
        {
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

        void UpdateDirector()
        {
            if (m_Director.time <= m_HomeSectionTimelineStoppingTime)
            {
                m_Director.time = m_Director.time += Time.unscaledDeltaTime;
                m_Director.Evaluate();
            }
        }

        public void SectionTitleButtonSelected(Node node)
        {
            changeMenuState(SpatialMenu.SpatialMenuState.navigatingSubMenuContent);
        }

        void DisplayHomeSectionContents()
        {
            m_BackButton.allowInteraction = false;
            this.RestartCoroutine(ref m_HomeSectionTitlesBackgroundBordersTransitionCoroutine, AnimateTopAndBottomCenterBackgroundBorders(true));

            // Proxy sub-menu/dynamicHUD menu element(s) display
            m_HomeTextBackgroundTransform.localScale = m_HomeTextBackgroundOriginalLocalScale;
            m_HomeSectionDescription.gameObject.SetActive(true);

            currentlyDisplayedMenuElements.Clear();
            var homeMenuElementParent = (RectTransform)m_HomeMenuLayoutGroup.transform;
            foreach (var data in spatialMenuData)
            {
                var instantiatedPrefabTransform = ObjectUtils.Instantiate(m_SectionTitleElementPrefab).transform as RectTransform;
                var providerMenuElement = instantiatedPrefabTransform.GetComponent<SpatialMenuElement>();
                this.ConnectInterfaces(instantiatedPrefabTransform);
                providerMenuElement.Setup(homeMenuElementParent, () => { }, data.spatialMenuName, null);
                currentlyDisplayedMenuElements.Add(providerMenuElement);
                providerMenuElement.selected = SectionTitleButtonSelected;
                providerMenuElement.highlightedAction = OnButtonHighlighted;
                providerMenuElement.parentMenuData = data;
            }
        }

        void DisplayHighlightedSubMenuContents()
        {
            m_BackButton.allowInteraction = true;
            ForceClearHomeMenuElements();
            foreach (var menuData in spatialMenuData)
            {
                if (menuData.highlighted)
                {
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
                        var providerMenuElement = instantiatedPrefab.GetComponent<SpatialMenuElement>();
                        this.ConnectInterfaces(providerMenuElement);
                        providerMenuElement.Setup(subMenuContainer, () => Debug.Log("Setting up SubMenu : " + subMenuElement.name), subMenuElement.name, subMenuElement.tooltipText);
                        currentlyDisplayedMenuElements.Add(providerMenuElement);
                        subMenuElement.VisualElement = providerMenuElement;
                        providerMenuElement.parentMenuData = menuData;
                        providerMenuElement.visible = true;
                        providerMenuElement.selected = subMenuElement.correspondingFunction;
                    }

                    break;
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

        public void HighlightElementInCurrentlyDisplayedMenuSection(int elementOrderPosition)
        {
            var menuElementCount = currentlyDisplayedMenuElements.Count;
            for (int i = 0; i < menuElementCount; ++i)
            {
                if (currentlyDisplayedMenuElements.Count > i && currentlyDisplayedMenuElements[i] != null)
                {
                    var element = currentlyDisplayedMenuElements[i];
                    element.highlighted = i == elementOrderPosition;

                    if (i == elementOrderPosition)
                    {
                        m_CurrentlyHighlightedMenuElement = element;

                        if (m_SpatialMenuState == SpatialMenu.SpatialMenuState.navigatingTopLevel)
                            m_HomeSectionDescription.text = element.parentMenuData.spatialMenuDescription;
                    }
                }
            }
        }

        public void SelectCurrentlyHighlightedElement(Node node)
        {
            if (m_CurrentlyHighlightedMenuElement != null)
            {
                // Spatial/cyclical/trackpad/thumbstick selection will set this reference
                m_CurrentlyHighlightedMenuElement.selected(node);
            }
            else
            {
                // Search for an element that is being hovered,
                // if no currentlyHighlightedMenuElement was assigned via a spatial/cyclical input means
                for (int i = 0; i < currentlyDisplayedMenuElements.Count; ++i)
                {
                    if (currentlyDisplayedMenuElements[i] != null)
                    {
                        var highlighted = currentlyDisplayedMenuElements[i].highlighted;
                        if (highlighted)
                        {
                            currentlyDisplayedMenuElements[i].selected(node);
                            return;
                        }
                    }
                }
            }
        }

        public void ReturnToPreviousInputMode()
        {
            // This is a convenience function that allows for a previous-non-override input state to be restored,
            // if an override input state was previously set (ray-based alternate hand interaction, etc)
            spatialInterfaceInputMode = m_PreviousSpatialInterfaceInputMode;
        }

        void Update()
        {
            // HACK: Don't ask... horizontal layout group refused to play nicely without this...
            // The horiz layout groupd would refuse to space elements as expected without this
            m_HomeMenuLayoutGroup.spacing = 1 % Time.unscaledDeltaTime * 0.01f;

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
                // UI hiding animation has finished, perform final cleanup
                m_HomeTextBackgroundInnerTransform.localScale = new Vector3(1f, 1f, 1f);
                m_SubMenuContentsCanvasGroup.alpha = 0f;

                StopAllCoroutines();
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
            }
            else if (m_SpatialMenuState == SpatialMenu.SpatialMenuState.navigatingTopLevel)
            {
                m_SubMenuContentsCanvasGroup.alpha = 0f;
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

        IEnumerator AnimateTopAndBottomCenterBackgroundBorders(bool visible)
        {
            var currentAlpha = m_HomeSectionCanvasGroup.alpha;
            var targetAlpha = visible ? 1f : 0f;
            var transitionAmount = 0f;
            var transitionSubtractMultiplier = 5f;
            while (transitionAmount < 1f)
            {
                var smoothTransition = MathUtilsExt.SmoothInOutLerpFloat(transitionAmount);
                var newAlpha = Mathf.Lerp(currentAlpha, targetAlpha, smoothTransition);
                m_HomeSectionCanvasGroup.alpha = newAlpha;
                m_HomeTextBackgroundInnerCanvasGroup.alpha = newAlpha;
                m_HomeSectionTitlesBackgroundBorderCanvasGroup.alpha = newAlpha;
                transitionAmount += Time.deltaTime * transitionSubtractMultiplier;
                yield return null;
            }

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
                // Continuously looping when visible is intentional
                // Maintain the sustained pulse while hovering the back button
                // When disabling the return-to-previous UI, the coroutine restart will stop the pulse
                this.Pulse(Node.None, m_SustainedHoverUIElementPulse);
                yield return null;
            }

            m_ReturnToPreviousLevelTransitionCoroutine = null;
        }
    }
}
#endif