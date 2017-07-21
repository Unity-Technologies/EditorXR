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
		bool activeTool { get; set; }
		bool toolTipVisible { set; }
		bool implementsSecondaryButton { get; set; }
		Vector3 primaryUIContentContainerLocalScale { get; set; }
		Transform tooltipTarget { get; set; }
		Action destroy { get; }
		Action<Type> selectTool { get; set; }
		Func<int> visibileButtonCount { get; set; }
		Func<bool> closeButton { get; set; }
		Action<IPinnedToolButton> showAllButtons { get; set; }
		Action hoverExit { get; set; }
		event Action hovered;
	}

	public static class IPinnedToolButtonMethods
	{
	}
}
#endif
