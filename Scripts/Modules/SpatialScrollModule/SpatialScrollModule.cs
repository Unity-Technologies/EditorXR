using System.Collections.Generic;
using Unity.Labs.ModuleLoader;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
    public sealed class SpatialScrollModule : MonoBehaviour, IModule, IUsesViewerScale, IControlHaptics,
        IControlSpatialHinting, IRayVisibilitySettings, INodeToRay
    {
        public class SpatialScrollData : INodeToRay
        {
            public SpatialScrollData(IControlSpatialScrolling caller, Node node, Vector3 startingPosition, Vector3 currentPosition, float repeatingScrollLengthRange, int scrollableItemCount, int maxItemCount = -1, bool centerVisuals = true)
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
            public IControlSpatialScrolling caller { get; set; }

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
            public bool passedMinDragActivationThreshold
            {
                get { return spatialDirection != null; }
            }

            public void UpdateExistingScrollData(Vector3 newPosition)
            {
                currentPosition = newPosition;
            }
        }

#pragma warning disable 649
        [SerializeField]
        HapticPulse m_ActivationPulse; // The pulse performed when initial activating spatial selection
#pragma warning restore 649

        // Collection housing objects whose scroll data is being processed
        readonly List<IControlSpatialScrolling> m_ScrollCallers = new List<IControlSpatialScrolling>();

        internal SpatialScrollData PerformScroll(IControlSpatialScrolling caller, Node node, Vector3 startingPosition, Vector3 currentPosition, float repeatingScrollLengthRange, int scrollableItemCount, int maxItemCount = -1, bool centerScrollVisuals = true)
        {
            // Continue processing of spatial scrolling for a given caller,
            // Or create new instance of scroll data for new callers. (Initial structure for support of simultaneous callers)
            SpatialScrollData scrollData = null;
            foreach (var scroller in m_ScrollCallers)
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
                scrollData = new SpatialScrollData(caller, node, startingPosition, currentPosition, repeatingScrollLengthRange, scrollableItemCount, maxItemCount, centerScrollVisuals);
                m_ScrollCallers.Add(caller);
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
                this.Pulse(node, m_ActivationPulse, repeatingPulseAmount, repeatingPulseAmount);
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
                scrollData.normalizedLoopingPosition = (Mathf.Abs(projectedAmount * (maxItemCount / scrollableItemCount)) % repeatingScrollLengthRange) * (1 / repeatingScrollLengthRange);
            }

            return scrollData;
        }

        internal void EndScroll(IControlSpatialScrolling caller)
        {
            if (m_ScrollCallers.Count == 0)
                return;

            foreach (var scroller in m_ScrollCallers)
            {
                if (scroller == caller)
                {
                    this.RemoveRayVisibilitySettings(caller.spatialScrollData.rayOrigin, caller);
                    this.SetSpatialHintState(SpatialHintModule.SpatialHintStateFlags.Hidden);
                    caller.spatialScrollData = null; // clear reference to the previously used scrollData
                    m_ScrollCallers.Remove(caller);
                    return;
                }
            }
        }

        public void LoadModule() { }

        public void UnloadModule() { }
    }
}
