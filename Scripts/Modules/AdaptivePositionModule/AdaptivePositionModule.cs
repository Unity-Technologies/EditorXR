#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
    public sealed class AdaptivePositionModule : MonoBehaviour, IDetectGazeDivergence, IUsesViewerScale
    {
        //[SerializeField] GameObject m_TestObject;
        //Transform m_TestObjectTransform;
        //float m_TestObjectAllowedDistanceDivergece = 0.1f;
        //Vector3 m_TestObjectAnchoredWorldPosition;
        Coroutine m_AdaptiveElementRepositionCoroutine;
        //Coroutine m_TestObjectAnimRotationCoroutine;
        bool m_TestInFocus;
        //SpatialUI m_TestSpatialUI;

        /// <summary>
        /// Distance beyond which content will be re-positioned at the ideal distance from the user's gaze/hmd
        /// </summary>
        const float k_AllowedDistanceDivergence = 0.5f;

        /// <summary>
        /// The additional amount of diverge/tolerance allowed after an object has been anchored
        /// This prevents a constant repositioning of elements, if the gaze lies at the edge of the divergence tolerance region
        /// </summary>
        float allowedStableDivergence = 0.001f;

        Transform m_GazeTransform;

        Transform m_WorldspaceAnchorTransform; // The player transform under which anchored objects will be parented

        [SerializeField]
        HapticPulse m_MovementStartPulse; // The pulse performed when beginning to move an element to a new destination

        [SerializeField]
        HapticPulse m_MovingPulse; // The pulse performed while moving an element to a new target position in the user's FOV

        [SerializeField]
        HapticPulse m_MovementEndPulse; // The pulse performed when movement of an element has ended, the element has bee repositioned in the user's FOV

        // Collection housing objects whose position is controlled by this module
        readonly List<IAdaptPosition> m_AdaptivePositionElements = new List<IAdaptPosition>();

        public class AdaptivePositionData
        {
            public AdaptivePositionData(IAdaptPosition caller, bool centerVisuals = true)
            {
                this.caller = caller;
                this.previousAnchoredPosition = previousAnchoredPosition;
                this.centerVisuals = centerVisuals;
                spatialDirection = null;
                startingPosition = caller.adaptiveTransform.position;
            }

            // Below is Data assigned by calling object requesting spatial scroll processing

            /// <summary>
            /// The object/caller initiating this particular spatial scroll action
            /// </summary>
            public IAdaptPosition caller { get; set; }

            /// <summary>
            /// The world-position at which this object was last anchored
            /// </summary>
            public Vector3 previousAnchoredPosition { get; set; }

            /// <summary>
            /// The origin/starting position of the scroll
            /// </summary>
            public Vector3 startingPosition { get; set; }

            /// <summary>
            /// If true, expand scroll visuals out from the center of the trigger/origin/start position
            /// </summary>
            public bool centerVisuals { get; set; }

            // The Values below are populated by scroll processing

            /// <summary>
            /// The vector defining the spatial scroll direction
            /// </summary>
            public Vector3? spatialDirection { get; set; }

            public void UpdateExistingScrollData(Vector3 newPosition)
            {
                //currentPosition = newPosition;
            }
        }

        void Awake()
        {
            m_GazeTransform = CameraUtils.GetMainCamera().transform;
            m_WorldspaceAnchorTransform = m_GazeTransform.parent;

            /*
            m_TestObjectTransform = ObjectUtils.Instantiate(m_TestObject, m_GazeTransform, false).transform;
            m_TestSpatialUI = m_TestObjectTransform.GetComponent<SpatialUI>();
            m_TestSpatialUI.adaptivePositionData = new AdaptivePositionData(m_TestSpatialUI);

            m_AdaptivePositionElements.Add(m_TestSpatialUI);

            m_TestObjectTransform.localPosition = new Vector3(0f, 0f, m_TestSpatialUI.distanceOffset); // push the object away from the HMD
            m_TestObjectTransform.parent = m_WorldspaceAnchorTransform;
            m_TestObjectAnchoredWorldPosition = m_TestObjectTransform.position;
            m_TestObjectTransform.rotation = Quaternion.identity;
            */
        }

        void OnDestroy()
        {
            //ObjectUtils.Destroy(m_TestObjectTransform.gameObject);
        }

        void Update()
        {
            //Debug.LogWarning("Position: " + m_GazeTransform.position);
            //Debug.LogWarning("Rotation: " + m_GazeTransform.rotation.eulerAngles);

            //m_TestObjectTransform.LookAt(m_GazeTransform, m_WorldspaceAnchorTransform.up);
            //m_TestObjectTransform.rotation.SetLookRotation(m_GazeTransform.position - m_TestObjectTransform.position, Vector3.up);

            if (m_AdaptivePositionElements.Count > 0)
            {
                var adaptiveElement = m_AdaptivePositionElements.First(); // TODO: add support for multiple implementers
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

                //Debug.LogError(Mathf.Abs(Vector3.Magnitude(m_GazeTransform.position - m_TestObjectTransform.position)));
                //var distance = Mathf.Abs(Vector3.Magnitude(m_GazeTransform.position - m_TestObjectTransform.position));
                if (m_AdaptiveElementRepositionCoroutine == null && Mathf.Abs(Vector3.Magnitude(m_GazeTransform.position - adaptiveTransform.position)) > k_AllowedDistanceDivergence)
                {
                    var isAboveTemporalDivergenceThreshold = this.IsAboveDivergenceThreshold(adaptiveTransform, allowedDegreeOfGazeDivergence, false);
                    if (isAboveTemporalDivergenceThreshold) // only move if above the gaze divergence threshold with respect to temporal stability
                        this.RestartCoroutine(ref m_AdaptiveElementRepositionCoroutine, RepositionElement(adaptiveElement));
                }
            }

            //adaptiveElement.inFocus = !isAboveDivergenceThreshold;

            /*
            if (attemptiaimingOutsideOfGazeThreshold != m_TestInFocus)
            {
                m_TestInFocus = attemptiaimingOutsideOfGazeThreshold;
                this.RestartCoroutine(ref m_TestObjectAnimRotationCoroutine, TestObjectInFocus());
            }
            */
        }

        public void ControlObject(IAdaptPosition adaptiveElement)
        {
            m_AdaptivePositionElements.Add(adaptiveElement);

            var adaptiveTransform = adaptiveElement.adaptiveTransform;
            adaptiveTransform.localPosition = new Vector3(0f, 0f, adaptiveElement.distanceOffset); // push the object away from the HMD
            adaptiveTransform.parent = m_WorldspaceAnchorTransform;
            adaptiveTransform.rotation = Quaternion.identity;

            adaptiveElement.adaptivePositionData = new AdaptivePositionData(adaptiveElement);
            adaptiveElement.adaptivePositionData.previousAnchoredPosition = adaptiveTransform.position;
        }

        public void FreeObject(IAdaptPosition objectToStopControlling)
        {
            m_AdaptivePositionElements.Remove(objectToStopControlling);
        }

        IEnumerator RepositionElement(IAdaptPosition adaptiveElement)
        {
            var adaptiveTransform = adaptiveElement.adaptiveTransform;
            var currentPosition = adaptiveTransform.position;
            var targetPosition = m_GazeTransform.position;
            targetPosition = targetPosition + (this.GetViewerScale() * m_GazeTransform.forward * adaptiveElement.distanceOffset);
            if (!adaptiveElement.resetAdaptivePosition)
            {
                adaptiveElement.beingMoved = true;
                //Debug.LogWarning("TestObjectReposition: ");
                // + (Vector3.one * m_TestObjectDistanceOffset);// - new Vector3(0f, 0f, m_TestObjectDistanceOffset);
                var transitionAmount = 0f; // this should account for the magnitude difference between the highlightedYPositionOffset, and the current magnitude difference between the local Y and the original Y
                var transitionSubtractMultiplier = 2f;

                //var targetRotation = m_GazeTransform.localRotation; // set same local rotation, due to the target object transform parent being the same as the gaze source/camera
                //m_TestObjectTransform.rotation = m_GazeTransform.rotation;
                //m_TestObjectTransform.localPosition = new Vector3(0f, 0f, m_TestObjectDistanceOffset); // push the object away from the HMD

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

        /*
        IEnumerator TestObjectInFocus()
        {
            m_TestSpatialUI.beingMoved = !m_TestInFocus;

            var currentScale = m_TestObjectTransform.localScale;
            var targetScale = m_TestInFocus ? Vector3.one * 1f : Vector3.one * 0.5f;
            var transitionAmount = 0f; // this should account for the magnitude difference between the highlightedYPositionOffset, and the current magnitude difference between the local Y and the original Y
            var transitionSubtractMultiplier = 5f;
            while (transitionAmount < 1f)
            {
                var smoothTransition = MathUtilsExt.SmoothInOutLerpFloat(transitionAmount);
                m_TestObjectTransform.localScale = Vector3.Lerp(currentScale, targetScale, smoothTransition);
                transitionAmount += Time.deltaTime * transitionSubtractMultiplier;
                yield return null;
            }

            m_TestObjectTransform.localScale = targetScale;
            m_TestObjectAnimRotationCoroutine = null;
        }
        */
    }
}
#endif
