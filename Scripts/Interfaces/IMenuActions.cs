using System;
using System.Collections.Generic;
using UnityEngine.VR.Actions;

public interface IMenuActions
{
	/// <summary>
	/// Collection of actions that can be performed
	/// </summary>
	List<IAction> menuActions { set; }

	/// <summary>
	/// The delegate that performs an action
	/// </summary>
	Func<IAction, bool> performAction { set; }
}
