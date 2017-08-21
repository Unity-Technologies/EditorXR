#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
	public sealed class SpatialScrollModule : MonoBehaviour, IUsesViewerScale, IControlHaptics
	{
		[SerializeField]
		HapticPulse m_ActivationPulse; // The pulse performed when initial activating spatial selection

		// Collection housing caller object & ScrollData pairs
		Dictionary<object, SpatialScrollData> m_ScrollData;

		public class SpatialScrollData
		{
			public SpatialScrollData(object caller, Node? node, Vector3 startingPosition, Vector3 currentPosition, float repeatingScrollLengthRange, int scrollableItemCount, int maxItemCount = -1, bool centerVisuals = true)
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
			}

			// Data assigned by calling object requesting spatial scroll processing
			public object caller { get; set; }
			public Node? node { get; set; }
			public Vector3 startingPosition { get; set; }
			public Vector3 currentPosition { get; set; }
			public float repeatingScrollLengthRange { get; set; }
			public int scrollableItemCount { get; set; }
			public int maxItemCount { get; set; }
			public bool centerVisuals { get; set; }

			// Values populated by scroll processing
			public Vector3? spatialDirection { get; set; }
			public Vector3 startingDragOrigin { get; set; }
			public Vector3 previousWorldPosition { get; set; }
			public float normalizedLoopingPosition { get; set; }
			public float dragDistance { get; set; }
			public bool passedMinDragActivationThreshold { get { return spatialDirection != null; } }

			public void UpdateExistingScrollData(Vector3 newPosition)
			{
				currentPosition = newPosition;
			}
		}

		void Awake()
		{
			m_ScrollData = new Dictionary<object, SpatialScrollData>();
		}

		internal SpatialScrollData PerformScroll(object caller, Node? node, Vector3 startingPosition, Vector3 currentPosition, float repeatingScrollLengthRange, int scrollableItemCount, int maxItemCount = -1, bool centerScrollVisuals = true)
		{
			// Continue processing of spatial scrolling for a given caller,
			// Or create new instance of scroll data for new callers. (Initial structure for support of simultaneous callers)
			SpatialScrollData spatialScrollData = null;
			foreach (var kvp in m_ScrollData)
			{
				if (kvp.Key == caller)
				{
					spatialScrollData = kvp.Value;
					spatialScrollData.UpdateExistingScrollData(currentPosition);
					break;
				}
			}

			if (spatialScrollData == null)
			{
				spatialScrollData = new SpatialScrollData(caller, node, startingPosition, currentPosition, repeatingScrollLengthRange, scrollableItemCount, maxItemCount, centerScrollVisuals);
				m_ScrollData.Add(caller, spatialScrollData);
			}

			return ProcessSpatialScrolling(spatialScrollData);
		}

		SpatialScrollData ProcessSpatialScrolling(SpatialScrollData scrollData)
		{
			var currentPosition = scrollData.currentPosition;
			var directionVector = currentPosition - scrollData.startingPosition;
			if (scrollData.spatialDirection == null)
			{
				var newDirectionVectorThreshold = 0.0175f; // Initial magnitude beyond which spatial scrolling will be evaluated
				newDirectionVectorThreshold *= this.GetViewerScale();
				var dragMagnitude = Vector3.Magnitude(directionVector);
				var dragPercentage = dragMagnitude / newDirectionVectorThreshold;
				var repeatingPulseAmount = Mathf.Sin(Time.realtimeSinceStartup * 20) > 0.5f ? 1f : 0f; // Perform an on/off repeating pulse while waiting for the drag threshold to be crossed
				scrollData.dragDistance = dragMagnitude > 0 ? dragPercentage : 0f; // Set normalized value representing how much of the pre-scroll drag amount has occurred
				this.Pulse(scrollData.node, m_ActivationPulse, repeatingPulseAmount, repeatingPulseAmount);
				if (dragMagnitude > newDirectionVectorThreshold)
					scrollData.spatialDirection = directionVector; // Initialize vector defining the spatial scroll direction
			}
			else
			{
				var projectedAmount = Vector3.Project(directionVector, scrollData.spatialDirection.Value).magnitude / this.GetViewerScale();
				scrollData.normalizedLoopingPosition = (Mathf.Abs(projectedAmount * (scrollData.maxItemCount / scrollData.scrollableItemCount)) % scrollData.repeatingScrollLengthRange) * (1 / scrollData.repeatingScrollLengthRange);
			}

			return scrollData;
		}

		internal void EndScroll(object caller)
		{
			if (m_ScrollData.Count == 0)
				return;

			foreach (var kvp in m_ScrollData)
			{
				if (kvp.Key == caller)
				{
					m_ScrollData.Remove(caller);
					return;
				}
			}
		}
	}
}
#endif
