#if UNITY_EDITOR
using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Gives decorated class Pinned Tool Button functionality
	/// </summary>
	public interface IPinnedToolButton
	{
		Type previewToolType { set; }
		Type toolType { get; set; }
		int order { get; set; }
		int maxButtonCount { get; set; }
		float iconHighlightedLocalZOffset { set; }
		bool highlighted { get; set; }
		bool secondaryButtonHighlighted { get; }
		bool isActiveTool { get; set; }
		bool toolTipVisible { set; }
		bool implementsSecondaryButton { get; set; }
		Vector3 primaryUIContentContainerLocalScale { get; set; }
		Transform tooltipTarget { get; set; }
		string previewToolDescription { set; }
		Action destroy { get; }
		Action<Type> selectTool { set; }
		Func<int> visibileButtonCount { set; }
		Func<bool> closeButton { set; }
		Action<IPinnedToolButton> showAllButtons { set; }
		Action hoverExit { set; }
		event Action hovered;
	}

	public static class IPinnedToolButtonMethods
	{
	}
}
#endif
