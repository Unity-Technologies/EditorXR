using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Actions;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Declares that a class has tool actions that should be picked up by the system
	/// </summary>
	public interface IActions
	{
		/// <summary>
		/// Collection of actions that the tool, module, etc. offers
		/// </summary>
		List<IAction> actions { get; }
	}
}
