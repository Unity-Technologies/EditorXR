using System;

namespace UnityEngine.VR.Tools
{
	public interface IHandleToolMenuUI
	{
		Func<Node, MenuOrigin, GameObject, GameObject> instantiateMenuUI
		{
			set;
		}

		Action<GameObject> destroyMenuUI
		{
			set;
		}
	}
}
