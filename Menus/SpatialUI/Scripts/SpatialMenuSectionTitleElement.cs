#if UNITY_EDITOR
using System;
using System.Collections;
using TMPro;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR
{
    internal class SpatialMenuSectionTitleElement : MonoBehaviour, ISpatialMenuElement, IControlHaptics, IRayEnterHandler, IRayExitHandler,
        IRayClickHandler, IPointerClickHandler
    {
        [SerializeField]
        TextMeshProUGUI m_Text;

        [SerializeField]
        Image m_Icon;

        [SerializeField]
        CanvasGroup m_CanvasGroup;

        [SerializeField]
        Button m_Button;

        [SerializeField]
        float m_TransitionDuration = 0.75f;

        [SerializeField]
        float m_FadeInZOffset = 0.05f;

        [SerializeField]
        float m_HighlightedZOffset = -0.005f;

        //[SerializeField]
        //Image m_BackgroundImage;

        [Header("Haptic Pulses")]
        [SerializeField]
        HapticPulse m_HighlightPulse;

        [SerializeField]
        HapticPulse m_TooltipDisplayPulse;

        RectTransform m_RectTransform;
        Action m_SelectedAction;
        Vector2 m_OriginalSize;
        Vector2 m_ExpandedTooltipDisplaySize;
        Coroutine m_VisibilityCoroutine;
        Coroutine m_TooltipVisualsVisibilityCoroutine;
        Vector3 m_TextOriginalLocalPosition;
        bool m_Highlighted;
        Vector3 m_OriginalBordersLocalScale;
        float m_BordersOriginalAlpha;
        bool m_Visible;

        public Transform elementTransform { get { return transform; } }
        public Action selectedAction { get { return m_SelectedAction; } }
        public Button button { get { return m_Button; } }

        public bool visible
        {
            get { return m_Visible; }
            set
            {
                if (m_Visible == value)
                    return;

                if (m_CanvasGroup != null)
                    this.RestartCoroutine(ref m_VisibilityCoroutine, AnimateVisibility(m_Visible));
            }
        }

        public bool highlighted
        {
            set
            {
                if (m_Highlighted == value)
                    return;

                m_Highlighted = value;
                parentMenuData.highlighted = value;

                this.RestartCoroutine(ref m_VisibilityCoroutine, AnimateHighlight(m_Highlighted));

                //Debug.LogWarning("<color=orange>Highlighting top level menu button : </color>" + m_Text.text);
                if (m_Highlighted)
                    this.Pulse(Node.None, m_HighlightPulse);
            }
        }

        public Action<Transform, Action, string, string> Setup { get; set; }
        public Action selected { get; set; }
        public SpatialMenu.SpatialMenuData parentMenuData { get; set; }
        public Action correspondingFunction { get; set; }

        void Awake()
        {
            Setup = SetupInternal;
            m_Button.onClick.AddListener(Select);
        }

        void OnDestroy()
        {
            m_Button.onClick.RemoveAllListeners();
        }

        void Select()
        {
            Debug.LogError("Selected called in spatial menu sectio title element :" + m_Text.text);
            if (selected != null)
                selected();
        }

        public void SetupInternal(Transform parentTransform, Action selectedAction, String displayedText = null, string toolTipText = null)
        {
            if (selectedAction == null)
            {
                Debug.LogWarning("Cannot setup SpatialUIMenuElement without an assigned action.");
                ObjectUtils.Destroy(gameObject);
                return;
            }

            m_SelectedAction = selectedAction;
            m_RectTransform = (RectTransform)transform;
            m_OriginalSize = m_RectTransform.sizeDelta;

            Sprite sprite = null;
            if (sprite != null) // Displaying a sprite icon instead of text
            {
                m_Icon.gameObject.SetActive(true);
                m_Text.gameObject.SetActive(false);
                m_Icon.sprite = sprite;
            }
            else // Displaying text instead of a sprite icon
            {
                m_Icon.gameObject.SetActive(false);
                m_Text.gameObject.SetActive(true);
                m_Text.text = displayedText;
            }

            transform.SetParent(parentTransform);
            transform.localRotation = Quaternion.identity;
            transform.localPosition = Vector3.zero;
            transform.localScale = Vector3.one;

            if (Mathf.Approximately(m_TransitionDuration, 0f))
                m_TransitionDuration = 0.001f;
        }

        void OnEnable()
        {
            // Cacheing position here, as layout groups were altering the position when originally cacheing in Start()
            m_TextOriginalLocalPosition = m_Text.transform.localPosition;
        }

        void OnDisable()
        {
            StopAllCoroutines();
        }

        public void OnRayEnter(RayEventData eventData)
        {
            highlighted = true;
        }

        public void OnRayExit(RayEventData eventData)
        {
            highlighted = false;
        }

        IEnumerator AnimateVisibility(bool fadeIn)
        {
            var currentAlpha = fadeIn ? 0f : m_CanvasGroup.alpha;
            var targetAlpha = fadeIn ? 1f : 0f;
            var alphaTransitionAmount = 0f;
            var textTransform = m_Text.transform;
            var textCurrentLocalPosition = textTransform.localPosition;
            textCurrentLocalPosition = fadeIn ? new Vector3(m_TextOriginalLocalPosition.x, m_TextOriginalLocalPosition.y, m_FadeInZOffset) : textCurrentLocalPosition;
            var textTargetLocalPosition = m_TextOriginalLocalPosition;
            var positionTransitionAmount = 0f;
            var transitionSubtractMultiplier = 1f / m_TransitionDuration;
            while (alphaTransitionAmount < 1f)
            {
                var alphaSmoothTransition = MathUtilsExt.SmoothInOutLerpFloat(alphaTransitionAmount);
                var positionSmoothTransition = MathUtilsExt.SmoothInOutLerpFloat(positionTransitionAmount);
                m_CanvasGroup.alpha = Mathf.Lerp(currentAlpha, targetAlpha, alphaSmoothTransition);
                textTransform.localPosition = Vector3.Lerp(textCurrentLocalPosition, textTargetLocalPosition, positionSmoothTransition);
                alphaTransitionAmount += Time.deltaTime * transitionSubtractMultiplier;
                positionTransitionAmount += alphaTransitionAmount * 1.35f;
                yield return null;
            }

            textTransform.localPosition = textTargetLocalPosition;
            m_CanvasGroup.alpha = targetAlpha;
            m_VisibilityCoroutine = null;
        }

        IEnumerator AnimateHighlight(bool isHighlighted)
        {
            var currentAlpha = m_CanvasGroup.alpha;
            var targetAlpha = 1f;
            var alphaTransitionAmount = 0f;
            var textTransform = m_Text.transform;
            var textCurrentLocalPosition = textTransform.localPosition;
            var textTargetLocalPosition = isHighlighted ? new Vector3(m_TextOriginalLocalPosition.x, m_TextOriginalLocalPosition.y, m_HighlightedZOffset) : m_TextOriginalLocalPosition;
            var positionTransitionAmount = 0f;
            var currentTextLocalScale = textTransform.localScale;
            var targetTextLocalScale = isHighlighted ? Vector3.one * 1.15f : Vector3.one;
            //var currentBackgroundColor = m_BackgroundImage.color;
            var targetBackgroundColor = isHighlighted ? Color.black : Color.clear;
            var speedMultiplier = isHighlighted ? 3f : 6f;
            while (alphaTransitionAmount < 1f)
            {
                var alphaSmoothTransition = MathUtilsExt.SmoothInOutLerpFloat(alphaTransitionAmount);
                var positionSmoothTransition = MathUtilsExt.SmoothInOutLerpFloat(positionTransitionAmount);
                m_CanvasGroup.alpha = Mathf.Lerp(currentAlpha, targetAlpha, alphaSmoothTransition);
                textTransform.localPosition = Vector3.Lerp(textCurrentLocalPosition, textTargetLocalPosition, positionSmoothTransition);
                textTransform.localScale = Vector3.Lerp(currentTextLocalScale, targetTextLocalScale, alphaSmoothTransition);
                alphaTransitionAmount += Time.deltaTime * speedMultiplier;
                positionTransitionAmount += alphaTransitionAmount * 1.35f;
                //m_BackgroundImage.color = Color.Lerp(currentBackgroundColor, targetBackgroundColor, alphaSmoothTransition * 4);
                yield return null;
            }

            textTransform.localPosition = textTargetLocalPosition;
            textTransform.localScale = targetTextLocalScale;
            //m_BackgroundImage.color = targetBackgroundColor;
            m_CanvasGroup.alpha = targetAlpha;
            m_VisibilityCoroutine = null;
        }

        public void OnRayClick(RayEventData eventData)
        {
            Debug.LogError("OnRayClick called for spatial menu section title element :" + m_Text.text);
         }

        public void OnPointerClick(PointerEventData eventData)
        {
            Debug.LogError("OnPointerClick called for spatial menu section title element :" + m_Text.text);
        }
    }
}
#endif
