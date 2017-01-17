using System;

namespace UnityEngine.Experimental.EditorVR
{
	public interface IUsesToolTip
	{
		Action<GameObject> addToolTip { set; }
	}
}
