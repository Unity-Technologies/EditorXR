#if UNITY_EDITOR
using System;
using UnityEditor.Experimental.EditorVR.Helpers;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Gives decorated class Pinned Tools Menu functionality
	/// </summary>
	public interface IPinnedToolsMenu : IUsesMenuOrigins, ICustomActionMap, IUsesNode
	{
		bool alternateMenuVisible { set; }
		Transform rayOrigin { get; set; }
		IPinnedToolButton previewToolButton { get; }
		Action<Transform, Type> selectTool { set; }
		Action<Transform> mainMenuActivatorSelected { set; }
		Action<Type, Sprite, Node> createPinnedToolButton { get; set; }
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
	}
}
#endif
