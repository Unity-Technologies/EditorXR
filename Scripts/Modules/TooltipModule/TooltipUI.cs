#if UNITY_EDITOR
using System.Collections;
using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace UnityEditor.Experimental.EditorVR.Modules
{
    sealed class TooltipUI : MonoBehaviour, IWillRender
    {
        const float k_IconTextMinSpacing = 4;

        [SerializeField]
        RawImage m_DottedLine;

        [SerializeField]
        Transform[] m_Spheres;

        [SerializeField]
        Image m_Background;

        [SerializeField]
        TMP_Text m_TextLeft;

        [SerializeField]
        TMP_Text m_TextRight;

        [SerializeField]
        CanvasGroup m_LeftTextCanvasGroup;

        [SerializeField]
        CanvasGroup m_RightTextCanvasGroup;

        [SerializeField]
        float m_IconTextSpacing = 14;

        [SerializeField]
        LayoutElement m_LeftSpacer;

        [SerializeField]
        LayoutElement m_RightSpacer;

        [SerializeField] private string m_DEMOTEXT;
        [SerializeField] private TextAlignment m_DEMOTEXTALIGNMENT;

        int m_OriginalRightPaddingAmount;
        int m_OriginalTopPaddingAmount;
        int m_OriginalBottomPaddingAmount;
        int m_OriginalLeftPaddingAmount;

        TextAlignment m_Alignment;

        Coroutine m_AnimateShowLeftSideTextCoroutine;
        Coroutine m_AnimateShowRightSideTextCoroutine;

        public RawImage dottedLine { get { return m_DottedLine; } }
        public Transform[] spheres { get { return m_Spheres; } }
        public Image background { get { return m_Background; } }
        public event Action becameVisible;

        public RectTransform rectTransform
        {
            get { return m_Background.rectTransform; }
        }

        void Start()
        {
            Show(m_DEMOTEXT, m_DEMOTEXTALIGNMENT);
        }

        public void Show(string text, TextAlignment alignment, Sprite icon = null)
        {
            //m_TMPText.text = text;
            //this.RestartCoroutine(ref m_AnimateShowTextCoroutine, AnimateShowText());

            // if Icon null, fade out opacity of cuttent icon
            // if icon is not null, fade out current, fade in new icon
            switch (alignment)
            {
                case TextAlignment.Center:
                case TextAlignment.Left:
                    // Treat center as left justified, aside from horizontal offset placement
                    m_TextRight.text = text;
                    m_TextRight.gameObject.SetActive(true);
                    m_TextLeft.gameObject.SetActive(false);
                    m_RightSpacer.minWidth = m_IconTextSpacing;
                    m_LeftSpacer.minWidth = k_IconTextMinSpacing;
                    break;
                case TextAlignment.Right:
                    m_TextLeft.text = text;
                    m_TextRight.gameObject.SetActive(false);
                    m_TextLeft.gameObject.SetActive(true);
                    m_RightSpacer.minWidth = k_IconTextMinSpacing;
                    m_LeftSpacer.minWidth = m_IconTextSpacing;
                    break;
            }
        }

        IEnumerator AnimateShowText(TMP_Text text, CanvasGroup textCanvasGroup)
        {
            yield return null; // a frame is needed for proper UI param retrieval
            // set text
            // wait a frame for UI to adjust if needed
            // start anim with horiz layout group right padding inverse of m_originalHorizontalLAyoutPreferredWidth

            Vector3 targetDemoStartPosition = transform.localPosition;
            Vector3 currentDemoStartPosition = transform.localPosition;

            /*
            const float kTargetAmount = 1.1f; // Overshoot in order to force the lerp to blend to maximum value, with needing to set again after while loop
            var speedScalar = 3f;// isVisible ? k_FadeInSpeedScalar : k_FadeOutSpeedScalar;
            var currentAmount = 0f;
            var currentRightPaddingAmount = m_OriginalRightPaddingAmount;

            //var visibilityDefinition = definition.visibilityDefinition;
            //var materialsAndColors = visibilityDefinition.materialsAndAssociatedColors;
            //var shaderColorPropety = visibilityDefinition.colorProperty;
            textCanvasGroup.alpha = 0;

            RectOffset tempPadding = new RectOffset
            (
                m_OriginalLeftPaddingAmount,
                m_OriginalRightPaddingAmount,
                m_OriginalTopPaddingAmount,
                m_OriginalBottomPaddingAmount
            );

            m_TextContainerHorizontalLayoutGroup.padding = tempPadding;

            var textInfo = text.textInfo;
            var visibleCharacterCount = textInfo.characterCount;
            text.maxVisibleCharacters = visibleCharacterCount;
            yield return null;

            var startingAmount = -m_TextContainerHorizontalLayoutGroup.preferredWidth;
            while (currentAmount < kTargetAmount)
            {
                var smoothedAmount = Mathf.Pow(MathUtilsExt.SmoothInOutLerpFloat(currentAmount += Time.unscaledDeltaTime * speedScalar), 4);
                //m_MiniBackgroundMask.localScale = Vector3.Lerp(m_OriginalMiniBackgroundLocalScale, m_OriginalMiniBackgroundLocalScale * 3, smoothedAmount);
                //smoothedAmount *= m_OriginalMinibackgroundOffsetValue;

                //transform.localPosition = Vector3.Lerp(targetDemoStartPosition, currentDemoStartPosition, smoothedAmount);

                var outlineExpandAmount = Mathf.Lerp(m_OriginalMinibackgroundOffsetValue, m_OriginalMinibackgroundOffsetValue * 30f, smoothedAmount);
                m_MiniBackgroundMask.offsetMin = new Vector2(-outlineExpandAmount, -outlineExpandAmount);
                m_MiniBackgroundMask.offsetMax = new Vector2(outlineExpandAmount, outlineExpandAmount);

                yield return null;
            }

            transform.localPosition = currentDemoStartPosition;

            currentAmount = 0f;
            textCanvasGroup.alpha = 1;
            speedScalar = 2f;
            while (currentAmount < kTargetAmount)
            {
                var smoothedAmount = Mathf.Pow(MathUtilsExt.SmoothInOutLerpFloat(currentAmount += Time.unscaledDeltaTime * speedScalar), 2);
                smoothedAmount = Mathf.Lerp(smoothedAmount, currentAmount, smoothedAmount);
                tempPadding = new RectOffset(
                    m_OriginalLeftPaddingAmount,
                    (int)Mathf.Lerp(startingAmount, m_OriginalRightPaddingAmount, smoothedAmount),
                    m_OriginalTopPaddingAmount,
                    m_OriginalBottomPaddingAmount);

                smoothedAmount = Mathf.Pow(smoothedAmount, 4);
                m_TextContainerHorizontalLayoutGroup.padding = tempPadding;
                m_RightTextCanvasGroup.alpha = smoothedAmount;
                text.maxVisibleCharacters = (int)(smoothedAmount * visibleCharacterCount);

                yield return null;
            }

            // Wait before restarting
            currentAmount = 0f;
            while (currentAmount < kTargetAmount)
            {
                currentAmount += Time.unscaledDeltaTime;
                yield return null;
            }

            this.RestartCoroutine(ref m_AnimateShowTextCoroutine, AnimateHideText());
            */
        }

        public void OnBecameVisible()
        {
            if (becameVisible != null)
                becameVisible();
        }

        public void OnBecameInvisible()
        {
        }
    }
}
#endif
