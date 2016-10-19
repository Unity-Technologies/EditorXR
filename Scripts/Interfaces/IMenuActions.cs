using System;
using System.Collections.Generic;
using UnityEngine.VR.Actions;

public interface IMenuActions
{
	/// <summary>
	/// Collection of actions that can be performed
	/// </summary>
	List<ActionMenuData> menuActions { set; }
}
