using System;

namespace UnityEngine.VR.Tools
{
	public interface ICustomMenuUI
	{
		Func<Transform, GameObject, GameObject> instantiateMenuUI { set; }

		Action<GameObject> destroyMenuUI { set; }
	}
}
