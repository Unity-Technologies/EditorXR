#if UNITY_EDITOR
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Gives decorated class ability to highlight a given GameObject
	/// </summary>
	public interface ISetHighlight
	{
	}

	public static class ISetHighlightMethods
	{
		internal delegate void SetHighlightDelegate(GameObject go, bool active, Transform rayOrigin = null, Material material = null, bool force = false);

		internal static SetHighlightDelegate setHighlight { get; set; }

		/// <summary>
		/// Method for highlighting objects
		/// </summary>
		/// <param name="go">The object to highlight</param>
		/// <param name="active">Whether to add or remove the highlight</param>
		/// <param name="rayOrigin">RayOrigin that hovered over the object (optional)</param>
		/// <param name="material">Custom material to use for this object</param>
		/// <param name="force">Force the setting or unsetting of the highlight</param>
		public static void SetHighlight(this ISetHighlight obj, GameObject go, bool active, Transform rayOrigin = null, Material material = null, bool force = false)
		{
			setHighlight(go, active, rayOrigin, material, force);
		}
	}
}
#endif
