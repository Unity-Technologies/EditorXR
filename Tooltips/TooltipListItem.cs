#if UNITY_EDITOR
using System.Collections;
using TMPro;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR.Modules
{
    public class TooltipListItem : MonoBehaviour
    {
        [Header("Round Icon")]
        [SerializeField]
        RectTransform m_MiniBackgroundMask;

        [Header("Text")]
        [SerializeField]
        HorizontalLayoutGroup m_TextContainerHorizontalLayoutGroup;

        [SerializeField]
        CanvasGroup m_TextContainerCanvasGroup;

        [SerializeField]
        CanvasGroup m_RightTextCanvasGroup;

        [SerializeField]
        CanvasGroup m_LeftTextCanvasGroup;

        [SerializeField]
        TMP_Text m_TMPText;

        [SerializeField]
        float m_StartDelay;

        [SerializeField]
        float m_IconTextSpacing = 14;

        [SerializeField]
        LayoutElement m_LeftSpacer;

        [SerializeField]
        LayoutElement m_RightSpacer;

        Vector3 m_OriginalMiniBackgroundLocalScale;
        float m_OriginalMinibackgroundOffsetValue;

        float m_OriginalHorizontalLayoutPreferredWidth;
        int m_OriginalRightPaddingAmount;
        int m_OriginalTopPaddingAmount;
        int m_OriginalBottomPaddingAmount;
        int m_OriginalLeftPaddingAmount;
        Coroutine m_AnimateShowTextCoroutine;

        [Header("DEMO ONLY")]
        [SerializeField]
        private RectTransform m_heightDemoTarget;

        // Use this for initialization
        IEnumerator Start()
        {
            yield return null;
            // Cache this, in order to set the inverse of this value to the right padding value
            m_OriginalHorizontalLayoutPreferredWidth = m_TextContainerHorizontalLayoutGroup.preferredWidth;
            m_OriginalMiniBackgroundLocalScale = m_MiniBackgroundMask.localScale;
            m_OriginalMinibackgroundOffsetValue = m_MiniBackgroundMask.offsetMin.x;// - 0.0015f; // Cache only one of the value (min/max), they're all scaled uniformly
            m_TextContainerCanvasGroup.alpha = 0;

            var originalPadding = m_TextContainerHorizontalLayoutGroup.padding;
            m_OriginalTopPaddingAmount = originalPadding.top;
            m_OriginalBottomPaddingAmount = originalPadding.bottom;
            m_OriginalLeftPaddingAmount = 17;// m_TextContainerHorizontalLayoutGroup.padding.left;
            m_OriginalRightPaddingAmount = originalPadding.right;

            m_StartDelay += 0.75f;

            //this.RestartCoroutine(ref m_AnimateShowTextCoroutine, AnimateShowText());
        }

        public void Show(string text)
        {
            m_TMPText.text = text;
            this.RestartCoroutine(ref m_AnimateShowTextCoroutine, AnimateShowText());
        }

        IEnumerator AnimateShowText()
        {
            yield return null; // a frame is needed for proper UI param retrieval
            // set text
            // wait a frame for UI to adjust if needed
            // start anim with horiz layout group right padding inverse of m_originalHorizontalLAyoutPreferredWidth

            Vector3 targetDemoStartPosition = transform.localPosition;
            Vector3 currentDemoStartPosition = transform.localPosition;

            if (m_StartDelay > 0)
            {
                targetDemoStartPosition = m_heightDemoTarget.localPosition;
                transform.localPosition = targetDemoStartPosition;
            }

            var countDown = 2f;
            while (m_StartDelay > 0 && countDown > 0)
            {
                countDown -= Time.unscaledDeltaTime;
                yield return null;
            }

            while (m_StartDelay > 0)
            {
                m_StartDelay -= Time.unscaledDeltaTime;
                var smoothedAmount = Mathf.Pow(MathUtilsExt.SmoothInOutLerpFloat(m_StartDelay), 4);
                transform.localPosition = Vector3.Lerp(currentDemoStartPosition, targetDemoStartPosition, smoothedAmount);
                yield return null;
            }

            const float kTargetAmount = 1.1f; // Overshoot in order to force the lerp to blend to maximum value, with needing to set again after while loop
            var speedScalar = 3f;// isVisible ? k_FadeInSpeedScalar : k_FadeOutSpeedScalar;
            var currentAmount = 0f;
            var currentRightPaddingAmount = m_OriginalRightPaddingAmount; // m_TextContainerHorizontalLayoutGroup.padding.right;
            //var ta
            //var visibilityDefinition = definition.visibilityDefinition;
            //var materialsAndColors = visibilityDefinition.materialsAndAssociatedColors;
            //var shaderColorPropety = visibilityDefinition.colorProperty;
            m_TextContainerCanvasGroup.alpha = 0;

            RectOffset tempPadding = new RectOffset(
                m_OriginalLeftPaddingAmount,
                m_OriginalRightPaddingAmount,
                m_OriginalTopPaddingAmount,
                m_OriginalBottomPaddingAmount);

            m_TextContainerHorizontalLayoutGroup.padding = tempPadding;

            var textInfo = m_TMPText.textInfo;
            var visibleCharacterCount = textInfo.characterCount;
            m_TMPText.maxVisibleCharacters = visibleCharacterCount;
            yield return null;

            var startingAmount = -m_TextContainerHorizontalLayoutGroup.preferredWidth;

            Debug.LogError(startingAmount);

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
            m_TextContainerCanvasGroup.alpha = 1;
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
                m_TMPText.maxVisibleCharacters = (int) (smoothedAmount * visibleCharacterCount);

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
        }

        IEnumerator AnimateHideText()
        {
            // set text
            // wait a frame for UI to adjust if needed
            // start anim with horiz layout group right padding inverse of m_originalHorizontalLAyoutPreferredWidth

            const float kTargetAmount = 0f; // Overshoot in order to force the lerp to blend to maximum value, with needing to set again after while loop
            var speedScalar = 3f;// isVisible ? k_FadeInSpeedScalar : k_FadeOutSpeedScalar;
            var currentAmount = 1f;
            var currentRightPaddingAmount = m_OriginalRightPaddingAmount; // m_TextContainerHorizontalLayoutGroup.padding.right;
            //var ta
            //var visibilityDefinition = definition.visibilityDefinition;
            //var materialsAndColors = visibilityDefinition.materialsAndAssociatedColors;
            //var shaderColorPropety = visibilityDefinition.colorProperty;
            m_TextContainerCanvasGroup.alpha = 1f;

            yield return null;
            var startingAmount = -m_TextContainerHorizontalLayoutGroup.preferredWidth;

            var textInfo = m_TMPText.textInfo;
            var visibleCharacterCount = textInfo.characterCount;

            m_TextContainerCanvasGroup.alpha = 1;
            while (currentAmount > kTargetAmount)
            {
                var smoothedAmount = Mathf.Pow(MathUtilsExt.SmoothInOutLerpFloat(currentAmount -= Time.unscaledDeltaTime * speedScalar), 2);
                RectOffset tempPadding = new RectOffset(
                    m_OriginalLeftPaddingAmount,
                    (int)Mathf.Lerp(startingAmount, m_OriginalRightPaddingAmount, smoothedAmount),
                    m_OriginalTopPaddingAmount,
                    m_OriginalBottomPaddingAmount);

                smoothedAmount = Mathf.Pow(smoothedAmount, 4);
                m_TextContainerHorizontalLayoutGroup.padding = tempPadding;
                m_RightTextCanvasGroup.alpha = smoothedAmount;
                m_TMPText.maxVisibleCharacters = (int)(smoothedAmount * visibleCharacterCount);

                yield return null;
            }

            m_TMPText.maxVisibleCharacters = visibleCharacterCount;

            currentAmount = 1f;
            var slightlySmallerOffsetTarget = m_OriginalMinibackgroundOffsetValue - 0.0035f;
            speedScalar = 4.5f;
            while (currentAmount > kTargetAmount)
            {
                var smoothedAmount = Mathf.Pow(MathUtilsExt.SmoothInOutLerpFloat(currentAmount -= Time.unscaledDeltaTime * speedScalar), 4);
                smoothedAmount = Mathf.Lerp(smoothedAmount, currentAmount, smoothedAmount);
                //m_MiniBackgroundMask.localScale = Vector3.Lerp(m_OriginalMiniBackgroundLocalScale, m_OriginalMiniBackgroundLocalScale * 3, smoothedAmount);
                //smoothedAmount *= m_OriginalMinibackgroundOffsetValue;

                var outlineExpandAmount = Mathf.Lerp(slightlySmallerOffsetTarget, m_OriginalMinibackgroundOffsetValue * 30f, smoothedAmount);
                m_MiniBackgroundMask.offsetMin = new Vector2(-outlineExpandAmount, -outlineExpandAmount);
                m_MiniBackgroundMask.offsetMax = new Vector2(outlineExpandAmount, outlineExpandAmount);

                yield return null;
            }

            m_MiniBackgroundMask.offsetMin = new Vector2(-slightlySmallerOffsetTarget, -slightlySmallerOffsetTarget);
            m_MiniBackgroundMask.offsetMax = new Vector2(slightlySmallerOffsetTarget, slightlySmallerOffsetTarget);

            // Wait before restarting
            while (currentAmount < 2f)
            {
                currentAmount += Time.unscaledDeltaTime;
                yield return null;
            }

            this.RestartCoroutine(ref m_AnimateShowTextCoroutine, AnimateShowText());
        }
    }
}
#endif
