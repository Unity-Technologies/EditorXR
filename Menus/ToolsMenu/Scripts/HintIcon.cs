using System.Collections;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace UnityEditor.Experimental.EditorVR.Menus
{
    public class HintIcon : MonoBehaviour
    {
        [SerializeField]
        bool m_HideOnInitialize = true;

        [SerializeField]
        Image m_Icon;

        [SerializeField]
        Color m_VisibleColor = Color.white;

        [SerializeField]
        Color m_HiddenColor = Color.clear;

        [SerializeField]
        Color m_PulseColor = Color.white;

        [SerializeField]
        float m_ShowDuration = 0.125f;

        [SerializeField]
        float m_HideDuration = 0.25f;

        [SerializeField]
        bool m_SlightlyRandomizeHideDuration = true;

        readonly Vector3 k_HiddenScale = Vector3.zero;

        Transform m_IconTransform;
        Vector3 m_VisibleLocalScale;
        Coroutine m_VisibilityCoroutine;
        Coroutine m_ScrollArrowPulseCoroutine;
        float m_PulseDuration;

        /// <summary>
        /// Bool denoting the visibility state of this icon
        /// </summary>
        public bool visible { set { this.RestartCoroutine(ref m_VisibilityCoroutine, AnimateVisibility(value)); } }

        /// <summary>
        /// The color to be displayed by this icon when it is visible
        /// </summary>
        public Color visibleColor
        {
            set
            {
                m_VisibleColor = value;
                visible = true;
            }
        }

        void Awake()
        {
            m_IconTransform = m_Icon.transform;
            m_VisibleLocalScale = m_IconTransform.localScale * 1.25F;
            m_Icon.color = m_VisibleColor;

            if (m_HideOnInitialize)
                visible = false;
        }

        IEnumerator AnimateVisibility(bool show = true)
        {
            var currentDuration = 0f;
            float targetDuration;
            var currentLocalScale = m_IconTransform.localScale;
            var targetLocalScale = show ? m_VisibleLocalScale : k_HiddenScale;

            // Only perform this wait if showing/revealing, not hiding
            if (show && currentLocalScale == k_HiddenScale)
            {
                // Only perform delay if fully hidden; otherwise resume showing
                targetDuration = Random.Range(0.125f, 0.175f); // Set an initial random wait duration
                while (currentDuration < targetDuration)
                {
                    currentDuration += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            currentDuration = 0f;
            targetDuration = show ? m_ShowDuration : m_HideDuration + (m_SlightlyRandomizeHideDuration ? 0f : Random.Range(0.125f, 0.2f)); // Set an initial random wait duration
            const int kAdditionalDurationShaping = 4;
            const int kAdditionalHideSpeedScalar = 3;
            var currentColor = m_Icon.color;
            var targetColor = show ? m_VisibleColor : m_HiddenColor;
            while (currentDuration < targetDuration)
            {
                var shapedDuration = MathUtilsExt.SmoothInOutLerpFloat(currentDuration / targetDuration);
                shapedDuration = Mathf.Pow(shapedDuration, kAdditionalDurationShaping);
                var colorLerpAmount = show ? shapedDuration : currentDuration * kAdditionalHideSpeedScalar;
                m_IconTransform.localScale = Vector3.Lerp(currentLocalScale, targetLocalScale, shapedDuration);
                m_Icon.color = Color.Lerp(currentColor, targetColor, colorLerpAmount);
                currentDuration += Time.unscaledDeltaTime;
                yield return null;
            }

            m_IconTransform.localScale = targetLocalScale;
        }

        /// <summary>
        /// Perform a colored visual pulse
        /// </summary>
        public void PulseColor()
        {
            if (Mathf.Approximately(m_PulseDuration, 0f) || m_PulseDuration > 0.85f)
                this.RestartCoroutine(ref m_ScrollArrowPulseCoroutine, AnimatePulseColor());
        }

        IEnumerator AnimatePulseColor()
        {
            const float kTargetDuration = 1f;
            m_PulseDuration = 0f;
            var currentColor = m_Icon.color;
            while (m_PulseDuration < kTargetDuration)
            {
                var shapedDuration = MathUtilsExt.SmoothInOutLerpFloat(m_PulseDuration / kTargetDuration);
                m_Icon.color = Color.Lerp(currentColor, m_PulseColor, shapedDuration);
                m_PulseDuration += Time.unscaledDeltaTime * 5;
                yield return null;
            }

            while (m_PulseDuration > 0f)
            {
                var shapedDuration = MathUtilsExt.SmoothInOutLerpFloat(m_PulseDuration / kTargetDuration);
                m_Icon.color = Color.Lerp(m_VisibleColor, m_PulseColor, shapedDuration);
                m_PulseDuration -= Time.unscaledDeltaTime * 2;
                yield return null;
            }

            m_Icon.color = m_VisibleColor;
            m_PulseDuration = 0f;
        }
    }
}
