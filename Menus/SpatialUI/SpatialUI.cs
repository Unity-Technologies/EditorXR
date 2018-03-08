#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.InputNew;
using UnityEngine.Playables;

namespace UnityEditor.Experimental.EditorVR
{
    public class SpatialUI : MonoBehaviour, IAdaptPosition, ICustomActionMap, IControlSpatialScrolling, IUsesNode, IUsesRayOrigin
    {
        // TODO expose as a user preference, for spatial UI distance
        const float k_DistanceOffset = 0.75f;
        const float k_AllowedGazeDivergence = 45f;
        const float k_SpatialQuickToggleDuration = 0.25f;

        enum State
        {
            hidden,
            navigatingTopLevel,
            navigatingSubMenuContent
        }

        [SerializeField]
        ActionMap m_ActionMap;

        [SerializeField]
        Transform m_DemoMenuElements;

        [SerializeField]
        TextMeshProUGUI m_SubMenuText;

        [Header("Common UI")]
        [SerializeField]
        CanvasGroup m_MainCanvasGroup;

        [SerializeField]
        Transform m_Background;

        [SerializeField]
        TextMeshProUGUI m_MenuTitleText;

        [Header("Home Section")]
        [SerializeField]
        Transform m_HomeMenuContainer;

        [SerializeField]
        CanvasGroup m_HomeTextCanvasGroup;

        [SerializeField]
        Transform m_HomeTextBackgroundTransform;

        [SerializeField]
        TextMeshProUGUI m_HomeSectionDescription;

        [SerializeField]
        List<TextMeshProUGUI> m_SectionNameTexts = new List<TextMeshProUGUI>();

        [Header("Prefabs")]
        [SerializeField]
        GameObject m_MenuElementPrefab;

        [Header("Animation")]
        [SerializeField]
        PlayableDirector m_Director;

        [SerializeField]
        PlayableAsset m_RevealTimelinePlayable;

        State m_State;

        bool m_Visible;
        bool m_BeingMoved;
        bool m_InFocus;
        Vector3 m_HomeTextBackgroundOriginalLocalScale;
        Vector3 m_HomeBackgroundOriginalLocalScale;

        Coroutine m_VisibilityCoroutine;
        Coroutine m_InFocusCoroutine;

        // Spatial rotation members
        Quaternion m_InitialSpatialLocalRotation;

        readonly Dictionary<ISpatialMenuProvider, SpatialUIMenuElement> m_ProviderToMenuElements = new Dictionary<ISpatialMenuProvider, SpatialUIMenuElement>();

        bool visible
        {
            get { return m_Visible; }

            set
            {
                if (m_Visible == value)
                    return;

                m_Visible = value;

                gameObject.SetActive(m_Visible);
            }
        }

        public Transform rayOrigin { get; set; }
        public Node node { get; set; }

        // Action Map interface members
        public ActionMap actionMap { get { return m_ActionMap; } }
        public bool ignoreActionMapInputLocking { get; private set; }

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

        public readonly List<ISpatialMenuProvider> m_spatialMenuProviders = new List<ISpatialMenuProvider>();

        public bool inFocus
        {
            set
            {
                //if (value != m_InFocus)
                    //this.RestartCoroutine(ref m_InFocusCoroutine, AnimateFocusVisuals());

                m_InFocus = value;
            }
        }

        public bool beingMoved
        {
            get { return m_BeingMoved; }
            set
            {
                if (m_BeingMoved != value)
                {
                    m_BeingMoved = value;
                    this.RestartCoroutine(ref m_VisibilityCoroutine, AnimateVisibility());
                }

                m_Director.Evaluate();
            }
        }

        public class SpatialUITableElement
        {
            public SpatialUITableElement(string elementName, Sprite icon, Action correspondingFunction)
            {
                this.name = elementName;
                this.icon = icon;
                this.correspondingFunction = correspondingFunction;
            }

            public string name { get; set; }

            public Sprite icon { get; set; }

            public Action correspondingFunction { get; set; }
        }

