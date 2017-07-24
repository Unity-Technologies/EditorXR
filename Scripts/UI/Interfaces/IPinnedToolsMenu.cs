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
		bool moveToAlternatePosition { set; }
		Transform rayOrigin { get; set; }
		IPinnedToolButton previewToolButton { get; }
		Action<Transform, Type> selectTool { set; }
		Action<Transform, GradientPair> highlightDevice { get; set; }
		Action<Transform> mainMenuActivatorSelected { set; }
		Action<Type, Sprite, Node> createPinnedToolButton { get; set; }
	}

	public static class IPinnedToolsMenuMethods
	{
		//public static void CreatePinnedToolButton(Type toolType, Sprite buttonIcon, Node node)
	}
}
#endif
