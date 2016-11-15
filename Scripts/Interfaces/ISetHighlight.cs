using System;

namespace UnityEngine.VR.Tools
{
	public interface ISetHighlight
	{
		Action<GameObject, bool> setHighlight { set; }
	}
}