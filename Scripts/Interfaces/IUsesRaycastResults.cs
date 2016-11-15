using System;

namespace UnityEngine.VR.Tools
{
	public interface IUsesRaycastResults
	{
		Func<Transform, GameObject> getFirstGameObject { set; }
	}
}
