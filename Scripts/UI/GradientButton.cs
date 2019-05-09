using System;
using System.Collections;
using Unity.Labs.Utils;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Helpers;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if INCLUDE_TEXT_MESH_PRO
using TMPro;
#endif

[assembly: OptionalDependency("TMPro.TextMeshProUGUI", "INCLUDE_TEXT_MESH_PRO")]

namespace UnityEditor.Experimental.EditorVR.UI
{
    sealed class GradientButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        const string k_MaterialAlphaProperty = "_Alpha";
        const string k_MaterialColorTopProperty = "_ColorTop";
        const string k_MaterialColorBottomProperty = "_ColorBottom";

#pragma warning disable 649
        [SerializeField]
        GradientPair m_NormalGradientPair;

        [SerializeField]
        GradientPair m_HighlightGradientPair;

        // The inner-button's background gradient MeshRenderer
        [SerializeField]
        Renderer m_ButtonMeshRenderer;

        // Transform-root of the contents in the icon container (icons, text, etc)
        [SerializeField]
        Transform m_IconContainer;

        // Transform-root of the contents that will be scaled when button is highlighted
        [SerializeField]
        Transform m_ContentContainer;

        // The canvas group managing the drawing of elements in the icon container
        [SerializeField]
        CanvasGroup m_CanvasGroup;

#if INCLUDE_TEXT_MESH_PRO
        [SerializeField]
        TextMeshProUGUI m_Text;
#endif

        [SerializeField]
        Image m_Icon;

        // Alternate icon sprite, shown when the main icon sprite isn't; If set, this button will swap icon sprites OnClick
        [SerializeField]
        Sprite m_AlternateIconSprite;

        [SerializeField]
        Color m_NormalContentColor;

        [SerializeField]
        Color m_DisabledColor = Color.gray;

        // The color that elements in the HighlightItems collection should inherit during the highlighted state
        [SerializeField]
        Color m_HighlightItemColor = UnityBrandColorScheme.light;

        // Collection of items that will change appearance during the highlighted state (color/position/etc)
        [SerializeField]
        Graphic[] m_HighlightItems;

        [SerializeField]
        bool m_Interactable;

        [SerializeField]
        float m_IconHighlightedLocalZOffset = -0.0015f;

        [SerializeField]
        float m_BeginHighlightDuration = 0.25f;

        [SerializeField]
        float m_EndHighlightDuration = 0.167f;

        [Header("Animated Reveal Settings")]
        [Tooltip("Default value is 0.25")]

        // If AnimatedReveal is enabled, wait this duration before performing the reveal
        [SerializeField]
        [Range(0f, 2f)]
        float m_DelayBeforeReveal = 0.25f;

        [SerializeField]
        float m_HighlightZScaleMultiplier = 2f;

        [SerializeField]
        float m_ContainerContentsAnimationSpeedMultiplier = 1f;
#pragma warning restore 649

        Sprite m_IconSprite;

        bool m_Pressed;
        bool m_Highlighted;
        bool m_Visible;

        Material m_ButtonMaterial;
        Vector3 m_OriginalIconLocalPosition;
        Vector3 m_OriginalContentContainerLocalScale;
        Vector3 m_HighlightContentContainerLocalScale;
        Vector3 m_IconHighlightedLocalPosition;
        Vector3 m_IconPressedLocalPosition;
        Sprite m_OriginalIconSprite;
        Vector3 m_OriginalLocalScale;

        // The initial button reveal coroutines, before highlighting occurs
        Coroutine m_VisibilityCoroutine;
        Coroutine m_ContentVisibilityCoroutine;

        // The visibility & highlight coroutines
        Coroutine m_HighlightCoroutine;
        Coroutine m_IconHighlightCoroutine;

        public event Action click;
        public event Action hoverEnter;
        public event Action hoverExit;

        public Sprite iconSprite
        {
            set
            {
                m_IconSprite = value;
                m_Icon.sprite = m_IconSprite;
            }
        }

        public bool pressed
        {
            get { return m_Pressed; }
            set
            {
                if (value != m_Pressed && value) // proceed only if value is true after previously being false
                {
                    m_Pressed = value;

                    this.StopCoroutine(ref m_IconHighlightCoroutine);

                    m_IconHighlightCoroutine = StartCoroutine(IconContainerContentsBeginHighlight(true));
                }
            }
        }

