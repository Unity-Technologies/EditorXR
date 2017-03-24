#if UNITY_EDITOR
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Gives access to the selection module
	/// </summary>
	public interface ISelectObject
	{
	}

	public static class ISelectObjectMethods
	{
		internal delegate GameObject GetSelectionCandidateDelegate(GameObject hoveredObject, bool useGrouping = false);
		internal delegate void SelectObjectDelegate(GameObject hoveredObject, Transform rayOrigin, bool multiSelect, bool useGrouping = false);

		internal static GetSelectionCandidateDelegate getSelectionCandidate { get; set; }
		internal static SelectObjectDelegate selectObject { get; set; }

		/// <summary>
		/// Given a hovered object, find what object would actually be selected
		/// </summary>
		/// <param name="hoveredObject">The hovered object that is being tested for selection</param>
		/// <param name="useGrouping">Use group selection</param>
		/// <returns>Returns what object would be selected by selectObject</returns>
		public static GameObject GetSelectionCandidate(this ISelectObject obj, GameObject hoveredObject, bool useGrouping = false)
		{
			return getSelectionCandidate(hoveredObject, useGrouping);
		}

		/// <summary>
		/// Select the given object using the given rayOrigin
		/// </summary>
		/// <param name="hoveredObject">The hovered object</param>
		/// <param name="rayOrigin">The rayOrigin used for selection</param>
		/// <param name="multiSelect">Whether to add the hovered object to the selection, or override the current selection</param>
		/// <param name="useGrouping">Use group selection</param>
		public static void SelectObject(this ISelectObject obj, GameObject hoveredObject, Transform rayOrigin, bool multiSelect, bool useGrouping = false)
		{
			selectObject(hoveredObject, rayOrigin, multiSelect, useGrouping);
		}
	}
}
#endif
