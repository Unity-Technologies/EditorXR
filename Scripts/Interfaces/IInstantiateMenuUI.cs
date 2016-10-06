using System;

namespace UnityEngine.VR.Tools
{
	public interface IInstantiateMenuUI
	{
		Func<Node, MenuOrigin, GameObject, GameObject> instantiateMenuUI
		{
			set;
		}
	}
}