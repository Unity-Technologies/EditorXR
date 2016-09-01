using System;
using System.Collections.Generic;
using UnityEngine.InputNew;

namespace UnityEngine.VR.Tools
{
	public interface IMainMenu : IMenuOrigins
	{
		/// <summary>
		/// The camera that handles UI events in the menu
		/// </summary>
		Camera eventCamera { get; set; }

		/// <summary>
		/// The action map input used to drive the menu
		/// </summary>
		MainMenuInput mainMenuInput { get; set; }

		/// <summary>
		/// The menu tools that will populate the menu
		/// </summary>
		List<Type> menuTools { set; }

		/// <summary>
		/// The actions that will populate the menu
		/// </summary>
		List<IAction> menuActions { set; }

		/// <summary>
		/// Delegate used to test for selecting items in the Main Menu, and setting them on the other hand's device
		/// </summary>
		Func<int, Type, bool> selectTool { set; }

		/// <summary>
		/// Delegate used to perform actions
		/// </summary>
		Func<IAction, bool> performAction { set; }

		/// <summary>
		/// The device tag index that the menu is set on
		/// </summary>
		int tagIndex { get; set; }
	}
}