#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor.Experimental.EditorVR.Helpers;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Gives decorated class spatial scrolling functionality
	/// </summary>
	public interface ISpatialScrolling : IUsesMenuOrigins
	{
		/*
		int menuButtonOrderPosition { get; }
		int activeToolOrderPosition  { get; }
		int order { get; set; }
		int activeButtonCount { set; }
		bool revealed { set; }
		bool moveToAlternatePosition { get; set; }
		Transform rayOrigin { get; set; }
		Type toolType { get; set; }
		Type previewToolType { set; }
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
		*/

		Vector3 startingPosition { set; }
		Vector3 currentPosition { set; }

		// start pos, current pos, world length before repeat, returns normalized repeating projection position
		//Func<Vector3, Vector3, float>
	}

	public static class ISpatialScrollingMethods
	{
	}
}
#endif
