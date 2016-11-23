using System;

namespace UnityEngine.VR.Tools
{
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