#if UNITY_EDITOR
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Gives decorated class ability to control spatial-hinting visuals
	/// </summary>
	public interface IControlSpatialHinting
	{
	}

	public static class IControlSpatialHintingMethods
	{
		internal delegate void SetSpatialHintStateDelegate(SpatialHintModule.SpatialHintStateFlags state);
		internal static SetSpatialHintStateDelegate setSpatialHintState { get; set; }

		/// <summary>
		/// 
		/// </summary>
		public static void SetSpatialHintState(this IControlSpatialHinting obj, SpatialHintModule.SpatialHintStateFlags state)
		{
			setSpatialHintState(state);
		}

		internal delegate void SetSpatialHintPositionDelegate(Vector3 position);
		internal static SetSpatialHintPositionDelegate setSpatialHintPosition { get; set; }

		/// <summary>
		/// 
		/// </summary>
		public static void SetSpatialHintPosition(this IControlSpatialHinting obj, Vector3 position)
		{
			setSpatialHintPosition(position);
		}

		internal delegate void SetSpatialHintRotationDelegate(Quaternion rotation);
		internal static SetSpatialHintRotationDelegate setSpatialHintRotation { get; set; }

		/// <summary>
		/// 
		/// </summary>
		public static void SetSpatialHintRotation(this IControlSpatialHinting obj, Quaternion rotation)
		{
			setSpatialHintRotation(rotation);
		}

		internal delegate void SetSpatialHintRotationTargetDelegate(Vector3 target);
		internal static SetSpatialHintRotationTargetDelegate setSpatialHintRotationTarget { get; set; }

		/// <summary>
		/// 
		/// </summary>
		public static void SetSpatialHintRotationTarget(this IControlSpatialHinting obj, Vector3 target)
		{
			setSpatialHintRotationTarget(target);
		}

		internal delegate void SetSpatialHintLookATRotationDelegate(Vector3 position);
		internal static SetSpatialHintLookATRotationDelegate setSpatialHintLookAtRotation { get; set; }

		/// <summary>
		/// 
		/// </summary>
		public static void SetSpatialHintLookATRotation(this IControlSpatialHinting obj, Vector3 position)
		{
			setSpatialHintLookAtRotation(position);
		}

		internal delegate void PulseSpatialHintScrollArrowsDelegate();
		internal static PulseSpatialHintScrollArrowsDelegate pulseSpatialHintScrollArrows { get; set; }

		/// <summary>
		/// Visually pulse the spatial-scroll arrows; the arrows shown when performing a spatiatil scroll
		/// </summary>
		public static void PulseSpatialHintScrollArrows(this IControlSpatialHinting obj)
		{
			pulseSpatialHintScrollArrows();
		}

		internal delegate void SetSpatialHintDragThresholdTriggerPositionDelegate(Vector3 position);
		internal static SetSpatialHintDragThresholdTriggerPositionDelegate setSpatialHintDragThresholdTriggerPosition { get; set; }

		/// <summary>
		/// 
		/// </summary>
		public static void SetSpatialHintDragThresholdTriggerPosition(this IControlSpatialHinting obj, Vector3 position)
		{
			setSpatialHintDragThresholdTriggerPosition(position);
		}

		internal delegate void SetSpatialHintControlObjectDelegate(Transform controlObject);
		internal static SetSpatialHintControlObjectDelegate setSpatialHintControlObject { get; set; }

		/// <summary>
		/// Set reference to the object, RayOrigin, controlling the Spatial Hint visuals
		/// </summary>
		public static void SetSpatialHintControlObject(this IControlSpatialHinting obj, Transform controlObject)
		{
			setSpatialHintControlObject(controlObject);
		}
	}
}
#endif
