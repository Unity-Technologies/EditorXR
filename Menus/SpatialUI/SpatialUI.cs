#if UNITY_EDITOR
using System.Collections;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Playables;

namespace UnityEditor.Experimental.EditorVR
{
    public class SpatialUI : MonoBehaviour, IAdaptPosition
    {
        const float k_DistanceOffset = 0.5f;

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
        Coroutine m_VisibilityCoroutine;
        Vector3 m_HomeTextBackgroundOriginalLocalScale;

        public Transform adaptiveTransform { get { return transform; } }
        public float allowedGazeDivergence { get; private set; }
        public float m_DistanceOffset { get { return k_DistanceOffset; } }
        public AdaptivePositionModule.AdaptivePositionData adaptivePositionData { get; set; }

        public bool beingMoved
        {
            get { return m_BeingMoved; }
            set
            {
                m_BeingMoved = value;
                this.RestartCoroutine(ref m_VisibilityCoroutine, AnimateVisibility(!m_BeingMoved));

                //m_Director.Play(m_RevealPlayable);
            }
        }

        void Awake()
        {
            m_HomeTextBackgroundOriginalLocalScale = m_HomeTextBackgroundTransform.localScale;
        }

        IEnumerator AnimateVisibility(bool show = true)
        {
            var speedScalar = show ? 3f : 2f;
            var currentAlpha = m_MainCanvasGroup.alpha;
            var targetMainCanvasAlpha = show ? 1f : 0.25f;

            var currentHomeTextAlpha = m_HomeTextCanvasGroup.alpha;
            var targetHomeTextAlpha = show ? 1f : 0f;

            var currentHomeBackgroundLocalScale = m_HomeTextBackgroundTransform.localScale;
            var targetHomeBackgroundLocalScale = show ? m_HomeTextBackgroundOriginalLocalScale : new Vector3(m_HomeTextBackgroundOriginalLocalScale.x, 0f, 1f);
            //var targetPosition = show ? (moveToAlternatePosition ? m_AlternateLocalPosition : m_OriginalLocalPosition) : Vector3.zero;
            //var targetScale = show ? (moveToAlternatePosition ? m_OriginalLocalScale : m_OriginalLocalScale * k_AlternateLocalScaleMultiplier) : Vector3.zero;
            //var currentPosition = transform.localPosition;
            //var currentIconScale = m_IconContainer.localScale;
            //var targetIconContainerScale = show ? m_OriginalIconContainerLocalScale : Vector3.zero;
            var transitionAmount = 0f;
            //var currentScale = transform.localScale;
            while (transitionAmount < 1)
            {
                var shapedAmount = MathUtilsExt.SmoothInOutLerpFloat(transitionAmount += Time.unscaledDeltaTime * speedScalar);
                //m_Director.time = shapedAmount;
                //m_IconContainer.localScale = Vector3.Lerp(currentIconScale, targetIconContainerScale, shapedAmount);
                //transform.localPosition = Vector3.Lerp(currentPosition, targetPosition, shapedAmount);
                //transform.localScale = Vector3.Lerp(currentScale, targetScale, shapedAmount);
                //m_MainCanvasGroup.alpha = Mathf.Lerp(currentAlpha, targetMainCanvasAlpha, shapedAmount);

                shapedAmount *= shapedAmount; // increase beginning & end anim emphasis
                m_HomeTextCanvasGroup.alpha = Mathf.Lerp(currentHomeTextAlpha, targetHomeTextAlpha, shapedAmount);

                m_HomeTextBackgroundTransform.localScale = Vector3.Lerp(currentHomeBackgroundLocalScale, targetHomeBackgroundLocalScale, shapedAmount);
                yield return null;
            }

            //m_IconContainer.localScale = targetIconContainerScale;
            //transform.localScale = targetScale;
            //transform.localPosition = targetPosition;

            //m_MainCanvasGroup.alpha = targetMainCanvasAlpha;

            m_VisibilityCoroutine = null;
        }
    }
}
#endif
