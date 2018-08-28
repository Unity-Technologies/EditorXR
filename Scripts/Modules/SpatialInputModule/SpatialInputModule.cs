#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Helpers;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
    /// <summary>
    /// The detectable types of spatial input that a node can perform
    /// </summary>
    [Flags]
    public enum SpatialInputType
    {
        None = 0,
        DragTranslation = 1 << 3,
        StateChangedThisFrame = 1 << 7
    }

    public sealed class SpatialInputModule : MonoBehaviour, IRayVisibilitySettings, IUsesViewerScale, IControlHaptics, IControlSpatialHinting
    {
        public enum SpatialCardinalScrollDirection
        {
            None,
            LocalX,
            LocalY,
            LocalZ
        }

        public enum ScrollRepeatType
        {
            Looping,
            Clamped
        }

        public class SpatialScrollData : INodeToRay, IUsesViewerScale
        {
            public SpatialScrollData(IProcessSpatialInput caller, Node node, Vector3 startingPosition, Vector3 currentPosition, float scrollLengthRange, ScrollRepeatType scrollRepeatType, int scrollableItemCount, int maxItemCount = -1, bool centerVisuals = true)
            {
                this.caller = caller;
                this.node = node;
                this.startingPosition = startingPosition;
                this.currentPosition = currentPosition;
                this.scrollLengthRange = scrollLengthRange;
                this.scrollableItemCount = scrollableItemCount;
                this.maxItemCount = maxItemCount;
                this.centerVisuals = centerVisuals;
                spatialDirection = null;
                rayOrigin = this.RequestRayOriginFromNode(node);
                directionChangedUpdatedConstrainedReferencePosition = startingPosition;
                m_ContinuousDirectionalVelocityTracker.Initialize(this.RequestRayOriginFromNode(node).position);
                this.scrollRepeatType = scrollRepeatType;
            }

            int m_lastChangedFrame;
            Vector3 m_PreviousProjectedVector;
            Vector3 m_CurrentProjectedVector;
            ContinuousDirectionalVelocityTracker m_ContinuousDirectionalVelocityTracker = new ContinuousDirectionalVelocityTracker();

            public SpatialCardinalScrollDirection spatialCardinalScrollDirection { get; set; }
            public ScrollRepeatType scrollRepeatType { get; set; }

            /// <summary>
            /// When not having been polled for a frame, stop monitoring the corresponding transform in Update
            /// </summary>
            public bool beingPolled
            {
                get
                {
                    // after a frame in which this data isn't polled/updated, end the scroll action
                    return Time.frameCount - m_lastChangedFrame < 2;
                }
                set
                {
                    // update the last changed frame value, as a convenience function/property
                    if (value)
                        m_lastChangedFrame = Time.frameCount;
                }
            }

            // Below is Data assigned by calling object requesting spatial scroll processing

            /// <summary>
            /// The object/caller initiating this particular spatial scroll action
            /// </summary>
            public IProcessSpatialInput caller { get; set; }

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
            public float scrollLengthRange { get; set; }

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
            public float normalizedLoopingPositionUnconstrained { get; set; }

            public float normalizedLoopingPositionXConstrained { get; set; }

            public float normalizedLoopingPositionYConstrained { get; set; }

            /// <summary>
            /// Value representing how much of the pre-scroll drag amount has occurred
            /// </summary>
            public float dragDistance { get; set; }

            /// <summary>
            /// Bool denoting that the scroll trigger magnitude has been exceeded
            /// </summary>
            public bool passedMinDragActivationThreshold { get { return spatialDirection != null; } }

            /// <summary>
            /// Order/position of the currently highlighted menu element
            /// </summary>
            public int highlightedMenuElementPositionUnconstrained { get { return (int)(scrollableItemCount * normalizedLoopingPositionUnconstrained); } }

            //public int highlightedMenuElementPositionYConstrained { get { return (int)(scrollableItemCount * normalizedLoopingPositionYConstrained); } }
            int m_HighlightedMenuElementsCycledThrough;
            public int clampedHighlightedMenuElementPositionXConstrained { get { return (int)Mathf.Clamp(m_HighlightedMenuElementsCycledThrough, 0, scrollableItemCount); } }
            public int loopingHighlightedMenuElementPositionXConstrained { get { return (int)Mathf.Repeat(m_HighlightedMenuElementsCycledThrough, scrollableItemCount); } }
            public int loopingHighlightedMenuElementPositionYConstrained { get { return (int)Mathf.Repeat(m_HighlightedMenuElementsCycledThrough, scrollableItemCount); } }

            private const float k_ProjectedVectorUpdateInterval = 0.125f;
            float m_NextProjectedVectorUpdateTime;
            public Vector3 previousProjectedVector { get { return m_PreviousProjectedVector; } }
            public Vector3 currentProjectedVector
            {
                get { return m_CurrentProjectedVector; }
                set
                {
                    m_ContinuousDirectionalVelocityTracker.Update(value, Time.unscaledDeltaTime);

                    // Prevent micro-movements from triggering a highlighted menu element position update
                    if (Vector3.Magnitude(m_CurrentProjectedVector - value) * this.GetViewerScale() < 0.0125f * this.GetViewerScale())
                        return;

                    // Limit the projection update rate independent of delta time
                    if (m_NextProjectedVectorUpdateTime > Time.realtimeSinceStartup)
                        return;

                    var directionalVelocity = 1f;// m_ContinuousDirectionalVelocityTracker.directionalVelocity;
                    m_PreviousProjectedVector = m_CurrentProjectedVector; // automatically update previous projected vector when setting new current projected vector
                    m_CurrentProjectedVector = value;
                    m_NextProjectedVectorUpdateTime = Time.realtimeSinceStartup + k_ProjectedVectorUpdateInterval;

                    if (movingInPositiveDirectionOnConstrainedAxis != m_PreviouslyMovingInPositivelyConstrainedDirection)
                    {
                        directionChangedUpdatedConstrainedReferencePosition = m_CurrentProjectedVector;// + m_CurrentProjectedVector * 0.5f;
                        Debug.Log("<color=red>constrained directional scrolling has reversed!</color> : " + movingInPositiveDirectionOnConstrainedAxis);
                    }

                    m_PreviouslyMovingInPositivelyConstrainedDirection = movingInPositiveDirectionOnConstrainedAxis;
                    var direction = movingInPositiveDirectionOnConstrainedAxis ? -1 : 1; // multiplier that singifies that m_HighlightedMenuElementsCycledThrough will be either incremented or decremented, based on the direction of input
                    var highlightedElementScrollAddition = 0;

                    switch (scrollRepeatType)
                    {
                        case ScrollRepeatType.Clamped:
                            highlightedElementScrollAddition = m_HighlightedMenuElementsCycledThrough + (int)((m_CurrentProjectedVector - directionChangedUpdatedConstrainedReferencePosition).magnitude * direction * this.GetViewerScale() * 10f * directionalVelocity);
                            var newIncrementalPosition = m_HighlightedMenuElementsCycledThrough + highlightedElementScrollAddition;
                            var incrementedValue = m_HighlightedMenuElementsCycledThrough + newIncrementalPosition;
                            if (incrementedValue < 0 || incrementedValue > scrollableItemCount)
                                highlightedElementScrollAddition = 0; // Clamped value should be within the bounds of the scrollableItemCount
                            break;
                        case ScrollRepeatType.Looping:
                            Debug.LogError("Validate Looping spatial scrolling after axis constraints were added.");
                            highlightedElementScrollAddition = (int)((m_CurrentProjectedVector - directionChangedUpdatedConstrainedReferencePosition).magnitude * direction * this.GetViewerScale() * 6f * directionalVelocity);
                            break;
                    }

                    m_HighlightedMenuElementsCycledThrough += highlightedElementScrollAddition;
                    Debug.Log(highlightedElementScrollAddition + " : <color=green>Updating current projected vector of scroll data</color> : " + m_CurrentProjectedVector + " - highlightedMenuElementsCycledThrough : " + m_HighlightedMenuElementsCycledThrough + " : directionChangedUpdatedConstrainedReferencePosition : " + directionChangedUpdatedConstrainedReferencePosition);
                    Debug.Log("m_CurrentProjectedVector : " + m_CurrentProjectedVector + " - directionChangedUpdatedConstrainedReferencePosition : " + directionChangedUpdatedConstrainedReferencePosition + " : MAGNITUDE: " + (m_CurrentProjectedVector - directionChangedUpdatedConstrainedReferencePosition).magnitude);
                }
            }

            bool m_PreviouslyMovingInPositivelyConstrainedDirection;
            public bool movingInPositiveDirectionOnConstrainedAxis
            {
                get
                {
                    var movingInPositiveDirection = false;
                    switch (spatialCardinalScrollDirection)
                    {
                        case SpatialCardinalScrollDirection.LocalX:
                            movingInPositiveDirection = m_PreviousProjectedVector.x - m_CurrentProjectedVector.x > 0;
                            break;
                        case SpatialCardinalScrollDirection.LocalY:
                            movingInPositiveDirection = m_CurrentProjectedVector.y - m_PreviousProjectedVector.y > 0;
                            //Debug.LogWarning("moving in positive direction : " + movingInPositiveDirection);
                            break;
                        case SpatialCardinalScrollDirection.LocalZ:
                            Debug.LogError("Z-based constrained spatial scrolling is not implemented");
                            //movingInPositiveDirection = m_CurrentProjectedVector.x - m_PreviousProjectedVector.x > 0;
                            break;
                    }

                    return movingInPositiveDirection;
                }
            }

            Vector3 directionChangedUpdatedConstrainedReferencePosition { get; set; } // TODO: rename

            public void UpdateExistingScrollData(Vector3 newPosition)
            {
                currentPosition = newPosition;
            }
        }

        public class SpatialInputReceiverData : INodeToRay
        {
            // SpatialInputReceiver interface reference / caller
            // Transform/rayorigin
            // bool request made this frame
            // bool request made previous frame
            // initialLocalPosition / cached when a new request this frame bool is set
            // initialLocalRotation / cached when a new request this frame bool is set
            // currentLocalPosition / cached each frame in which either request bool is true
            // currentLocalRotation / cached each frame in which either request bool is true
            // processing & cacheing of currentPosition+rotation is skipped if no request was made this frame, or previous frame
            //
            public SpatialInputReceiverData(Node node, IProcessSpatialInput caller)
            {
                this.caller = caller;
                rayOrigin = this.RequestRayOriginFromNode(node);
                //m_Callers.Add(caller);

                initialPosition = rayOrigin.position;
                initialLocalRotation = rayOrigin.localRotation;
                spatialInputType = SpatialInputType.None;
            }

            SpatialInputType m_SpatialInputType;
            bool m_PolledThisFrame;
            bool m_PolledPreviousFrame;

            // Collection housing caller objects requesting that this node be evaluated
            //readonly List<IDetectSpatialInputType> m_Callers = new List<IDetectSpatialInputType>();

            /// <summary>
            /// The object/caller that will request spatial input related processing for a given node
            /// </summary>
            public IProcessSpatialInput caller { get; set; }

            /// <summary>
            /// The ray origin on which this spatial scroll is being processed
            /// </summary>
            public Transform rayOrigin { get; set; }

            /// <summary>
            /// Signed starting-position-dependent direction
            /// Each time an input type change occurs, this value is then recalculated from the new local input direction type
            /// </summary>
            public float signedDeltaMagnitude { get; set; }

            /// <summary>
            /// The origin/starting input position
            /// </summary>
            public Vector3 initialPosition { get; set; }

            /// <summary>
            /// The current input position
            /// </summary>
            public Vector3 currentPosition { get { return rayOrigin.position; } }

            /// <summary>
            /// The origin/starting input rotation
            /// </summary>
            public Quaternion initialLocalRotation { get; set; }

            /// <summary>
            /// The current input rotation
            /// </summary>
            public Quaternion currentLocalRotation { get { return rayOrigin.localRotation; } }

            public float CurrenLocalXRotation { get { return rayOrigin.localRotation.eulerAngles.x; } }
            public float CurrenLocalYRotation { get { return rayOrigin.localRotation.eulerAngles.y; } }
            public float CurrenLocalZRotation { get { return rayOrigin.localRotation.eulerAngles.z; } }

            /* TODO: multiple callers per-node is not needed at this time, refactor out of system
            public void AddCaller(IDetectSpatialInputType caller)
            {
                if (!m_Callers.Contains(caller))
                    m_Callers.Add(caller);
            }

            public bool RemoveCaller(IDetectSpatialInputType caller)
            {
                var noCallersRemain = false;
                foreach (var existingCaller in m_Callers)
                {
                    if (existingCaller == caller)
                    {
                        m_Callers.Remove(caller);
                        break;
                    }
                }

                return m_Callers.Count > 0;
            }
            */

            /// <summary>
            /// When not having been polled for a frame, stop monitoring the corresponding transform in Update
            /// </summary>
            public bool beingPolled { get { return (m_PolledThisFrame || m_PolledPreviousFrame); } }

            public bool stateChangedThisFrame { get; set; }

            /// <summary>
            /// Current evaluated spatial input type being performed by this node
            /// </summary>
            public SpatialInputType spatialInputType
            {
                get { return m_SpatialInputType; }

                set
                {
                    if (m_SpatialInputType == value)
                    {
                        stateChangedThisFrame = false;
                        return;
                    }

                    //Debug.LogWarning("Changing state to : " + spatialInputType.ToString() + " : " + rayOrigin.name);

                    stateChangedThisFrame = true;
                    m_SpatialInputType = value; // Set new state
                    m_SpatialInputType |= SpatialInputType.StateChangedThisFrame; // Set frame change flag
                    if (m_SpatialInputType == SpatialInputType.None)
                        return;

                    // A new ACTIVE state has been set
                    // Cache new relevant transform values for further comparision/validation
                    initialPosition = rayOrigin.position;
                    initialLocalRotation = rayOrigin.localRotation;
                }
            }
        }

        // Serialized Field region
        [SerializeField]
        HapticPulse m_TranslationPulse; // The pulse performed on a node performing a spatial scroll while only in translation mode

        [SerializeField]
        HapticPulse m_FreeRotationPulse; // The pulse performed on a node performing a spatial scroll while only in free-rotation mode

        [SerializeField]
        HapticPulse m_SingleAxistRotationPulse; // The pulse performed on a node performing a spatial scroll while only in single-axis rotation mode

        // Collection housing objects whose spatial input is being processed
        readonly Dictionary<Node, SpatialInputReceiverData> m_SpatialNodeData = new Dictionary<Node, SpatialInputReceiverData>();

        // Collection housing objects whose scroll data is being processed
        readonly List<IProcessSpatialInput> m_SpatialScrollCallers = new List<IProcessSpatialInput>();

        RotationVelocityTracker m_RotationVelocityTracker = new RotationVelocityTracker();

        Transform m_HMDTransform;

        void Awake()
        {
            m_HMDTransform = CameraUtils.GetMainCamera().transform;
        }

        void Update()
        {
            if (m_SpatialScrollCallers.Count > 0)
            {
                // Automatically prune any spatial scroll data that isn't currently being polled(updated/active)
                foreach (var scroller in m_SpatialScrollCallers)
                {
                    if (!scroller.spatialScrollData.beingPolled)
                    {
                        this.RemoveRayVisibilitySettings(scroller.spatialScrollData.rayOrigin, scroller);
                        this.SetSpatialHintState(SpatialHintModule.SpatialHintStateFlags.Hidden);
                        scroller.spatialScrollData = null; // clear reference to the previously used scrollData
                        m_SpatialScrollCallers.Remove(scroller);
                        return;
                    }
                }
            }

            // Iterate over all ACTIVE(performing spatial input) nodes perform a spatial scroll
            // Update the SpatialInputType for each ACTIVE node
            // Set SpatialInputType for nodes not performing any spatial input to NONE
            // Otherwise, set relevant SpatialInputType value

            foreach (var nodeToSpatialData in m_SpatialNodeData)
            {
                var spatialInputData = nodeToSpatialData.Value;
                if (!spatialInputData.beingPolled)
                {
                    // Spatial input is NOT being performed on this node
                    // A frame with a state of NONE has already been processed, now unset the stateChangedThisFrameValue
                    /*
                    if (spatialInputData.spatialInputType == SpatialInputType.StateChangedThisFrame)
                    {
                        spatialInputData.spatialInputType |= SpatialInputType.StateChangedThisFrame; // Clear frame change flag
                        spatialInputData.stateChangedThisFrame = false; // Consider removing
                    }
                    */

                    spatialInputData.spatialInputType = SpatialInputType.None; // Clears frame change flag
                }
                else
                {
                    //spatialProxyRayOrigin.localRotation = spatialInputData.currentLocalRotation;
                    //this.UpdateSpatialRay();

                    // New initial position & rotation was just set
                    // Skip further evaluation for this data this frame for efficiency; evaluate next frame
                    if (spatialInputData.stateChangedThisFrame)
                        return;

                    m_RotationVelocityTracker.Update(spatialInputData.currentLocalRotation, Time.deltaTime);
                    //Debug.LogError("Rotation strength " + m_RotationVelocityTracker.rotationStrength);

                    // Order tests based on the active spatial input type of the node
                    // Testing for the opposite type of input will set the SpatialInputType accordingly, if a given input type change has occurred
                    switch (spatialInputData.spatialInputType)
                    {
                        case SpatialInputType.DragTranslation:
                            //isNodeRotatingSingleAxisOrFreely(spatialInputData);
                            break;
                        case SpatialInputType.None:
                            isNodeTranslating(spatialInputData);
                            break;
                    }
                }
            }
        }

        bool isNodeTranslating(SpatialInputReceiverData nodeReceiverData)
        {
            const float kSubMenuNavigationTranslationTriggerThreshold = 0.075f;
            var initialPosition = nodeReceiverData.initialPosition;
            var currentPosition = nodeReceiverData.currentPosition;
            var aboveMagnitudeDeltaThreshold = Vector3.Magnitude(initialPosition - currentPosition) > kSubMenuNavigationTranslationTriggerThreshold;

            if (aboveMagnitudeDeltaThreshold)
            {
                nodeReceiverData.spatialInputType = SpatialInputType.DragTranslation;
            }

            /* No need to test this, only another rotation state, or a state of NONE will clear the value
            if (!aboveMagnitudeDeltaThreshold && nodeData.spatialInputType == SpatialInputType.DragTranslation)
            {
                // Clear dragTranslation state if the previous state was dragTranslation, and now below the threshold
                nodeData.spatialInputType = SpatialInputType.None;
            }
            */

            return aboveMagnitudeDeltaThreshold;
        }

        // Perform a constant haptic for translation/dragging
        // Perform a sharply pulsing haptic for rotation
        // Perform a gradual pulsing for free rotation
        // Monitor and perform the relevant pulses for all registered nodes, when spatial scrolling is being performed by that node

        /// <summary>
        /// Initiate spatial input processing for a given node & caller
        /// </summary>
        /// <param name="caller">Object requesting that a given node be tracked</param>
        /// <param name="node">Node whose input will be processed.  A caller may track multiple nodes.</param>
        public void BeginSpatialInputDetection(IDetectSpatialInputType caller, Node node)
        {
        /*
            SpatialInputReceiverData existingReceiverData = null;
            foreach (var nodeData in m_SpatialNodeData)
            {
                if (nodeData.Key == node)
                {
                    existingReceiverData = nodeData.Value;
                    break;
                }
            }

            if (existingReceiverData != null)
            {
                existingReceiverData.AddCaller(caller);
            }
            else
            {
                // Create a new KVP for a node not currently being processed
                // Additional callers can be added to a node's correspondng SpatialInputData
                var newTrackedObjectData = new SpatialInputReceiverData(node, caller);
                m_SpatialNodeData.Add(node, newTrackedObjectData);
            }
        */
        }

        public void EndSpatialInputDetection(IDetectSpatialInputType caller, Node node)
        {
        /*
            // remove caller from any spatialInputData objects referencing this caller
            // If no callers remain for a node, remove the corresponding entry from the SpatialNodeData collection
            foreach (var nodeData in m_SpatialNodeData)
            {
                if (nodeData.Key == node)
                {
                    var spatialInputData = nodeData.Value;
                    var noCallersRemaining = spatialInputData.RemoveCaller(caller);
                    if (noCallersRemaining)
                        m_SpatialNodeData.Remove(node);

                    break;
                }
            }
        */
        }

        /// <summary>
        /// Separate helper function, due to the logic re-use for individual axis'
        /// </summary>
        /// <param name="initialSingleAxisRotationValue"></param>
        /// <param name="currentSingleAxisRotationValue"></param>
        /// <returns></returns>
        bool PerformSingleAxisRotationTest(float initialSingleAxisRotationValue, float currentSingleAxisRotationValue)
        {
            const float kRotationThreshold = 0.3f; // Estimated wrist rotation threshold
            var deltaAngle = Mathf.Abs(Mathf.DeltaAngle(initialSingleAxisRotationValue, currentSingleAxisRotationValue));
            var aboveThreshold = deltaAngle > kRotationThreshold;
            return aboveThreshold;
        }

        public SpatialInputType GetSpatialInputTypeForNode(IDetectSpatialInputType obj, Node node)
        {
            // Iterate on the node to active state collection
            // Return none for those not performing a spatial input action
            // Return the relevant SpatialInputType for a given node otherwise

            var nodeDetected = false;
            SpatialInputType spatialInputType = SpatialInputType.None;
            /*
            foreach (var nodeToInputType in m_SpatialNodeData)
            {
                if (nodeToInputType.Key == node)
                {
                    var nodeData = nodeToInputType.Value;
                    nodeData.AddCaller(obj);
                    spatialInputType = nodeData.spatialInputType;
                    break;
                }
            }

            if (!nodeDetected)
            {
                // Node not found, add node and caller to collection
                BeginSpatialInputDetection(obj, node);
            }
            */

            return spatialInputType;
        }

        internal SpatialScrollData PerformSpatialScroll(IProcessSpatialInput caller, Node node, Vector3 startingPosition, Vector3 currentPosition, float scrollLengthRange, ScrollRepeatType scrollRepeatType, int scrollableItemCount, int maxItemCount = -1, bool centerScrollVisuals = true)
        {
            // Continue processing of spatial scrolling for a given caller,
            // Or create new instance of scroll data for new callers. (Initial structure for support of simultaneous callers)
            SpatialScrollData scrollData = null;
            foreach (var scroller in m_SpatialScrollCallers)
            {
                if (scroller == caller)
                {
                    scrollData = scroller.spatialScrollData;
                    scrollData.UpdateExistingScrollData(currentPosition);
                    break;
                }
            }

            if (scrollData == null)
            {
                scrollData = new SpatialScrollData(caller, node, startingPosition, currentPosition, scrollLengthRange, scrollRepeatType, scrollableItemCount, maxItemCount, centerScrollVisuals);
                m_SpatialScrollCallers.Add(caller);
                this.AddRayVisibilitySettings(scrollData.rayOrigin, caller, false, false, 1);
            }

            var directionVector = currentPosition - startingPosition;
            if (scrollData.spatialDirection == null)
            {
                var newDirectionVectorThreshold = 0.0175f; // Initial magnitude beyond which spatial scrolling will be evaluated
                newDirectionVectorThreshold *= this.GetViewerScale();
                var dragMagnitude = Vector3.Magnitude(directionVector);
                var dragPercentage = dragMagnitude / newDirectionVectorThreshold;
                const int kPulseSpeedMultiplier = 20;
                const float kPulseThreshold = 0.5f;
                const float kPulseOnAmount = 1f;
                const float kPulseOffAmount = 0f;
                var repeatingPulseAmount = Mathf.Sin(Time.realtimeSinceStartup * kPulseSpeedMultiplier) > kPulseThreshold ? kPulseOnAmount : kPulseOffAmount; // Perform an on/off repeating pulse while waiting for the drag threshold to be crossed
                scrollData.dragDistance = dragMagnitude > 0 ? dragPercentage : 0f; // Set value representing how much of the pre-scroll drag amount has occurred
                //this.Pulse(node, m_ActivationPulse, repeatingPulseAmount, repeatingPulseAmount);
                if (dragMagnitude > newDirectionVectorThreshold)
                    scrollData.spatialDirection = directionVector; // Initialize vector defining the spatial scroll direction
            }
            else
            {
                var spatialDirection = scrollData.spatialDirection.Value;
                var scrollingAfterTriggerOirigin = Vector3.Dot(directionVector, spatialDirection) >= 0; // Detect that the user is scrolling forward from the trigger origin point
                var projectionVector = scrollingAfterTriggerOirigin ? spatialDirection : spatialDirection + spatialDirection;
                var projectedAmount = Vector3.Project(directionVector, projectionVector).magnitude / this.GetViewerScale();

                // Mandate that scrolling maintain the initial direction, regardless of the user scrolling before/after the trigger origin point; prevent direction flipping
                projectedAmount = scrollingAfterTriggerOirigin ? projectedAmount : 1 - projectedAmount;
                scrollData.normalizedLoopingPositionUnconstrained = (Mathf.Abs(projectedAmount * (maxItemCount / scrollableItemCount)) % scrollLengthRange) * (1 / scrollLengthRange);
            }

            return scrollData;
        }

        internal SpatialScrollData PerformLocalCardinallyConstrainedSpatialScroll(IProcessSpatialInput caller, SpatialCardinalScrollDirection cardinalScrollDirection, Node node, Vector3 startingPosition, Vector3 currentPosition, float scrollLengthRange, ScrollRepeatType scrollRepeatType, int scrollableItemCount, int maxItemCount = -1, bool centerScrollVisuals = true)
        {
            // Continue processing of spatial scrolling for a given caller,
            // Or create new instance of scroll data for new callers. (Initial structure for support of simultaneous callers)
            SpatialScrollData scrollData = null;
            if (cardinalScrollDirection == SpatialCardinalScrollDirection.None)
            {
                Debug.LogWarning("A Cardinal scroll direction must be defined when performing a cardinally constrained spatial scroll!");
                return scrollData;
            }

            foreach (var scroller in m_SpatialScrollCallers)
            {
                if (scroller == caller)
                {
                    scrollData = scroller.spatialScrollData;
                    scrollData.UpdateExistingScrollData(currentPosition);
                    break;
                }
            }

            if (scrollData == null)
            {
                scrollData = new SpatialScrollData(caller, node, startingPosition, currentPosition, scrollLengthRange, scrollRepeatType, scrollableItemCount, maxItemCount, centerScrollVisuals);
                scrollData.spatialCardinalScrollDirection = cardinalScrollDirection;
                m_SpatialScrollCallers.Add(caller);
                this.AddRayVisibilitySettings(scrollData.rayOrigin, caller, false, false, 1);
            }

            scrollData.beingPolled = true;

            var cardinalDirectionVector = startingPosition;
            /*
            switch (cardinalScrollDirection)
            {
                case SpatialCardinalScrollDirection.LocalX:
                    cardinalDirectionVector = Vector3.left;
                    break;
                case SpatialCardinalScrollDirection.LocalY:
                    cardinalDirectionVector = Vector3.up;
                    break;
                case SpatialCardinalScrollDirection.LocalZ:
                    cardinalDirectionVector = Vector3.forward;
                    break;
            }
            */

            var directionVector = currentPosition - cardinalDirectionVector;

            // Define the initial vector upon which further spatial scrolling will be orthogonally projected upon
            var hmdToDeviceInitialVector = Vector3.Normalize(m_HMDTransform.position - startingPosition);
            var hmdToDeviceCurrentVector = Vector3.Normalize(m_HMDTransform.position - currentPosition);
            var constrainedProjectedVector = Vector3.ProjectOnPlane(hmdToDeviceCurrentVector, hmdToDeviceInitialVector);

            Debug.Log("Projected vector : <color=red>" + constrainedProjectedVector + "</color>");
            switch (cardinalScrollDirection)
            {
                    case SpatialCardinalScrollDirection.LocalX:
                        constrainedProjectedVector.y = 0;
                        constrainedProjectedVector.z = 0;
                        break;
                    case SpatialCardinalScrollDirection.LocalY:
                        constrainedProjectedVector.x = 0;
                        constrainedProjectedVector.z = 0;
                        break;
                    case SpatialCardinalScrollDirection.LocalZ:
                        constrainedProjectedVector.x = 0;
                        constrainedProjectedVector.y = 0;
                        break;
            }

            Debug.Log("Projected vector : <color=green>" + constrainedProjectedVector + "</color>");
            scrollData.currentProjectedVector = constrainedProjectedVector;

            //project additional positions upon the plane defined by the hmdToDeviceInitialVector

            if (scrollData.spatialDirection == null)
            {
                var newDirectionVectorThreshold = 0.0175f; // Initial magnitude beyond which spatial scrolling will be evaluated
                newDirectionVectorThreshold *= this.GetViewerScale();
                var dragMagnitude = Vector3.Magnitude(directionVector);
                var dragPercentage = dragMagnitude / newDirectionVectorThreshold;
                const int kPulseSpeedMultiplier = 20;
                const float kPulseThreshold = 0.5f;
                const float kPulseOnAmount = 1f;
                const float kPulseOffAmount = 0f;
                var repeatingPulseAmount = Mathf.Sin(Time.realtimeSinceStartup * kPulseSpeedMultiplier) > kPulseThreshold ? kPulseOnAmount : kPulseOffAmount; // Perform an on/off repeating pulse while waiting for the drag threshold to be crossed
                scrollData.dragDistance = dragMagnitude > 0 ? dragPercentage : 0f; // Set value representing how much of the pre-scroll drag amount has occurred
                //this.Pulse(node, m_ActivationPulse, repeatingPulseAmount, repeatingPulseAmount);
                if (dragMagnitude > newDirectionVectorThreshold)
                    scrollData.spatialDirection = directionVector; // Initialize vector defining the spatial scroll direction
            }
            else
            {
                var spatialDirection = scrollData.spatialDirection.Value;
                var scrollingAfterTriggerOirigin = Vector3.Dot(directionVector, spatialDirection) >= 0; // Detect that the user is scrolling forward from the trigger origin point
                var projectionVector = scrollingAfterTriggerOirigin ? spatialDirection : spatialDirection + spatialDirection;
                var projectedAmount = Vector3.Project(directionVector, projectionVector).magnitude / this.GetViewerScale();
                projectedAmount *= scrollData.movingInPositiveDirectionOnConstrainedAxis ? 1 : -1;

                // Mandate that scrolling maintain the initial direction, regardless of the user scrolling before/after the trigger origin point; prevent direction flipping
                projectedAmount = scrollingAfterTriggerOirigin ? projectedAmount : 1 - projectedAmount;
                scrollData.normalizedLoopingPositionUnconstrained = (Mathf.Abs(projectedAmount * (maxItemCount / scrollableItemCount)) % scrollLengthRange) * (1 / scrollLengthRange);
                scrollData.normalizedLoopingPositionYConstrained = (Mathf.Abs(projectedAmount * (maxItemCount / scrollableItemCount)) % scrollLengthRange) * (1 / scrollLengthRange);
            }

            return scrollData;
        }

        internal SpatialScrollData PerformOriginalSpatialScroll(IProcessSpatialInput caller, Node node, Vector3 startingPosition, Vector3 currentPosition, float scrollLengthRange, SpatialInputModule.ScrollRepeatType scrollRepeatType, int scrollableItemCount, int maxItemCount = -1, bool centerScrollVisuals = true)
        {
            // Continue processing of spatial scrolling for a given caller,
            // Or create new instance of scroll data for new callers. (Initial structure for support of simultaneous callers)
            SpatialScrollData scrollData = null;
            foreach (var scroller in m_SpatialScrollCallers)
            {
                if (scroller == caller)
                {
                    scrollData = scroller.spatialScrollData;
                    scrollData.UpdateExistingScrollData(currentPosition);
                    break;
                }
            }

            if (scrollData == null)
            {
                scrollData = new SpatialScrollData(caller, node, startingPosition, currentPosition, scrollLengthRange, scrollRepeatType, scrollableItemCount, maxItemCount, centerScrollVisuals);
                m_SpatialScrollCallers.Add(caller);
                this.AddRayVisibilitySettings(scrollData.rayOrigin, caller, false, false, 1);
            }

            var directionVector = currentPosition - startingPosition;
            if (scrollData.spatialDirection == null)
            {
                var newDirectionVectorThreshold = 0.0175f; // Initial magnitude beyond which spatial scrolling will be evaluated
                newDirectionVectorThreshold *= this.GetViewerScale();
                var dragMagnitude = Vector3.Magnitude(directionVector);
                var dragPercentage = dragMagnitude / newDirectionVectorThreshold;
                const int kPulseSpeedMultiplier = 20;
                const float kPulseThreshold = 0.5f;
                const float kPulseOnAmount = 1f;
                const float kPulseOffAmount = 0f;
                var repeatingPulseAmount = Mathf.Sin(Time.realtimeSinceStartup * kPulseSpeedMultiplier) > kPulseThreshold ? kPulseOnAmount : kPulseOffAmount; // Perform an on/off repeating pulse while waiting for the drag threshold to be crossed
                scrollData.dragDistance = dragMagnitude > 0 ? dragPercentage : 0f; // Set value representing how much of the pre-scroll drag amount has occurred
                //this.Pulse(node, m_ActivationPulse, repeatingPulseAmount, repeatingPulseAmount);
                if (dragMagnitude > newDirectionVectorThreshold)
                    scrollData.spatialDirection = directionVector; // Initialize vector defining the spatial scroll direction
            }
            else
            {
                var spatialDirection = scrollData.spatialDirection.Value;
                var scrollingAfterTriggerOirigin = Vector3.Dot(directionVector, spatialDirection) >= 0; // Detect that the user is scrolling forward from the trigger origin point
                var projectionVector = scrollingAfterTriggerOirigin ? spatialDirection : spatialDirection + spatialDirection;
                var projectedAmount = Vector3.Project(directionVector, projectionVector).magnitude / this.GetViewerScale();

                // Mandate that scrolling maintain the initial direction, regardless of the user scrolling before/after the trigger origin point; prevent direction flipping
                projectedAmount = scrollingAfterTriggerOirigin ? projectedAmount : 1 - projectedAmount;
                scrollData.normalizedLoopingPositionUnconstrained = (Mathf.Abs(projectedAmount * (maxItemCount / scrollableItemCount)) % scrollLengthRange) * (1 / scrollLengthRange);
            }

            return scrollData;
        }

        //TODO: remove after refactor
        internal void EndScroll(IProcessSpatialInput caller)
        {
            if (m_SpatialScrollCallers.Count == 0)
                return;

            foreach (var scroller in m_SpatialScrollCallers)
            {
                if (scroller == caller)
                {
                    this.RemoveRayVisibilitySettings(caller.spatialScrollData.rayOrigin, caller);
                    this.SetSpatialHintState(SpatialHintModule.SpatialHintStateFlags.Hidden);
                    caller.spatialScrollData = null; // clear reference to the previously used scrollData
                    m_SpatialScrollCallers.Remove(caller);
                    return;
                }
            }
        }
    }
}
#endif