        public bool highlighted
        {
            get { return m_Highlighted; }
            set
            {
                if (m_Highlighted == value)
                    return;

                // Stop any existing icon highlight coroutines
                this.StopCoroutine(ref m_IconHighlightCoroutine);

                m_Highlighted = value;

                // Stop any existing begin/end highlight coroutine
                this.StopCoroutine(ref m_HighlightCoroutine);

                if (!gameObject.activeInHierarchy)
                    return;

                if (m_Highlighted)
                    this.RestartCoroutine(ref m_HighlightCoroutine, BeginHighlight());
                else
                    this.RestartCoroutine(ref m_HighlightCoroutine, EndHighlight());
            }
        }

        public bool alternateIconVisible
        {
            set
            {
                if (m_AlternateIconSprite) // Only allow sprite swapping if an alternate sprite exists
                    m_Icon.sprite = value ? m_AlternateIconSprite : m_OriginalIconSprite; // If true, set the icon sprite back to the original sprite
            }
            get { return m_Icon.sprite == m_AlternateIconSprite; }
        }

        public bool visible
        {
            get { return m_Visible; }
            set
            {
                if (m_Visible == value)
                    return;

                m_Visible = value;

                if (m_Visible && !gameObject.activeSelf)
                    gameObject.SetActive(true);

                this.StopCoroutine(ref m_VisibilityCoroutine);
                m_VisibilityCoroutine = value ? StartCoroutine(AnimateShow()) : StartCoroutine(AnimateHide());
            }
        }

        public float containerContentsAnimationSpeedMultiplier { set { m_ContainerContentsAnimationSpeedMultiplier = value; } }

        public float iconHighlightedLocalZOffset
        {
            set
            {
                m_IconHighlightedLocalZOffset = value;
                m_IconHighlightedLocalPosition = m_OriginalIconLocalPosition + Vector3.forward * m_IconHighlightedLocalZOffset;
            }
        }

        public GradientPair normalGradientPair
        {
            get { return m_NormalGradientPair; }
            set { m_NormalGradientPair = value; }
        }

        public GradientPair highlightGradientPair
        {
            get { return m_HighlightGradientPair; }
            set { m_HighlightGradientPair = value; }
        }

        public bool interactable
        {
            get { return m_Interactable; }
            set
            {
                if (m_Interactable == value)
                    return;

                m_Interactable = value;
                m_Icon.color = m_Interactable ? m_NormalContentColor : m_DisabledColor;
            }
        }

        void Awake()
        {
            m_OriginalIconSprite = m_Icon.sprite;
            m_ButtonMaterial = MaterialUtils.GetMaterialClone(m_ButtonMeshRenderer);
            m_OriginalLocalScale = transform.localScale;
            m_OriginalIconLocalPosition = m_IconContainer.localPosition;
            m_OriginalContentContainerLocalScale = m_ContentContainer.localScale;
            m_HighlightContentContainerLocalScale = new Vector3(m_OriginalContentContainerLocalScale.x, m_OriginalContentContainerLocalScale.y, m_OriginalContentContainerLocalScale.z * m_HighlightZScaleMultiplier);
            m_IconHighlightedLocalPosition = m_OriginalIconLocalPosition + Vector3.forward * m_IconHighlightedLocalZOffset;
            m_IconPressedLocalPosition = m_OriginalIconLocalPosition + Vector3.back * m_IconHighlightedLocalZOffset;

            m_Icon.color = m_NormalContentColor;
#if INCLUDE_TEXT_MESH_PRO
            m_Text.color = m_NormalContentColor;

            // Clears/resets any non-sprite content(text) from being displayed if a sprite was set on this button
            if (m_OriginalIconSprite)
                SetContent(m_OriginalIconSprite, m_AlternateIconSprite);
            else if (!string.IsNullOrEmpty(m_Text.text))
                SetContent(m_Text.text);
#endif
        }

        void OnDestroy()
        {
            UnityObjectUtils.Destroy(m_ButtonMaterial);
        }

        void OnEnable()
        {
            m_ContentContainer.gameObject.SetActive(true);
        }

