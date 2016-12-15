using System.Collections.Generic;
using UnityEngine.VR.Actions;

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
