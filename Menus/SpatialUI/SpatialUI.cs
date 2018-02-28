#if UNITY_EDITOR
using System;
using System.Collections;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.Playables;

namespace UnityEditor.Experimental.EditorVR
{
    public class SpatialUI : MonoBehaviour, IAdaptPosition
    {
        // TODO expose as a user preference, for spatial UI distance
        const float k_DistanceOffset = 0.75f;
        const float k_AllowedGazeDivergence = 45f;

        [SerializeField]
        CanvasGroup m_MainCanvasGroup;

        [SerializeField]
        CanvasGroup m_HomeTextCanvasGroup;

        [SerializeField]
        Transform m_HomeTextBackgroundTransform;

        //[SerializeField]
        //PlayableDirector m_Director;

        [SerializeField]
        PlayableAsset m_RevealPlayable;

        bool m_BeingMoved;
        bool m_InFocus;
        Vector3 m_HomeTextBackgroundOriginalLocalScale;

        Coroutine m_VisibilityCoroutine;
        Coroutine m_InFocusCoroutine;

        public Transform adaptiveTransform { get { return transform; } }
        public float allowedDegreeOfGazeDivergence { get { return k_AllowedGazeDivergence; } }
        public float distanceOffset { get { return k_DistanceOffset; } }
        public AdaptivePositionModule.AdaptivePositionData adaptivePositionData { get; set; }

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

                //m_Director.Play(m_RevealPlayable);
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
        }

        IEnumerator AnimateVisibility()
        {
            var speedScalar = m_BeingMoved ? 2f : 4f;
            var currentAlpha = m_MainCanvasGroup.alpha;
            var targetMainCanvasAlpha = m_BeingMoved ? 0.25f : 1f;

            var currentHomeTextAlpha = m_HomeTextCanvasGroup.alpha;
            var targetHomeTextAlpha = m_BeingMoved ? 0f : 1f;

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

                shapedAmount *= shapedAmount; // increase beginning & end anim emphasis
                m_HomeTextCanvasGroup.alpha = Mathf.Lerp(currentHomeTextAlpha, targetHomeTextAlpha, shapedAmount);

                //m_HomeTextBackgroundTransform.localScale = Vector3.Lerp(currentHomeBackgroundLocalScale, targetHomeBackgroundLocalScale, shapedAmount);
                yield return null;
            }

            //m_IconContainer.localScale = targetIconContainerScale;
            //transform.localScale = targetScale;
            //transform.localPosition = targetPosition;

            //m_MainCanvasGroup.alpha = targetMainCanvasAlpha;

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
    }
}
#endif