        void Awake()
        {
            m_HomeTextBackgroundOriginalLocalScale = m_HomeTextBackgroundTransform.localScale;
            m_HomeBackgroundOriginalLocalScale = m_Background.localScale;

            // TODO remove serialized inspector references for home menu section titles, use instantiated prefabs only
            m_SectionNameTexts.Clear();
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

            Debug.LogError("Adding a provider : " + provider.spatialMenuName);
            m_spatialMenuProviders.Add(provider);

            var providerMenuElement = ObjectUtils.Instantiate(m_MenuElementPrefab, m_HomeMenuContainer).GetComponent<SpatialUIMenuElement>();
            providerMenuElement.Setup(providerMenuElement.transform, () => Debug.LogError("Setting up : " + provider.spatialMenuName), provider.spatialMenuName);
            m_ProviderToMenuElements.Add(provider, providerMenuElement);

            //m_MenuTitleText.text = provider.spatialMenuName;

            //UpdateSectionNames();
        }

        void UpdateSectionNames()
        {
            for (int i = 0; i < m_spatialMenuProviders.Count; ++i)
            {
                m_SectionNameTexts[i].text = m_spatialMenuProviders[i].spatialMenuName;
            }
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

        IEnumerator AnimateVisibility()
        {
            var speedScalar = m_BeingMoved ? 2f : 4f;
            var currentAlpha = m_MainCanvasGroup.alpha;
            var targetMainCanvasAlpha = m_BeingMoved ? 0.25f : 1f;

            var currentHomeTextAlpha = m_HomeTextCanvasGroup.alpha;
            var targetHomeTextAlpha = m_BeingMoved ? 0f : 1f;

            var currentBackgroundLocalScale = m_Background.localScale;
            var targetBackgroundLocalScale = Vector3.one * (m_BeingMoved ? 0.75f : 1f);

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

            while (transitionAmount < 1)
            {
                var shapedAmount = MathUtilsExt.SmoothInOutLerpFloat(transitionAmount += Time.unscaledDeltaTime * speedScalar);
                //m_Director.time = shapedAmount;
                //m_IconContainer.localScale = Vector3.Lerp(currentIconScale, targetIconContainerScale, shapedAmount);
                //transform.localPosition = Vector3.Lerp(currentPosition, targetPosition, shapedAmount);
                //transform.localScale = Vector3.Lerp(currentScale, targetScale, shapedAmount);
                m_MainCanvasGroup.alpha = Mathf.Lerp(currentAlpha, targetMainCanvasAlpha, shapedAmount);

                m_Background.localScale = Vector3.Lerp(currentBackgroundLocalScale, targetBackgroundLocalScale, shapedAmount);

                shapedAmount *= shapedAmount; // increase beginning & end anim emphasis
                m_HomeTextCanvasGroup.alpha = Mathf.Lerp(currentHomeTextAlpha, targetHomeTextAlpha, shapedAmount);

                m_HomeTextBackgroundTransform.localScale = Vector3.Lerp(currentHomeBackgroundLocalScale, targetHomeBackgroundLocalScale, shapedAmount);
                yield return null;
            }

            //m_IconContainer.localScale = targetIconContainerScale;
            //transform.localScale = targetScale;
            //transform.localPosition = targetPosition;

            m_MainCanvasGroup.alpha = targetMainCanvasAlpha;
            m_Background.localScale = targetBackgroundLocalScale;

            m_VisibilityCoroutine = null;
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

        void DisplaySubMenuContents(ISpatialMenuProvider provider)
        {
            m_SubMenuText.text = provider.spatialTableElements[0].name;
        }

        public void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl)
        {
            Debug.Log("processing input in SpatialUI");

            const float kSubMenuNavigationTranslationTriggerThreshold = 0.075f;
            var actionMapInput = (SpatialUIInput)input;

            // This block is only processed after a frame with both trigger buttons held has been detected
            if (spatialScrollData != null && actionMapInput.cancel.wasJustPressed)
            {
                consumeControl(actionMapInput.cancel);
                consumeControl(actionMapInput.show);
                consumeControl(actionMapInput.select);

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

                if (m_State == State.navigatingTopLevel)
                    spatialScrollStartPosition = actionMapInput.localPosition.vector3;
            }

            // Detect the initial activation of the relevant Spatial input
            if ((actionMapInput.show.wasJustPressed && actionMapInput.select.wasJustPressed) ||
                (actionMapInput.show.wasJustPressed && actionMapInput.select.isHeld) ||
                (actionMapInput.show.isHeld && actionMapInput.select.wasJustPressed))
            {
                m_State = State.navigatingTopLevel;
                spatialScrollStartPosition = actionMapInput.localPosition.vector3; // rayOrigin.position;
                
                // Cache the current starting rotation, current deltaAngle will be calculated relative to this rotation
                m_InitialSpatialLocalRotation = actionMapInput.localRotationQuaternion.quaternion;

                // DEMO
                // Proxy sub-menu/dynamicHUD menu element(s) display
                m_DemoMenuElements.gameObject.SetActive(false);
                m_HomeTextBackgroundTransform.localScale = m_HomeTextBackgroundOriginalLocalScale;
                m_SectionNameTexts[0].transform.localScale = Vector3.one;
                m_SectionNameTexts[1].transform.localScale = Vector3.one;
                m_HomeSectionDescription.gameObject.SetActive(true);

                // Director related
                m_Director.time = 0f;
                m_Director.Evaluate();
            }

            //m_Director.time = m_Director.time += Time.unscaledDeltaTime;
            //m_Director.Evaluate();

            if (actionMapInput.show.isHeld)
            {
                visible = true;

                if (m_State == State.navigatingTopLevel && Vector3.Magnitude(spatialScrollStartPosition - actionMapInput.localPosition.vector3) > kSubMenuNavigationTranslationTriggerThreshold)
                {
                    //Debug.LogError("Crossed translation threshold");
                    m_State = State.navigatingSubMenuContent;
                    m_SectionNameTexts[0].transform.localScale = Vector3.zero;
                    m_SectionNameTexts[1].transform.localScale = Vector3.zero;
                    m_HomeTextBackgroundTransform.localScale = new Vector3(m_HomeTextBackgroundOriginalLocalScale.x, m_HomeTextBackgroundOriginalLocalScale.y * 6, 1f);
                    m_HomeSectionDescription.gameObject.SetActive(false);

                    m_DemoMenuElements.gameObject.SetActive(true);
                    DisplaySubMenuContents(m_spatialMenuProviders[1]);
                    return;
                }

                if (m_State != State.navigatingSubMenuContent)
                {
                    var localZRotationDelta = Mathf.DeltaAngle(m_InitialSpatialLocalRotation.z, actionMapInput.localRotationQuaternion.quaternion.z);//Mathf.Abs(m_InitialSpatialLocalZRotation - currentLocalZRotation);// Mathf.Clamp((m_InitialSpatialLocalZRotation + 1) + currentLocalZRotation, 0f, 2f);
                    if (localZRotationDelta > 0.05f) // Rotating (relatively) leftward
                    {
                        m_HomeSectionDescription.text = m_spatialMenuProviders[1].spatialMenuDescription;
                        m_SectionNameTexts[0].transform.localScale = Vector3.one * 0.5f;
                        m_SectionNameTexts[1].transform.localScale = Vector3.one;
                    }
                    else if (localZRotationDelta < -0.05f)
                    {
                        m_HomeSectionDescription.text = m_spatialMenuProviders[0].spatialMenuDescription;
                        m_SectionNameTexts[1].transform.localScale = Vector3.one * 0.5f;
                        m_SectionNameTexts[0].transform.localScale = Vector3.one;
                    }
                }

                return;
            }

            if (!actionMapInput.show.isHeld && !actionMapInput.select.isHeld)
            {
                visible = false;
                m_State = State.hidden;
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
