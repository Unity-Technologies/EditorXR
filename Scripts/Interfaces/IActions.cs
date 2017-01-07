using System.Collections.Generic;
using UnityEngine.Experimental.EditorVR.Actions;

namespace UnityEngine.Experimental.EditorVR.Tools
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
