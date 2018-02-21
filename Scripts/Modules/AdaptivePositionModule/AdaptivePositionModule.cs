#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.SpatialUI;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngineInternal;


namespace UnityEditor.Experimental.EditorVR.Modules
{
    sealed class AdaptivePositionModule : MonoBehaviour, IDetectGazeDivergence
    {
        [SerializeField] GameObject m_TestObject;
        Transform m_TestObjectTransform;
        float m_TestObjectDistanceOffset = 0.5f;
        Vector3 m_TestObjectAnchoredWorldPosition;
        Coroutine m_TestObjectAnimCoroutine;
        bool m_TestInFocus;

        /// <summary>
        /// Distance beyond which content will be re-positioned at the ideal distance from the user's gaze/hmd
        /// </summary>
        const float k_AllowedDistanceDivergence = 0.1f;

        [SerializeField]
        HapticPulse m_MovementStartPulse; // The pulse performed when beginning to move an element to a new destination

        [SerializeField]
        HapticPulse m_MovingPulse; // The pulse performed while moving an element to a new target position in the user's FOV

        [SerializeField]
        HapticPulse m_MovementEndPulse; // The pulse performed when movement of an element has ended, the element has bee repositioned in the user's FOV

        // Collection housing objects whose position is controlled by this module
        readonly List<IAdaptPosition> m_AdaptivePositionElements = new List<IAdaptPosition>();

        Transform m_GazeTransform;

        Transform m_WorldspaceAnchorTransform; // The player transform under which anchored objects will be parented

        public class AdaptivePositionData : INodeToRay
        {
            public AdaptivePositionData(IAdaptPosition caller, Node node, Vector3 startingPosition, Vector3 currentPosition, float repeatingScrollLengthRange, int scrollableItemCount, int maxItemCount = -1, bool centerVisuals = true)
            {
                this.caller = caller;
                this.node = node;
                this.startingPosition = startingPosition;
                this.currentPosition = currentPosition;
                this.repeatingScrollLengthRange = repeatingScrollLengthRange;
                this.scrollableItemCount = scrollableItemCount;
                this.maxItemCount = maxItemCount;
                this.centerVisuals = centerVisuals;
                spatialDirection = null;
                rayOrigin = this.RequestRayOriginFromNode(node);
            }

            // Below is Data assigned by calling object requesting spatial scroll processing

            /// <summary>
            /// The object/caller initiating this particular spatial scroll action
            /// </summary>
            public IAdaptPosition caller { get; set; }

            /// <summary>
            /// The node on which this spatial scroll is being processed
            /// </summary>
            public Node node { get; set; }

            /// <summary>
            /// The ray origin on which this spatial scroll is being processed
            /// </summary>
            public Transform rayOrigin { get; set; }

            /// <summary>
            /// The origin/starting position of the scroll
            /// </summary>
            public Vector3 startingPosition { get; set; }

            /// <summary>
            /// The current scroll position
            /// </summary>
            public Vector3 currentPosition { get; set; }

            /// <summary>
            /// The magnitude at which a scroll will repeat/reset to its original scroll starting value
            /// </summary>
            public float repeatingScrollLengthRange { get; set; }

            /// <summary>
            /// Number of items being scrolled through
            /// </summary>
            public int scrollableItemCount { get; set; }

            /// <summary>
            /// Maximum number of items (to be scrolled through) that will be allowed
            /// </summary>
            public int maxItemCount { get; set; }

            /// <summary>
            /// If true, expand scroll visuals out from the center of the trigger/origin/start position
            /// </summary>
            public bool centerVisuals { get; set; }

            // The Values below are populated by scroll processing

            /// <summary>
            /// The vector defining the spatial scroll direction
            /// </summary>
            public Vector3? spatialDirection { get; set; }

            /// <summary>
            /// 0-1 offset/magnitude of current scroll position, relative to the trigger/origin/start point, and the repeatingScrollLengthRange
            /// </summary>
            public float normalizedLoopingPosition { get; set; }

            /// <summary>
            /// Value representing how much of the pre-scroll drag amount has occurred
            /// </summary>
            public float dragDistance { get; set; }

            /// <summary>
            /// Bool denoting that the scroll trigger magnitude has been exceeded
            /// </summary>
            public bool passedMinDragActivationThreshold { get { return spatialDirection != null; } }

            public void UpdateExistingScrollData(Vector3 newPosition)
            {
                currentPosition = newPosition;
            }
        }

        void Start()
        {
            m_GazeTransform = CameraUtils.GetMainCamera().transform;
            m_WorldspaceAnchorTransform = m_GazeTransform.parent;

            m_TestObjectTransform = ObjectUtils.Instantiate(m_TestObject, m_GazeTransform, false).transform;
            m_TestObjectTransform.localPosition = new Vector3(0f, 0f, m_TestObjectDistanceOffset); // push the object away from the HMD
            m_TestObjectTransform.parent = m_WorldspaceAnchorTransform;
            m_TestObjectAnchoredWorldPosition = m_TestObjectTransform.position;
            m_TestObjectTransform.rotation = Quaternion.identity;
        }

        void OnDestroy()
        {
            ObjectUtils.Destroy(m_TestObjectTransform.gameObject);
        }

        void Update()
        {
            //Debug.LogWarning("Position: " + m_GazeTransform.position);
            //Debug.LogWarning("Rotation: " + m_GazeTransform.rotation.eulerAngles);

            m_TestObjectTransform.LookAt(m_GazeTransform, m_WorldspaceAnchorTransform.up);
            //m_TestObjectTransform.rotation.SetLookRotation(m_GazeTransform.position - m_TestObjectTransform.position, Vector3.up);

            var attemptiaimingOutsideOfGazeThreshold = this.IsAboveDivergenceThreshold(m_TestObjectTransform, 15f);

            if (attemptiaimingOutsideOfGazeThreshold != m_TestInFocus)
            {
                m_TestInFocus = attemptiaimingOutsideOfGazeThreshold;
                this.RestartCoroutine(ref m_TestObjectAnimCoroutine, TestObjectInFocus());
            }
        }

        IEnumerator TestObjectInFocus()
        {
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
            m_TestObjectAnimCoroutine = null;
        }
    }
}
#endif
