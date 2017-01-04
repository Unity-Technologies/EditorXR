using System;

namespace UnityEngine.Experimental.EditorVR
{
	/// <summary>
	/// Given a hovered object, test whether the selection will succeed
	/// </summary>
	/// <param name="hoveredObject">The hovered object that is being tested for selection</param>
	/// <param name="useGroupRoot">Whether the selection will be making use of the group root</param>
	/// <returns>Returns whether the selection will succeed</returns>
	public delegate bool CanSelectObjectDelegate(GameObject hoveredObject, bool useGroupRoot = false);

	/// <summary>
	/// Select the given object using the given rayOrigin
	/// </summary>
	/// <param name="hoveredObject">The hovered object</param>
	/// <param name="rayOrigin">The rayOrigin used for selection</param>
	/// <param name="multiSelect">Whether to add the hovered object to the selection, or override the current selection</param>
	/// <param name="useGroupRoot">Whether the selection will be making use of the group root</param>
	public delegate void SelectObjectDelegate(GameObject hoveredObject, Transform rayOrigin, bool multiSelect, bool useGroupRoot = false);

	/// <summary>
	/// Gives access to the selection module
	/// </summary>
	public interface ISelectObject
	{
		/// <summary>
		/// Given a hovered object, test whether the selection will succeed
		/// </summary>
		CanSelectObjectDelegate canSelectObject { set; }

		/// <summary>
		/// Get the group to which the GameObject belongs
		/// GameObject: the hovered object
		/// Returns the group root
		/// </summary>
		Func<GameObject, GameObject> getGroupRoot { set; }

		/// <summary>
		/// Select the given object using the given rayOrigin
		/// </summary>
		SelectObjectDelegate selectObject { set; }
	}
}