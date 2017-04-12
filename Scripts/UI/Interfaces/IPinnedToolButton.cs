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
		int menuButtonOrderPosition { get; }
		int activeToolOrderPosition  { get; }
		int order { get; set; }
		int activeButtonCount { get; set; }
		bool highlighted { set; }
		bool moveToAlternatePosition { set; }
		Type toolType { get; set; }
		Type previewToolType { get; set; }
		Vector3 toolButtonActivePosition { get; } // Shared active button offset from the alternate menu
		GradientPair gradientPair { set; }

		event Action<Transform> hoverEnter;
		event Action<Transform> hoverExit;
		event Action<Transform> selected;
	}

	public static class IPinnedToolButtonMethods
	{
	}
}
#endif
