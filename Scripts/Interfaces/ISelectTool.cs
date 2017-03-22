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
	}

	public static class ISelectToolMethods
	{
		internal static Func<Transform, Type, bool> selectTool { get; set; }

		/// <summary>
		/// Delegate used to select tools from the menu
		/// Returns whether the tool was successfully selected
		/// </summary>
		/// <param name="rayOrigin">The rayOrigin that the tool should spawn under</param>
		/// <param name="toolType">Type of tool to spawn/select</param>
		public static bool SelectTool(this ISelectTool obj, Transform rayOrigin, Type toolType)
		{
			if (selectTool != null)
				return selectTool(rayOrigin, toolType);

			return false;
		}
	}
}
#endif
