#if UNITY_EDITOR
using System.Collections.Generic;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Implementors receive a list of asset types found in the project
	/// </summary>
	public interface IFilterUI
	{
		/// <summary>
		/// Set accessor for the filter list
		/// </summary>
		List<string> filterList { set; }
	}
}
#endif
