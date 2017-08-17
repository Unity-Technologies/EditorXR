#if UNITY_EDITOR
using System;
using UnityEditor.Experimental.EditorVR.Helpers;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Gives decorated class Pinned Tools Menu functionality
	/// </summary>
	public interface IPinnedToolsMenu : IUsesMenuOrigins, ICustomActionMap, IUsesNode, ISelectTool
	{
		bool alternateMenuVisible { set; }

		/// <summary>
		/// This menu's RayOrigin
		/// </summary>
		Transform rayOrigin { get; set; }

		/// <summary>
		/// The PinnedToolButton that the menu uses to display tool previews
		/// </summary>
		IPinnedToolButton previewToolButton { get; }

		/// <summary>
		/// Function that assigns & sets up a tool button for a given tool type
		/// This method isn't hooked up in EVR, it should reside in the implementing class
		/// </summary>
		Action<Type, Sprite> SetButtonForType { get; }

		/// <summary>
		/// Delete the tool button with corresponding type of the first parameter.
		/// Then, select the tool button with corresponds to the type of the second parameter.
		/// </summary>
		Action<Type, Type> deletePinnedToolButton { get; }
	}

	public static class IPinnedToolsMenuMethods
	{
		public static Action<Transform, GradientPair> highlightDevice { get; set; }

		/// <summary>
		/// Visually highlights an input device
		/// </summary>
		/// <param name="rayOrigin">This menu's RayOrigin</param>
		/// <param name="gradientPair">The gradient pair used in the highlight visuals</param>
		public static void HighlightDevice(this IPinnedToolsMenu obj, Transform rayOrigin, GradientPair gradientPair)
		{
			highlightDevice(rayOrigin, gradientPair);
		}

		public static Action<Transform> mainMenuActivatorSelected { get; set; }

		/// <summary>
		/// Called when selecting the main menu activator
		/// </summary>
		/// <param name="rayOrigin">This menu's RayOrigin</param>
		public static void MainMenuActivatorSelected(this IPinnedToolsMenu obj, Transform rayOrigin)
		{
			mainMenuActivatorSelected(rayOrigin);
		}

		public static Action<Transform, Type> selectTool { get; set; }

		/// <summary>
		/// Selects a tool, based on type, from a pinned tool button
		/// </summary>
		/// <param name="rayOrigin">This menu's RayOrigin</param>
		/// <param name="type">The type of the tool that is to be selected</param>
		public static void SelectTool(this IPinnedToolsMenu obj, Transform rayOrigin, Type type)
		{
			selectTool(rayOrigin, type);
		}
	}
}
#endif
