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
		Transform rayOrigin { get; set; }
		IPinnedToolButton previewToolButton { get; }
		// This method isn't hooked up in EVR, it should reside in the implementing class
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
		/// 
		/// </summary>
		/// <param name="rayOrigin"></param>
		/// <param name="gradientPair"></param>
		public static void HighlightDevice(this IPinnedToolsMenu obj, Transform rayOrigin, GradientPair gradientPair)
		{
			highlightDevice(rayOrigin, gradientPair);
		}

		public static Action<Transform> mainMenuActivatorSelected { get; set; }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="rayOrigin"></param>
		public static void MainMenuActivatorSelected(this IPinnedToolsMenu obj, Transform rayOrigin)
		{
			mainMenuActivatorSelected(rayOrigin);
		}

		public static Action<Transform, Type> selectTool { get; set; }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="rayOrigin"></param>
		/// <param name="type"></param>
		public static void SelectTool(this IPinnedToolsMenu obj, Transform rayOrigin, Type type)
		{
			selectTool(rayOrigin, type);
		}
	}
}
#endif
