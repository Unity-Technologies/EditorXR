using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor.Experimental.EditorVR;
using UnityEditor.Experimental.EditorVR.Actions;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;
using UnityEngine.Playables;
using UnityEngine.UI;
using SpatialinterfaceState = UnityEditor.Experimental.EditorVR.SpatialMenu.SpatialinterfaceState;

public class SpatialMenuUI : MonoBehaviour, IAdaptPosition
{
    const float k_DistanceOffset = 0.75f;
    const float k_AllowedGazeDivergence = 45f;

    readonly string k_TranslationInputModeName = "Spatial Input Mode";
    readonly string k_RayBasedInputModeName = "Ray-based Input Mode";
    readonly string k_RotationInputModeName = "Rotation Input Mode";
    readonly string k_BCIInputModeName = "Brain Input Mode";

    public enum SpatialInterfaceInputMode
    {
        Translation,
        Rotation,
        Ray,
        BCI
    }

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

    [Header("Ghost Input Device")]
    [SerializeField]
    SpatialMenuGhostVisuals m_SpatialUIGhostVisuals;

    [Header("Surrounding Arrows")]
    [SerializeField]
    Transform m_SurroundingArrowsContainer;

    bool m_Visible;
    SpatialInterfaceInputMode m_SpatialInterfaceInputMode;
    SpatialinterfaceState m_SpatialinterfaceState;

    Vector3 m_HomeTextBackgroundOriginalLocalScale;
    Vector3 m_HomeBackgroundOriginalLocalScale;
    float m_HomeSectionTimelineDuration;
    float m_HomeSectionTimelineStoppingTime;
    float m_OriginalHomeSectionTitleTextSpacing;

    // Adaptive Position related fields
    bool m_InFocus;
    bool m_BeingMoved;

    Coroutine m_VisibilityCoroutine;
    Coroutine m_InFocusCoroutine;
    Coroutine m_HomeSectionTitlesBackgroundBordersTransitionCoroutine;

    // Reference set by the controller in the Setup method
    //readonly Dictionary<ISpatialMenuProvider, ISpatialMenuElement> m_ProviderToHomeMenuElements = new Dictionary<ISpatialMenuProvider, ISpatialMenuElement>();

    readonly List<TextMeshProUGUI> m_SectionNameTexts = new List<TextMeshProUGUI>();

    readonly List<ISpatialMenuElement> currentlyDisplayedMenuElements = new List<ISpatialMenuElement>();

    public static List<ISpatialMenuProvider> spatialMenuProviders;

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

    bool visible
    {
        get { return m_Visible; }
        set
        {
            if (m_Visible == value)
                return;

            m_Visible = value;
            gameObject.SetActive(m_Visible);
            allowAdaptivePositioning = m_Visible;
            resetAdaptivePosition = m_Visible;

            if (!m_Visible)
                spatialinterfaceState = SpatialinterfaceState.hidden;
        }
    }

