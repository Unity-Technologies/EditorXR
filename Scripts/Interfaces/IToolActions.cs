using System.Collections.Generic;
using UnityEngine.VR.Actions;

namespace UnityEngine.VR.Tools
{
	/// <summary>
	/// Declares that a class has tool actions that should be picked up by the system
	/// </summary>
	public interface IToolActions
	{
		/// <summary>
		/// Collection of actions that can be performed when the tool is selected
		/// </summary>
		List<IAction> toolActions { get; }
	}
}