        void OnDisable()
        {
            if (!gameObject.activeInHierarchy)
            {
                this.StopCoroutine(ref m_IconHighlightCoroutine);
                this.StopCoroutine(ref m_HighlightCoroutine);
                this.StopCoroutine(ref m_ContentVisibilityCoroutine);
                m_ContentContainer.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Animate the reveal of this button's visual elements
        /// </summary>
        IEnumerator AnimateShow()
        {
            m_CanvasGroup.interactable = false;
            m_ButtonMaterial.SetFloat(k_MaterialAlphaProperty, 0f);
            m_ContentContainer.localScale = m_OriginalContentContainerLocalScale;
            SetMaterialColors(normalGradientPair);

            this.StopCoroutine(ref m_ContentVisibilityCoroutine);
            m_ContentVisibilityCoroutine = StartCoroutine(ShowContent());

            const float kScaleRevealDuration = 0.25f;
            var delay = 0f;
            var scale = Vector3.zero;
            var smoothVelocity = Vector3.zero;
            var currentDuration = 0f;
            var totalDuration = m_DelayBeforeReveal + kScaleRevealDuration;
            var visibleLocalScale = m_OriginalLocalScale;
            while (currentDuration < totalDuration)
            {
                currentDuration += Time.deltaTime;
                transform.localScale = scale;
                m_ButtonMaterial.SetFloat(k_MaterialAlphaProperty, scale.y);

                // Perform initial delay
                while (delay < m_DelayBeforeReveal)
                {
                    delay += Time.deltaTime;
                    yield return null;
                }

                // Perform the button depth reveal
                scale = MathUtilsExt.SmoothDamp(scale, visibleLocalScale, ref smoothVelocity, kScaleRevealDuration, Mathf.Infinity, Time.deltaTime);
                yield return null;
            }

            m_ButtonMaterial.SetFloat(k_MaterialAlphaProperty, 1f);
            transform.localScale = m_OriginalLocalScale;
            m_VisibilityCoroutine = null;
        }

        /// <summary>
        /// Animate the hiding of this button's visual elements
        /// </summary>
        IEnumerator AnimateHide()
        {
            m_CanvasGroup.interactable = false;
            m_ButtonMaterial.SetFloat(k_MaterialAlphaProperty, 0f);

            const float kTotalDuration = 0.25f;
            var scale = transform.localScale;
            var smoothVelocity = Vector3.zero;
            var hiddenLocalScale = Vector3.zero;
            var currentDuration = 0f;
            while (currentDuration < kTotalDuration)
            {
                currentDuration += Time.deltaTime;
                scale = MathUtilsExt.SmoothDamp(scale, hiddenLocalScale, ref smoothVelocity, kTotalDuration, Mathf.Infinity, Time.deltaTime);
                transform.localScale = scale;
                m_ButtonMaterial.SetFloat(k_MaterialAlphaProperty, scale.z);

                yield return null;
            }

            m_ButtonMaterial.SetFloat(k_MaterialAlphaProperty, 0f);
            transform.localScale = hiddenLocalScale;
            m_VisibilityCoroutine = null;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Animate the canvas group's alpha to full opacity
        /// </summary>
        IEnumerator ShowContent()
        {
            m_CanvasGroup.interactable = true;

            const float kTargetAlpha = 1f;
            const float kRevealDuration = 0.4f;
            const float kInitialDelayLengthenMultipler = 5f; // used to scale up the initial delay based on the m_InitialDelay value
            var delay = 0f;
            var targetDelay = Mathf.Clamp(m_DelayBeforeReveal * kInitialDelayLengthenMultipler, 0f, 2.5f); // scale the target delay, with a maximum clamp
            var alpha = 0f;
            var opacitySmoothVelocity = 1f;
            var currentDuration = 0f;
            var targetDuration = targetDelay + kRevealDuration;
            while (currentDuration < targetDuration)
            {
                currentDuration += Time.deltaTime;
                m_CanvasGroup.alpha = alpha;

                while (delay < targetDelay)
                {
                    delay += Time.deltaTime;
                    yield return null;
                }

                alpha = MathUtilsExt.SmoothDamp(alpha, kTargetAlpha, ref opacitySmoothVelocity, targetDuration, Mathf.Infinity, Time.deltaTime);
                yield return null;
            }

            m_CanvasGroup.alpha = 1;
            m_ContentVisibilityCoroutine = null;
        }

        /// <summary>
        /// Performs the animated beginning of a button's highlighted state
        /// </summary>
        IEnumerator BeginHighlight()
        {
            this.StopCoroutine(ref m_IconHighlightCoroutine);
            m_IconHighlightCoroutine = StartCoroutine(IconContainerContentsBeginHighlight());

            var transitionAmount = Time.deltaTime;
            var currentGradientPair = GetMaterialColors();
            var targetGradientPair = highlightGradientPair;
            var currentLocalScale = m_ContentContainer.localScale;
            var highlightedLocalScale = m_HighlightContentContainerLocalScale;
            var highlightDuration = m_BeginHighlightDuration;
            while (transitionAmount < highlightDuration) // Skip while look if user has set the m_BeginHighlightDuration to a value at or below zero
            {
                var shapedTransitionAmount = MathUtilsExt.SmoothInOutLerpFloat(transitionAmount += Time.unscaledDeltaTime / highlightDuration);
                m_ContentContainer.localScale = Vector3.Lerp(currentLocalScale, highlightedLocalScale, shapedTransitionAmount);
                currentGradientPair = GradientPair.Lerp(currentGradientPair, targetGradientPair, shapedTransitionAmount);
                SetMaterialColors(currentGradientPair);
                yield return null;
            }

            SetMaterialColors(targetGradientPair);
            m_ContentContainer.localScale = highlightedLocalScale;
            m_HighlightCoroutine = null;
        }

        /// <summary>
        /// Performs the animated ending of a button's highlighted state
        /// </summary>
        IEnumerator EndHighlight()
        {
            this.StopCoroutine(ref m_IconHighlightCoroutine);
            m_IconHighlightCoroutine = StartCoroutine(IconContainerContentsEndHighlight());

            var transitionAmount = Time.deltaTime;
            var originalGradientPair = GetMaterialColors();
            var targetGradientPair = normalGradientPair;
            var currentLocalScale = m_ContentContainer.localScale;
            var targetScale = m_OriginalContentContainerLocalScale;
            var highlightDuration = m_EndHighlightDuration > 0f ? m_EndHighlightDuration : 0.01f; // Add sane default if highlight duration is zero
            while (transitionAmount < highlightDuration)
            {
                var shapedTransitionAmount = MathUtilsExt.SmoothInOutLerpFloat(transitionAmount += Time.unscaledDeltaTime / highlightDuration);
                var transitioningGradientPair = GradientPair.Lerp(originalGradientPair, targetGradientPair, shapedTransitionAmount);
                SetMaterialColors(transitioningGradientPair);
                m_ContentContainer.localScale = Vector3.Lerp(currentLocalScale, targetScale, shapedTransitionAmount);
                yield return null;
            }

            SetMaterialColors(normalGradientPair);
            m_ContentContainer.localScale = targetScale;
            m_HighlightCoroutine = null;
        }

        /// <summary>
        /// Performs the animated transition of the icon container's visual elements to their highlighted state
        /// </summary>
        /// <param name="pressed">If true, perform pressed-state specific visual changes, as opposed to hover-state specific visuals</param>
        IEnumerator IconContainerContentsBeginHighlight(bool pressed = false)
        {
            var currentPosition = m_IconContainer.localPosition;
            var targetPosition = pressed == false ? m_IconHighlightedLocalPosition : m_IconPressedLocalPosition; // forward for highlight, backward for press
            var transitionAmount = Time.deltaTime;
            var transitionAddMultiplier = !pressed ? 2 : 5; // Faster transition in for highlight; slower for pressed highlight
            while (transitionAmount < 1)
            {
                transitionAmount += Time.unscaledDeltaTime * transitionAddMultiplier * m_ContainerContentsAnimationSpeedMultiplier;

                foreach (var graphic in m_HighlightItems)
                {
                    if (graphic && m_Interactable)
                        graphic.color = Color.Lerp(m_NormalContentColor, m_HighlightItemColor, transitionAmount);
                }

                m_IconContainer.localPosition = Vector3.Lerp(currentPosition, targetPosition, transitionAmount);
                yield return null;
            }

            foreach (var graphic in m_HighlightItems)
            {
                if (graphic && m_Interactable)
                    graphic.color = m_HighlightItemColor;
            }

            m_IconContainer.localPosition = targetPosition;
            m_IconHighlightCoroutine = null;
        }

        /// <summary>
        /// Performs the animated transition of the icon container's visual elements to their non-highlighted state
        /// </summary>
        IEnumerator IconContainerContentsEndHighlight()
        {
            var currentPosition = m_IconContainer.localPosition;
            var transitionAmount = 1f;
            const float kTransitionSubtractMultiplier = 5f;
            while (transitionAmount > 0)
            {
                transitionAmount -= Time.deltaTime * kTransitionSubtractMultiplier;

                foreach (var graphic in m_HighlightItems)
                {
                    if (graphic && m_Interactable)
                        graphic.color = Color.Lerp(m_NormalContentColor, m_HighlightItemColor, transitionAmount);
                }

                m_IconContainer.localPosition = Vector3.Lerp(m_OriginalIconLocalPosition, currentPosition, transitionAmount);
                yield return null;
            }

            foreach (var graphic in m_HighlightItems)
            {
                if (graphic && m_Interactable)
                    graphic.color = m_NormalContentColor;
            }

            m_IconContainer.localPosition = m_OriginalIconLocalPosition;
            m_IconHighlightCoroutine = null;
        }

        /// <summary>
        /// Enable button highlighting on ray enter if autoHighlight is true
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            highlighted = true;

            if (hoverEnter != null)
                hoverEnter();

            eventData.Use();
        }

        /// <summary>
        /// Disable button highlighting on ray exit if autoHighlight is true
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            highlighted = false;

            if (hoverExit != null)
                hoverExit();

            eventData.Use();
        }

        /// <summary>
        /// Raise the OnClick event when this button is clicked
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            SwapIconSprite();

            if (click != null)
                click();
        }

