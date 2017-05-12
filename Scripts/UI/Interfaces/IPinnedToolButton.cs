#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor.Experimental.EditorVR.Helpers;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Gives decorated class Pinned Tool Button functionality
	/// </summary>
	public interface IPinnedToolButton
	{
		Type previewToolType { set; }
		int order { get; set; }
		Type toolType { get; set; }
		bool highlighted { get; set; }
		bool secondaryButtonHighlighted { get; }
		int maxButtonCount { get; set; }
		Func<int> visibileButtonCount { get; set; }
		bool activeTool { get; set; }
		Action<Type> selectTool { get; set; }
		Func<bool> closeButton { get; set; }
		bool toolTipVisible { set; }
		Action destroy { get; }
		bool implementsSecondaryButton { get; set; }
		Action<IPinnedToolButton> showAllButtons { get; set; }
		Action hoverExit { get; set; }

		/*
		int menuButtonOrderPosition { get; }
		int activeToolOrderPosition  { get; }
		int activeButtonCount { set; }
		bool revealed { set; }
		bool moveToAlternatePosition { get; set; }
		Transform rayOrigin { get; set; }
		Vector3 toolButtonActivePosition { get; } // Shared active button offset from the alternate menu
		Sprite icon { set; }
		Node node { set; }
		GradientPair gradientPair { get; }

		event Action<Transform> hoverEnter;
		event Action<Transform> hoverExit;
		event Action<Transform> selected;

		Action<Transform, Type> selectTool { set; }
		Action<Transform, IPinnedToolButton> deletePinnedToolButton { set; }
		Action<Transform, bool> revealAllToolButtons { set; }
		Action<Transform, int, bool> dighlightSingleButton { set; }
		Action<Transform> selectHighlightedButton { set; }
		Action<Transform> deleteHighlightedButton { set; }
		*/
	}

	public static class IPinnedToolButtonMethods
	{
	}
}
#endif
