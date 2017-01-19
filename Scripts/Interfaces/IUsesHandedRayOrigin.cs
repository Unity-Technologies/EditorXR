using UnityEngine.Experimental.EditorVR.Tools;

namespace UnityEngine.Experimental.EditorVR {
	public interface IUsesHandedRayOrigin : IUsesRayOrigin
	{
		Node node { set; }
	}
}