        /// <summary>
        /// Swap between the main and alternate icon-sprites
        /// </summary>
        void SwapIconSprite()
        {
            // Alternate between the main icon and the alternate icon when the button is clicked
            if (m_AlternateIconSprite)
                alternateIconVisible = !alternateIconVisible;
        }

        /// <summary>
        /// Set this button to only display the first character of a given string, instead of an icon-sprite
        /// </summary>
        /// <param name="displayedText">String for which the first character is to be displayed</param>
        public void SetContent(string displayedText)
        {
            m_AlternateIconSprite = null;
            iconSprite = null;
            m_Icon.enabled = false;
#if INCLUDE_TEXT_MESH_PRO
            m_Text.text = displayedText.Substring(0, 2);
#endif
        }

        /// <summary>
        /// Set this button to display a sprite, instead of a text character.
        /// </summary>
        /// <param name="icon">The main icon-sprite to display</param>
        /// <param name="alternateIcon">If set, the alternate icon to display when this button is clicked</param>
        public void SetContent(Sprite icon, Sprite alternateIcon = null)
        {
            m_Icon.enabled = true;
            iconSprite = icon;
            m_AlternateIconSprite = alternateIcon;
#if INCLUDE_TEXT_MESH_PRO
            m_Text.text = string.Empty;
#endif
        }

        GradientPair GetMaterialColors()
        {
            GradientPair gradientPair;
            gradientPair.a = m_ButtonMaterial.GetColor(k_MaterialColorTopProperty);
            gradientPair.b = m_ButtonMaterial.GetColor(k_MaterialColorBottomProperty);
            return gradientPair;
        }

        /// <summary>
        /// Set this button's gradient colors
        /// </summary>
        /// <param name="gradientPair">The gradient pair to set on this button's material</param>
        void SetMaterialColors(GradientPair gradientPair)
        {
            m_ButtonMaterial.SetColor(k_MaterialColorTopProperty, gradientPair.a);
            m_ButtonMaterial.SetColor(k_MaterialColorBottomProperty, gradientPair.b);
        }

        public void UpdateMaterialColors()
        {
            SetMaterialColors(m_Highlighted ? highlightGradientPair : normalGradientPair);
        }
    }
}
