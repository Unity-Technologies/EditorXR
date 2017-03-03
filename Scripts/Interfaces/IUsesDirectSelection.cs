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
		Func<Dictionary<Transform, DirectSelectionData>> getDirectSelection { set; }
	}
}
#endif
