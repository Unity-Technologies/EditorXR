#if UNITY_EDITOR
using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Gives decorated class the ability to select tools from a menu
	/// </summary>
	public interface ISelectTool
	{
		/// <summary>
		/// Delegate used to select tools from the menu
		/// Transform = ray origin
		/// Type = type of tool
		/// Returns whether the tool was successfully selected
		/// </summary>
		Func<Transform, Type, bool> selectTool { set; }
	}
}
#endif
