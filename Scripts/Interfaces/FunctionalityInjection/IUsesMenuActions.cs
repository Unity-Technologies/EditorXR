#if UNITY_EDITOR
using System.Collections.Generic;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Decorates a class that wants to receive menu actions
	/// </summary>
	public interface IUsesMenuActions
	{
		/// <summary>
		/// Collection of actions that can be performed
		/// </summary>
		List<ActionMenuData> menuActions { set; }
	}
}
#endif
