using System;

namespace UnityEngine.VR.Tools
{
	public interface IInstantiateUI
	{
		Func<GameObject, GameObject> instantiateUI { set; }
	}
}