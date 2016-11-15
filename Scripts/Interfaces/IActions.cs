using System.Collections.Generic;
using UnityEngine.VR.Actions;

namespace UnityEngine.VR.Tools
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
