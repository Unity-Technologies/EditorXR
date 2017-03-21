#if UNITY_EDITOR
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Method signature highlighting objects
	/// </summary>
	/// <param name="go">The object to highlight</param>
	/// <param name="active">Whether to add or remove the highlight</param>
	/// <param name="rayOrigin">RayOrigin that hovered over the object (optional)</param>
	/// <param name="material">Custom material to use for this object</param>
	public delegate void SetHighlightDelegate(GameObject go, bool active, Transform rayOrigin = null, Material material = null);

	/// <summary>
	/// Gives decorated class ability to highlight a given GameObject
	/// </summary>
	public interface ISetHighlight
	{
		/// <summary>
		/// Method provided by the system to add or remove a highlight on an object
		/// </summary>
		SetHighlightDelegate setHighlight { set; }
	}
}
#endif
