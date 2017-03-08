#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Gives decorated class access to direct selections
	/// </summary>
	public interface IUsesDirectSelection
	{
		/// <summary>
		/// ConnectInterfaces provides a delegate which can be called to get a dictionary of the current direct selection
		/// Key is the rayOrigin used to select the object
		/// Value is a data class containing the selected object and metadata
		/// </summary>
		//Func<Dictionary<Transform, DirectSelectionData>> GetDirectSelection { set; }
	}

	public static class IUsesDirectSelectionMethods
	{
		internal delegate Dictionary<Transform, DirectSelectionData> GetDirectSelectionDelegate();

		internal static GetDirectSelectionDelegate s_GetDirectSelection;

		public static Dictionary<Transform, DirectSelectionData> GetDirectSelection(this IUsesDirectSelection obj)
		{
			if (s_GetDirectSelection != null)
				return s_GetDirectSelection();

			return null;
		}

	}
}
#endif
