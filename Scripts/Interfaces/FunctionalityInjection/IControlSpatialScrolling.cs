#if UNITY_EDITOR
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Gives decorated class ability to control/perform spatial-scrolling
	/// </summary>
	public interface IControlSpatialScrolling
	{
	}

	public static class IControlSpatialScrollingMethods
	{
		internal delegate SpatialScrollModule.SpatialScrollData PerformSpatialScrollDelegate (object caller, Node? node, Vector3 startingPosition, Vector3 currentPosition, float repeatingScrollLengthRange, int scrollableItemCount, int maxItemCount = -1);
		internal static PerformSpatialScrollDelegate performSpatialScroll { private get; set; }

		/// <summary>
		/// 
		/// </summary>
		public static SpatialScrollModule.SpatialScrollData PerformSpatialScroll(this IControlSpatialHinting obj, object caller, Node? node, Vector3 startingPosition, Vector3 currentPosition, float repeatingScrollLengthRange, int scrollableItemCount, int maxItemCount = -1)
		{
			return performSpatialScroll(caller, node, startingPosition, currentPosition, repeatingScrollLengthRange, scrollableItemCount, maxItemCount);
		}

		internal delegate void EndSpatialScrollDelegate (object caller);
		internal static EndSpatialScrollDelegate endSpatialScroll { private get; set; }

		/// <summary>
		/// 
		/// </summary>
		public static void EndSpatialScroll(this IControlSpatialHinting obj, object caller)
		{
			endSpatialScroll(caller);
		}
	}
}
#endif
