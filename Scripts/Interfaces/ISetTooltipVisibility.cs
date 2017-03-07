using System;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Provides access to the ability to show or hide a Tooltip
	/// </summary>
	public interface ISetTooltipVisibility
	{
		/// <summary>
		/// Show the given Tooltip
		/// </summary>
		Action<ITooltip> showTooltip { set; }

		/// <summary>
		/// Hide the given Tooltip
		/// </summary>
		Action<ITooltip> hideTooltip { set; }
	}
}
