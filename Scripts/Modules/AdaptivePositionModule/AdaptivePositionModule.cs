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

        bool m_TestInFocus;
        Transform m_GazeTransform;
        Transform m_WorldspaceAnchorTransform; // The player transform under which anchored objects will be parented

        // Collection of objects whose position is controlled by this module
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
                foreach (var element in m_AdaptivePositionElements)
                {
                    var repositionCoroutine = element.adaptiveElementRepositionCoroutine;
                    if (element.resetAdaptivePosition)
                    {
                        this.RestartCoroutine(ref repositionCoroutine, RepositionElement(element));
                        element.adaptiveElementRepositionCoroutine = repositionCoroutine;
                        return;
                    }

                    var adaptiveTransform = element.adaptiveTransform;
                    var allowedDegreeOfGazeDivergence = element.allowedDegreeOfGazeDivergence;
                    var isAboveDivergenceThreshold = this.IsAboveDivergenceThreshold(adaptiveTransform, allowedDegreeOfGazeDivergence);
                    element.inFocus = !isAboveDivergenceThreshold; // gaze divergence threshold test regardless of temporal stability

                    if (!element.allowAdaptivePositioning)
                        continue;

                    if (repositionCoroutine == null)
                    {
                        var moveElement = false;
                        var isAboveTemporalDivergenceThreshold = this.IsAboveDivergenceThreshold(adaptiveTransform, allowedDegreeOfGazeDivergence, false);
                        if (element.repositionIfOutOfFocus && isAboveTemporalDivergenceThreshold)
                        {
                            moveElement = true;
                        }
                        else
                        {
                            var distanceFromGazeTransform = Mathf.Abs(Vector3.Magnitude(m_GazeTransform.position - adaptiveTransform.position)) / this.GetViewerScale();
                            if (distanceFromGazeTransform > element.allowedMaxDistanceDivergence)
                            {
                                // only move if above the gaze divergence threshold with respect to temporal stability
                                if (element.onlyMoveWhenOutOfFocus && !isAboveTemporalDivergenceThreshold)
                                    continue;

                                moveElement = true;
                            }
                            else if (distanceFromGazeTransform < element.allowedMinDistanceDivergence)
                            {
                                // Always move away from the hmd regardless of angle, in order to prevent the user from moving through the UI
                                moveElement = true;
                            }
                        }

                        if (moveElement)
                        {
                            this.RestartCoroutine(ref repositionCoroutine, RepositionElement(element));
                            element.adaptiveElementRepositionCoroutine = repositionCoroutine;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Implementers are assigned in the AdaptivePositionModuleConnector's ConnectInterface() function
        /// </summary>
        /// <param name="adaptiveElement">Implementer that will be added to the AdaptivePositionElements collection</param>
        public void ControlObject(IAdaptPosition adaptiveElement)
        {
            if (m_AdaptivePositionElements.Contains(adaptiveElement))
                return;

            var adaptiveTransform = adaptiveElement.adaptiveTransform;
            adaptiveTransform.localPosition = new Vector3(0f, 0f, adaptiveElement.adaptivePositionRestDistance); // push the object away from the HMD
            adaptiveTransform.parent = m_WorldspaceAnchorTransform;
            adaptiveTransform.rotation = Quaternion.identity;

            m_AdaptivePositionElements.Add(adaptiveElement);
            adaptiveElement.adaptivePositionData = new AdaptivePositionData(adaptiveElement, adaptiveTransform.position);
        }

        /// <summary>
        /// Implementers are removed from the AdaptivePositionModuleConnector's DisconnectInterface() function
        /// </summary>
        /// <param name="adaptiveElement">Implementer that will be removed from the AdaptivePositionElements collection</param>
        public void FreeObject(IAdaptPosition adaptiveElement)
        {
            if (m_AdaptivePositionElements.Contains(adaptiveElement))
                m_AdaptivePositionElements.Remove(adaptiveElement);
        }

        IEnumerator RepositionElement(IAdaptPosition adaptiveElement)
        {
            var adaptiveTransform = adaptiveElement.adaptiveTransform;
            var currentPosition = adaptiveTransform.position;
            var targetPosition = m_GazeTransform.position;
            targetPosition = targetPosition + this.GetViewerScale() * m_GazeTransform.forward * adaptiveElement.adaptivePositionRestDistance;
            if (!adaptiveElement.resetAdaptivePosition)
            {
                this.Pulse(Node.None, m_MovingPulse);
                const float kTransitionSpeedScalar = 4f;
                adaptiveElement.beingMoved = true;
                var transitionAmount = 0f;
                while (transitionAmount < 1f)
                {
                    var smoothTransition = MathUtilsExt.SmoothInOutLerpFloat(transitionAmount);
                    smoothTransition *= smoothTransition;
                    adaptiveTransform.position = Vector3.Lerp(currentPosition, targetPosition, smoothTransition);
                    transitionAmount += Time.deltaTime * kTransitionSpeedScalar;
                    adaptiveTransform.LookAt(m_GazeTransform);
                    yield return null;
                }
            }

            adaptiveTransform.position = targetPosition;
            adaptiveTransform.LookAt(m_GazeTransform);
            adaptiveElement.adaptiveElementRepositionCoroutine = null;
            adaptiveElement.beingMoved = false;
            adaptiveElement.resetAdaptivePosition = false;
        }
    }
}
#endif
