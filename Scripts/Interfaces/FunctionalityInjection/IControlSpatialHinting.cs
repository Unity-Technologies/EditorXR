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
		/// Set the spatial hint state
		/// </summary>
		/// <param name="state">SpatialHintState to set</param>
		public static void SetSpatialHintState(this IControlSpatialHinting obj, SpatialHintModule.SpatialHintStateFlags state)
		{
			setSpatialHintState(state);
		}

		internal delegate void SetSpatialHintPositionDelegate(Vector3 position);
		internal static SetSpatialHintPositionDelegate setSpatialHintPosition { get; set; }

		/// <summary>
		/// Set the position of the spatial hint visuals
		/// </summary>
		/// <param name="position">The position at which the spatial hint visuals should be displayed</param>
		public static void SetSpatialHintPosition(this IControlSpatialHinting obj, Vector3 position)
		{
			setSpatialHintPosition(position);
		}

		internal delegate void SetSpatialHintRotationDelegate(Quaternion rotation);
		internal static SetSpatialHintRotationDelegate setSpatialHintContainerRotation { get; set; }

		/// <summary>
		/// Set the rotation of the spatial hint visuals container game object
		/// </summary>
		/// <param name="rotation">The rotation to set on the spatial visuals</param>
		public static void SetSpatialHintContainerRotation(this IControlSpatialHinting obj, Quaternion rotation)
		{
			setSpatialHintContainerRotation(rotation);
		}

		internal delegate void SetSpatialHintRotationTargetDelegate(Vector3 target);
		internal static SetSpatialHintRotationTargetDelegate setSpatialHintShowHideRotationTarget { get; set; }

		/// <summary>
		/// Sets the target for the spatial hint visuals to look at while performing an animated show or hide
		/// </summary>
		/// <param name="target">The position to target</param>
		public static void SetSpatialHintShowHideRotationTarget(this IControlSpatialHinting obj, Vector3 target)
		{
			setSpatialHintShowHideRotationTarget(target);
		}

		internal delegate void SetSpatialHintLookATRotationDelegate(Vector3 position);
		internal static SetSpatialHintLookATRotationDelegate setSpatialHintLookAtRotation { get; set; }

		/// <summary>
		/// Set the LookAt target
		/// </summary>
		/// <param name="position">The position the visuals should look at</param>
		public static void SetSpatialHintLookAtRotation(this IControlSpatialHinting obj, Vector3 position)
		{
			setSpatialHintLookAtRotation(position);
		}

		internal delegate void PulseSpatialHintScrollArrowsDelegate();
		internal static PulseSpatialHintScrollArrowsDelegate pulseSpatialHintScrollArrows { get; set; }

		/// <summary>
		/// Visually pulse the spatial-scroll arrows; the arrows shown when performing a spatial scroll
		/// </summary>
		public static void PulseSpatialHintScrollArrows(this IControlSpatialHinting obj)
		{
			pulseSpatialHintScrollArrows();
		}

		internal delegate void SetSpatialHintDragThresholdTriggerPositionDelegate(Vector3 position);
		internal static SetSpatialHintDragThresholdTriggerPositionDelegate setSpatialHintDragThresholdTriggerPosition { get; set; }

		/// <summary>
		/// Set the magnitude at which the user will trigger spatial scrolling
		/// </summary>
		/// <param name="position">The position, whose magnitude from the origin will be used to detect an initiation of spatial scrolling</param>
		public static void SetSpatialHintDragThresholdTriggerPosition(this IControlSpatialHinting obj, Vector3 position)
		{
			setSpatialHintDragThresholdTriggerPosition(position);
		}

		internal delegate void SetSpatialHintControlObjectDelegate(Transform controlObject);
		internal static SetSpatialHintControlObjectDelegate setSpatialHintControlObject { get; set; }

		/// <summary>
		/// Set reference to the object, RayOrigin, controlling the Spatial Hint visuals
		/// Each control-object has it's spatial scrolling processed independently
		/// </summary>
		/// <param name="controlObject">Control-object whose spatial scrolling will be processed independently</param>
		public static void SetSpatialHintControlObject(this IControlSpatialHinting obj, Transform controlObject)
		{
			setSpatialHintControlObject(controlObject);
		}
	}
}
#endif
