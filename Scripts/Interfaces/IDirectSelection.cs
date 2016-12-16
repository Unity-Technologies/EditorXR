using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.EditorVR.Modules
{
	/// <summary>
	/// Gives decorated class access to direct selections
	/// </summary>
	public interface IDirectSelection
	{
		/// <summary>
		/// ConnectInterfaces provides a delegate which can be called to get a dictionary of the current direct selection
		/// Key is the rayOrigin used to select the object
		/// Value is a data class containing the selected object and metadata
		/// </summary>
		Func<Dictionary<Transform, DirectSelectionData>> getDirectSelection { set; }
	}
}