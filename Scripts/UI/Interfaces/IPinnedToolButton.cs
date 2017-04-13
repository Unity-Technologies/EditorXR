#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor.Experimental.EditorVR.Helpers;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Gives decorated class Pinned Tool Button functionality
	/// </summary>
	public interface IPinnedToolButton : IUsesMenuOrigins
	{
		int menuButtonOrderPosition { get; }
		int activeToolOrderPosition  { get; }
		int order { get; set; }
		int activeButtonCount { set; }
		bool highlighted { set; }
		bool moveToAlternatePosition { get; set; }
		Transform rayOrigin { get; set; }
		Type toolType { get; set; }
		Type previewToolType { set; }
		Vector3 toolButtonActivePosition { get; } // Shared active button offset from the alternate menu
		Node node { set; }
		GradientPair gradientPair { get; }

		event Action<Transform> hoverEnter;
		event Action<Transform> hoverExit;
		event Action<Transform> selected;

		Action<Transform, Type> selectTool { set; }
		Action<Transform, IPinnedToolButton> deletePinnedToolButton { set; }
		Action<Transform, bool> highlightAllToolButtons { set; }
	}

	public static class IPinnedToolButtonMethods
	{
	}
}
#endif
