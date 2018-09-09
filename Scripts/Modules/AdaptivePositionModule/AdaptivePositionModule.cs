#if UNITY_EDITOR && UNITY_2017_2_OR_NEWER
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
    public sealed class AdaptivePositionModule : MonoBehaviour, IDetectGazeDivergence, IUsesViewerScale, IControlHaptics
    {
        [SerializeField]
        HapticPulse m_MovingPulse; // The pulse performed while moving an element to a new target position in the user's FOV

        Coroutine m_AdaptiveElementRepositionCoroutine;
        bool m_TestInFocus;
        Transform m_GazeTransform;
        Transform m_WorldspaceAnchorTransform; // The player transform under which anchored objects will be parented

        // Collection objects whose position is controlled by this module
        readonly List<IAdaptPosition> m_AdaptivePositionElements = new List<IAdaptPosition>();

        public class AdaptivePositionData
        {
            public AdaptivePositionData(IAdaptPosition caller, Vector3 previousAnchoredPosition)
            {
                this.previousAnchoredPosition = previousAnchoredPosition;
                startingPosition = caller.adaptiveTransform.position;
            }

            /// <summary>
            /// The world-position at which this object was last anchored
            /// </summary>
            public Vector3 previousAnchoredPosition { get; set; }

            /// <summary>
            /// The origin/starting position of the object being re-positioned
            /// </summary>
            public Vector3 startingPosition { get; set; }
        }

        void Awake()
        {
            m_GazeTransform = CameraUtils.GetMainCamera().transform;
            m_WorldspaceAnchorTransform = m_GazeTransform.parent;
        }

        void Update()
        {
            if (m_AdaptivePositionElements.Count > 0)
            {
                var adaptiveElement = m_AdaptivePositionElements.First();
                if (adaptiveElement.resetAdaptivePosition)
                {
                    this.RestartCoroutine(ref m_AdaptiveElementRepositionCoroutine, RepositionElement(adaptiveElement));
                    return;
                }

                var adaptiveTransform = adaptiveElement.adaptiveTransform;
                var allowedDegreeOfGazeDivergence = adaptiveElement.allowedDegreeOfGazeDivergence;
                var isAboveDivergenceThreshold = this.IsAboveDivergenceThreshold(adaptiveTransform, allowedDegreeOfGazeDivergence);
                adaptiveElement.inFocus = !isAboveDivergenceThreshold; // gaze divergence threshold test regardless of temporal stability

                if (!adaptiveElement.allowAdaptivePositioning)
                    return;

                const float kAllowedDistanceDivergence = 0.5f; // distance beyond which content will be re-positioned at the ideal distance from the user's gaze/hmd
                if (m_AdaptiveElementRepositionCoroutine == null && Mathf.Abs(Vector3.Magnitude(m_GazeTransform.position - adaptiveTransform.position)) > kAllowedDistanceDivergence)
                {
                    var isAboveTemporalDivergenceThreshold = this.IsAboveDivergenceThreshold(adaptiveTransform, allowedDegreeOfGazeDivergence, false);
                    if (isAboveTemporalDivergenceThreshold) // only move if above the gaze divergence threshold with respect to temporal stability
                        this.RestartCoroutine(ref m_AdaptiveElementRepositionCoroutine, RepositionElement(adaptiveElement));
                }
            }
        }

        public void ControlObject(IAdaptPosition adaptiveElement)
        {
            if (m_AdaptivePositionElements.Contains(adaptiveElement))
                return;

            var adaptiveTransform = adaptiveElement.adaptiveTransform;
            adaptiveTransform.localPosition = new Vector3(0f, 0f, adaptiveElement.distanceOffset); // push the object away from the HMD
            adaptiveTransform.parent = m_WorldspaceAnchorTransform;
            adaptiveTransform.rotation = Quaternion.identity;

            m_AdaptivePositionElements.Add(adaptiveElement);
            adaptiveElement.adaptivePositionData = new AdaptivePositionData(adaptiveElement, adaptiveTransform.position);
        }

        public void FreeObject(IAdaptPosition objectToReposition)
        {
            if (m_AdaptivePositionElements.Contains(objectToReposition))
                m_AdaptivePositionElements.Remove(objectToReposition);
        }

        IEnumerator RepositionElement(IAdaptPosition adaptiveElement)
        {
            var adaptiveTransform = adaptiveElement.adaptiveTransform;
            var currentPosition = adaptiveTransform.position;
            var targetPosition = m_GazeTransform.position;
            targetPosition = targetPosition + (this.GetViewerScale() * m_GazeTransform.forward * adaptiveElement.distanceOffset);
            if (!adaptiveElement.resetAdaptivePosition)
            {
                this.Pulse(Node.None, m_MovingPulse);
                adaptiveElement.beingMoved = true;
                var transitionAmount = 0f;
                var transitionSubtractMultiplier = 2f;
                while (transitionAmount < 1f)
                {
                    var smoothTransition = MathUtilsExt.SmoothInOutLerpFloat(transitionAmount);
                    smoothTransition *= smoothTransition;
                    adaptiveTransform.position = Vector3.Lerp(currentPosition, targetPosition, smoothTransition);
                    transitionAmount += Time.deltaTime * transitionSubtractMultiplier;
                    adaptiveTransform.LookAt(m_GazeTransform);
                    yield return null;
                }
            }

            adaptiveTransform.position = targetPosition;
            adaptiveTransform.LookAt(m_GazeTransform);
            m_AdaptiveElementRepositionCoroutine = null;
            adaptiveElement.beingMoved = false;
            adaptiveElement.resetAdaptivePosition = false;
        }
    }
}
#endif
