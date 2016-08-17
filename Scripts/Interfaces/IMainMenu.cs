using System;
using System.Collections.Generic;
using UnityEngine.InputNew;

namespace UnityEngine.VR.Tools
{
	public interface IMainMenu
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
		/// The transform under which the menu input object should be parented, inheriting position, scale, and rotation
		/// </summary>
		Transform menuInputOrigin { get; set; }

		/// <summary>
		/// The transform under which the menu should be parented, inheriting position and rotation
		/// </summary>
		Transform menuOrigin { get; set; }

		/// <summary>
		/// The menu tools that will populate the menu
		/// </summary>
		List<Type> menuTools { set; }

		/// <summary>
		/// Delegate used to test for selecting items in the Main Menu
		/// </summary>
		Func<int, Type, bool> selectTool { set; }

		/// <summary>
		/// The device tag index that the menu is set on
		/// </summary>
		int tagIndex { get; set; }
	}
}