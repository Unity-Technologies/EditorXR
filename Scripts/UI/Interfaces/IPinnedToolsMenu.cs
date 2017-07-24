#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Helpers;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Gives decorated class Pinned Tools Menu functionality
	/// </summary>
	public interface IPinnedToolsMenu : IUsesMenuOrigins, ICustomActionMap, IUsesNode
	{
		Dictionary<Type, Sprite> icons { set; }
		int activeToolOrderPosition  { get; }
		//int activeButtonCount { set; }
		bool revealed { set; }
		bool moveToAlternatePosition { set; }
		Transform rayOrigin { get; set; }
		Vector3 alternateMenuItem { get; } // Shared active button offset from the alternate menu
		IPinnedToolButton previewToolButton { get; }

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
		Action<Transform> mainMenuActivatorSelected { set; }

		// CONVERT INTO METHODS
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
