#if UNITY_EDITOR
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Given a hovered object, find what object would actually be selected
	/// </summary>
	/// <param name="hoveredObject">The hovered object that is being tested for selection</param>
	/// <param name="useGrouping">Use group selection</param>
	/// <returns>Returns what object would be selected by selectObject</returns>
	public delegate GameObject GetSelectionCandidateDelegate(GameObject hoveredObject, bool useGrouping = false);

	/// <summary>
	/// Select the given object using the given rayOrigin
	/// </summary>
	/// <param name="hoveredObject">The hovered object</param>
	/// <param name="rayOrigin">The rayOrigin used for selection</param>
	/// <param name="multiSelect">Whether to add the hovered object to the selection, or override the current selection</param>
	/// <param name="useGrouping">Use group selection</param>
	public delegate void SelectObjectDelegate(GameObject hoveredObject, Transform rayOrigin, bool multiSelect, bool useGrouping = false);

	/// <summary>
	/// Gives access to the selection module
	/// </summary>
	public interface ISelectObject
	{
		/// <summary>
		/// Given a hovered object, test whether the selection will succeed
		/// </summary>
		GetSelectionCandidateDelegate getSelectionCandidate { set; }

		/// <summary>
		/// Select the given object using the given rayOrigin
		/// </summary>
		SelectObjectDelegate selectObject { set; }
	}
}
#endif
