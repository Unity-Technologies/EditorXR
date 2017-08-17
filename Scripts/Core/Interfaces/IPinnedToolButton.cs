#if UNITY_EDITOR
using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Provides an interface for handling Pinned Tool Button functionality
	/// </summary>
	public interface IPinnedToolButton
	{
		Type previewToolType { set; }
		Type toolType { get; }

		int order { get; set; }
		int maxButtonCount { set; }
		float iconHighlightedLocalZOffset { set; }
		bool highlighted { get; set; }
		bool secondaryButtonHighlighted { get; }
		bool isActiveTool { set; }
		bool toolTipVisible { set; }
		bool implementsSecondaryButton { set; }
		Vector3 primaryUIContentContainerLocalScale { get; set; }
		Transform tooltipTarget { set; }
		string previewToolDescription { set; }

		Action destroy { get; }
		Action<Type> selectTool { set; }
		Action<IPinnedToolButton> showAllButtons { set; }
		Action hoverExit { set; }

		Func<Type, int> visibileButtonCount { set; }
		Func<bool> closeButton { set; }

		event Action hovered;
	}
}
#endif