    //public ISpatialMenuProvider highlightedTopLevelMenuProvider { private get; set; }
    public SpatialinterfaceState spatialinterfaceState
    {
        set
        {
            // If the previous state was hidden, reset the state of the UI
            if (m_SpatialinterfaceState == SpatialinterfaceState.hidden && value == SpatialinterfaceState.navigatingTopLevel)
                Reset();

            if (m_SpatialinterfaceState == value)
                return;

            // If the previous state was Hidden & this object was disabled, enable the UI gameobject
            //if (m_SpatialinterfaceState == SpatialinterfaceState.hidden && !gameObject.activeSelf)
                //gameObject.SetActive(true);

            m_SpatialinterfaceState = value;
            Debug.LogError("Switching spatial menu state to " + m_SpatialinterfaceState);

            switch (m_SpatialinterfaceState)
            {
                case SpatialinterfaceState.navigatingTopLevel:
                    visible = true;
                    DisplayHomeSectionContents();
                    break;
                case SpatialinterfaceState.navigatingSubMenuContent:
                    DisplayHighlightedSubMenuContents();
                    break;
                case SpatialinterfaceState.hidden:
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
        set
        {
            if (m_SpatialInterfaceInputMode == value)
                return;

            m_SpatialInterfaceInputMode = value;

            switch (m_SpatialInterfaceInputMode)
            {
                case SpatialInterfaceInputMode.Translation:
                    m_InputModeText.text = k_TranslationInputModeName;
                    m_SpatialUIGhostVisuals.spatialInteractionType = SpatialMenuGhostVisuals.SpatialInteractionType.touch;
                    break;
                case SpatialInterfaceInputMode.Ray:
                    m_InputModeText.text = k_RayBasedInputModeName;
                    m_SpatialUIGhostVisuals.spatialInteractionType = SpatialMenuGhostVisuals.SpatialInteractionType.ray;
                    break;
                case SpatialInterfaceInputMode.BCI:
                    m_InputModeText.text = k_BCIInputModeName;
                    m_SpatialUIGhostVisuals.spatialInteractionType = SpatialMenuGhostVisuals.SpatialInteractionType.bci;
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

            //if (value != m_InFocus)
            //this.RestartCoroutine(ref m_InFocusCoroutine, AnimateFocusVisuals());

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

    //public GameObject menuElementPrefab { get { return m_MenuElementPrefab; } }

    //public GameObject subMenuElementPrefab { get { return m_SubMenuElementPrefab; } }

    public Transform subMenuContainer { get { return m_SubMenuContainer; } }

    void Start()
    {
        visible = false;
    }

    public void Setup()
    {
        m_HomeTextBackgroundOriginalLocalScale = m_HomeTextBackgroundTransform.localScale;
        m_HomeBackgroundOriginalLocalScale = m_Background.localScale;

        // TODO remove serialized inspector references for home menu section titles, use instantiated prefabs only
        //m_SectionNameTexts.Clear();

        m_OriginalHomeSectionTitleTextSpacing = m_HomeMenuLayoutGroup.spacing;

        m_HomeSectionTimelineDuration = (float) m_RevealTimelinePlayable.duration;
        m_HomeSectionTimelineStoppingTime = m_HomeSectionTimelineDuration * 0.5f;
        Reset();
    }

    public void Reset()
    {
        Debug.LogWarning("Resetting state in Spatial menu UI " + m_SpatialinterfaceState);
        ClearHomeMenuElements();
        ClearSubMenuElements();

        m_InputModeText.text = k_TranslationInputModeName;
        m_Director.playableAsset = m_RevealTimelinePlayable;
        m_HomeSectionCanvasGroup.alpha = 1f;
        m_HomeTextBackgroundInnerCanvasGroup.alpha = 1f;
        m_HomeSectionTitlesBackgroundBorderCanvasGroup.alpha = 1f;

        // Director related
        m_Director.time = 0f;
        m_Director.Evaluate();

        // Hack that fixes the home section menu element positions not being recalculated when first revealed
        //m_HomeMenuLayoutGroup.enabled = false;
        //m_HomeMenuLayoutGroup.enabled = true;
        m_SpatialUIGhostVisuals.spatialInteractionType = SpatialMenuGhostVisuals.SpatialInteractionType.touch;
    }

    void ClearHomeMenuElements()
    {
        var homeMenuElementParent = m_HomeMenuLayoutGroup.transform;
        var childrenToDelete = homeMenuElementParent.GetComponentsInChildren<Transform>().Where((x) => x != homeMenuElementParent);
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

    void ClearSubMenuElements()
    {
        var childrenToDelete = m_SubMenuContainer.GetComponentsInChildren<Transform>().Where((x) => x != m_SubMenuContainer);
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

    public void UpdateProviderMenuElements()
    {

    }

    public void UpdateGhostDeviceRotation(Quaternion newRotation)
    {
        m_SpatialUIGhostVisuals.UpdateRotation(newRotation);
    }

    public void UpdateDirector()
    {
        if (m_Director.time <= m_HomeSectionTimelineStoppingTime)
        {
            m_Director.time = m_Director.time += Time.unscaledDeltaTime;
            m_Director.Evaluate();
        }
    }

    /*
    public void UpdateSectionNames(List<ISpatialMenuProvider> spatialMenuProviders)
    {
        for (int i = 0; i < spatialMenuProviders.Count; ++i)
        {
            m_SectionNameTexts[i].text = spatialMenuProviders[i].spatialMenuName;
        }
    }
    */

    void DisplayHomeSectionContents()
    {
        m_SpatialUIGhostVisuals.SetPositionOffset(Vector3.zero);
        m_SpatialUIGhostVisuals.spatialInteractionType = SpatialMenuGhostVisuals.SpatialInteractionType.touch;
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
            providerMenuElement.Setup(homeMenuElementParent, () => { }, spatialMenuData[i].spatialMenuName, null);
            currentlyDisplayedMenuElements.Add(providerMenuElement);
            //m_ProviderToHomeMenuElements[menuData] = providerMenuElement;
        }
    }

    public void DisplayHighlightedSubMenuContents()
    {
        ClearHomeMenuElements();
        const float subMenuElementHeight = 0.022f; // TODO source height from individual sub-menu element height, not arbitrary value
        int subMenuElementCount = 0;
        foreach (var menuData in spatialMenuData)
        {
            if (menuData.highlighted)
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

                foreach (var subMenuElement in menuData.spatialMenuElements)
                {
                    ++subMenuElementCount;
                    var instantiatedPrefab = ObjectUtils.Instantiate(m_SubMenuElementPrefab).transform as RectTransform;
                    var providerMenuElement = instantiatedPrefab.GetComponent<ISpatialMenuElement>();
                    providerMenuElement.Setup(subMenuContainer, () => Debug.Log("Setting up SubMenu : " + subMenuElement.name), subMenuElement.name, subMenuElement.tooltipText);
                    currentlyDisplayedMenuElements.Add(providerMenuElement);
                    subMenuElement.VisualElement = providerMenuElement;
                    providerMenuElement.visible = true;
                }

                //.Add(provider, providerMenuElement);
                //instantiatedPrefab.transform.SetParent(m_SubMenuContainer);
                //instantiatedPrefab.localRotation = Quaternion.identity;
                //instantiatedPrefab.localPosition = Vector3.zero;
                //instantiatedPrefab.localScale = Vector3.one;
            }

            //menuData.Value.gameObject.SetActive(false);
        }

        var newGhostInputDevicePositionOffset = new Vector3(0f, subMenuElementHeight * subMenuElementCount, 0f);
        m_SpatialUIGhostVisuals.SetPositionOffset(newGhostInputDevicePositionOffset);
        m_HomeSectionDescription.gameObject.SetActive(false);
        this.RestartCoroutine(ref m_HomeSectionTitlesBackgroundBordersTransitionCoroutine, AnimateTopAndBottomCenterBackgroundBorders(false));
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

    public void HighlightSingleElementInCurrentMenu(int elementOrderPosition)
    {
        //Debug.Log("<color=blue>HighlightSingleElementInCurrentMenu in SpatialMenuUI : " + elementOrderPosition + "</color>");
        var menuElementCount = highlightedMenuElements.Count;
        for (int i = 0; i < menuElementCount; ++i)
        {

            //var x = m_ProviderToMenuElements[m_HighlightedTopLevelMenuProvider];
            currentlyDisplayedMenuElements[i].highlighted = i == elementOrderPosition;
            //m_HighlightedTopLevelMenuProvider.spatialTableElements[i].name = i == highlightedButtonPosition ? "Highlighted" : "Not";
        }
    }

    void Update()
    {
        m_HomeMenuLayoutGroup.spacing = 1 % Time.unscaledDeltaTime * 0.01f; // Don't ask... horizontal layout group refused to play nicely without this... b'cause magic mysetery something
        //Debug.Log("<color=yellow> SpatialMenuUI state : " + m_SpatialinterfaceState + " : director time : " + m_Director.time + "</color>");
        if (m_SpatialinterfaceState == SpatialinterfaceState.hidden && m_Director.time <= m_HomeSectionTimelineDuration)
        {
            //Debug.LogWarning("<color=orange>Hiding spatial menu UI</color>");
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
            Debug.LogWarning("<color=green>Finished hiding spatial menu UI</color>");
            // UI hiding animation has finished, perform final cleanup.  TODO: optimze for pooling and a lesser GC impact
            //m_Director.time = 0f;
            m_HomeTextBackgroundInnerTransform.localScale = new Vector3(1f, 1f, 1f);
            m_SubMenuContentsCanvasGroup.alpha = 0f;

            this.StopAllCoroutines();
            //HideSubMenu();
            m_Director.Evaluate();
            ClearHomeMenuElements();
            ClearSubMenuElements();
            visible = false;
        }
        else if (m_SpatialinterfaceState == SpatialinterfaceState.navigatingSubMenuContent)
        {
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
        else if (m_SpatialinterfaceState == SpatialinterfaceState.navigatingTopLevel)
        {
            //Debug.LogWarning("SpatialUI : <color=green>Navigating top level content</color>");
            var targetScale = 1f;
            var timeMultiplier = 24;
            if (m_HomeTextBackgroundInnerTransform.localScale.y > targetScale)
            {
                if (m_HomeTextBackgroundInnerTransform.localScale.y - Time.unscaledDeltaTime * timeMultiplier < targetScale)
                {
                    m_HomeTextBackgroundInnerTransform.localScale = new Vector3(1f, targetScale, 1f);
                    m_SubMenuContentsCanvasGroup.alpha = 0f;
                    ClearSubMenuElements();
                    return;
                }

                var newScale = new Vector3(m_HomeTextBackgroundInnerTransform.localScale.x, m_HomeTextBackgroundInnerTransform.localScale.y - Time.unscaledDeltaTime * timeMultiplier, m_HomeTextBackgroundInnerTransform.localScale.z);
                m_HomeTextBackgroundInnerTransform.localScale = newScale;
                m_SubMenuContentsCanvasGroup.alpha -= Time.unscaledDeltaTime * 10;
            }
            else
            {
                UpdateDirector();
                //m_HomeTextBackgroundInnerTransform.localScale = new Vector3(1f, targetScale, 1f);
                //m_SubMenuContentsCanvasGroup.alpha = 0f;
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
            m_SurroundingArrowsContainer.localScale = Vector3.one + (Vector3.one * Mathf.Sin(transitionAmount * 2) * 0.1f);
            transitionAmount += Time.deltaTime * transitionSubtractMultiplier;
            yield return null;
            //LayoutRebuilder.ForceRebuildLayoutImmediate(m_HomeMenuLayoutGroup.transform as RectTransform);
        }

        //m_HomeMenuLayoutGroup.enabled = true;
        m_HomeSectionCanvasGroup.alpha = targetAlpha;
        m_HomeSectionTitlesBackgroundBordersTransitionCoroutine = null;
    }
}
