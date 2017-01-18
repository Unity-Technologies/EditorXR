using System;

namespace UnityEngine.Experimental.EditorVR
{
	public interface IUsesTooltip
	{
		Action<ITooltip> showTooltip { set; }
		Action<ITooltip> hideTooltip { set; }
	}
}
