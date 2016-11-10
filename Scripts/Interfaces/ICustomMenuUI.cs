using System;

namespace UnityEngine.VR.Tools
{
	public interface ICustomMenuUI
	{
		Func<Transform, MenuOrigin, GameObject, GameObject> instantiateMenuUI
		{
			set;
		}

		Action<GameObject> destroyMenuUI
		{
			set;
		}
	}
}
