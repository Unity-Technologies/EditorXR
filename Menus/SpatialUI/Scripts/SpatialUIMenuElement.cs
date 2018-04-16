#if UNITY_EDITOR
using System;
using System.Collections;
using TMPro;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR
{
    public class SpatialUIMenuElement : MonoBehaviour
    {
        [SerializeField]
        TextMeshProUGUI m_Text;

        [SerializeField]
        Image m_Icon;

        [SerializeField]
        CanvasGroup m_CanvasGroup;

        [SerializeField]
        float m_transitionDuration = 1f;

        Transform m_Transform;
        Action m_SelectedAction;
        Coroutine m_VisibilityCoroutine;

        public Transform transform { get { return m_Transform; } }
        public Action selectedAction { get { return m_SelectedAction; } }

        public void Setup(Transform transform, Transform parentTransform, Action selectedAction, String displayedText = null, Sprite sprite = null)
        {
            if (selectedAction == null)
            {
                Debug.LogWarning("Cannot setup SpatialUIMenuElement without an assigned action.");
                ObjectUtils.Destroy(gameObject);
                return;
            }

            m_SelectedAction = selectedAction;
            m_Transform = transform;

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

            if (Mathf.Approximately(m_transitionDuration, 0f))
                m_transitionDuration = 0.001f;
        }

        void OnEnable()
        {
            if (m_CanvasGroup != null)
                this.RestartCoroutine(ref m_VisibilityCoroutine, AnimateVisibility(true));
        }

        void OnDisable()
        {
            StopAllCoroutines();
        }

        // TODO perform animated reveal of content after setup
        public IEnumerator AnimateVisibility(bool fadeIn)
        {
            Debug.Log("Performing AnimateShow for SpatialUIMenuElement : " + m_Text.text);
            var currentAlpha = fadeIn ? 0f : m_CanvasGroup.alpha;
            var targetAlpha = fadeIn ? 1f : 0f;
            var transitionAmount = 0f;
            var transitionSubtractMultiplier = 1f / m_transitionDuration;
            while (transitionAmount < 1f)
            {
                var smoothTransition = MathUtilsExt.SmoothInOutLerpFloat(transitionAmount);
                m_CanvasGroup.alpha = Mathf.Lerp(currentAlpha, targetAlpha, smoothTransition);
                transitionAmount += Time.deltaTime * transitionSubtractMultiplier;
                yield return null;
            }

            m_CanvasGroup.alpha = targetAlpha;
            m_VisibilityCoroutine = null;
        }
    }
}
#endif
