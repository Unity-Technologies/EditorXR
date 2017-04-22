#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Gives decorated class access to direct selections
	/// </summary>
	public interface IUsesDirectSelection
	{
	}

	public static class IUsesDirectSelectionMethods
	{
		internal delegate Dictionary<Transform, DirectSelectionData> GetDirectSelectionDelegate();

		internal static GetDirectSelectionDelegate getDirectSelection { get; set; }

		/// <summary>
		/// Returns a dictionary of direct selections
		/// </summary>
		/// <returns>Dictionary (K,V) where K = rayOrigin used to select the object and V = info about the direct selection</returns>
		public static Dictionary<Transform, DirectSelectionData> GetDirectSelection(this IUsesDirectSelection obj)
		{
			return getDirectSelection();
		}
	}
}
#endif
