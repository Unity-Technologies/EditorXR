using System;

namespace UnityEngine.Experimental.EditorVR
{
	public delegate void ShowToolTipDelegate(ITooltip tooltip, bool centered = true);

	/// <summary>
	/// Provides access to the ability to show or hide a Tooltip
	/// </summary>
	public interface IUsesTooltip
	{
		/// <summary>
		/// Show the given Tooltip
		/// </summary>
		ShowToolTipDelegate showTooltip { set; }

		/// <summary>
		/// Hide the given Tooltip
		/// </summary>
		Action<ITooltip> hideTooltip { set; }
	}
}
