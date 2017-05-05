#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.EditorVR.Helpers;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Gives decorated class Pinned Tools Menu functionality
	/// </summary>
	public interface IPinnedToolsMenu : IUsesMenuOrigins, ICustomActionMap
	{
		Dictionary<Type, IPinnedToolButton> pinnedToolButtons { set; }
		Dictionary<Type, Sprite> icons { set; }
		int activeToolOrderPosition  { get; }
		//int activeButtonCount { set; }
		bool revealed { set; }
		bool moveToAlternatePosition { get; set; }
		Transform rayOrigin { get; set; }
		Type previewToolType { set; }
		Vector3 alternateMenuItem { get; } // Shared active button offset from the alternate menu
		Node node { set; } // Used for button and tooltip alignment

		event Action<Transform> hoverEnter;
		event Action<Transform> hoverExit;
		event Action<Transform> selected;

		Action<Transform, Type> selectTool { set; }
		//Action<Transform, IPinnedToolButton> deletePinnedToolButton { set; }
		//Action<Transform, bool> revealAllToolButtons { set; }
		Action<Transform, int, bool> HighlightSingleButton { set; }
		Action<Transform> SelectHighlightedButton { set; }
		Action<Transform> deleteHighlightedButton { set; }
		Action<Transform> onButtonHoverEnter { get; set; }
		Action<Transform> onButtonHoverExit { get; set; }
		Action<Transform, GradientPair> highlightDevice { get; set; }

		// CONVERT INTO METHODS
		Action<Type, Sprite, Node> createPinnedToolButton { get; set; }
	}

	public static class IPinnedToolsMenuMethods
	{
		//public static void CreatePinnedToolButton(Type toolType, Sprite buttonIcon, Node node)
	}
}
#endif
