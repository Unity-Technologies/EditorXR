using UnityEngine.Experimental.EditorVR.Input;
using UnityEngine.Experimental.EditorVR.Utilities;

namespace UnityEngine.Experimental.EditorVR.Proxies
{
	public class SixenseProxy : TwoHandedProxyBase
	{
		public override void Awake()
		{
			base.Awake();
			transform.position = U.Camera.GetCameraRig().position; // Reference position should be the camera rig root, so remove any offsets
			m_InputToEvents = U.Object.AddComponent<SixenseInputToEvents>(gameObject);
		}
	}
}
