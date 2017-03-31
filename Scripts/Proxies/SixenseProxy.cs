#if UNITY_EDITOR
using UnityEditor.Experimental.EditorVR.Input;
using UnityEditor.Experimental.EditorVR.Utilities;

namespace UnityEditor.Experimental.EditorVR.Proxies
{
	sealed class SixenseProxy : TwoHandedProxyBase
	{
		public override void Awake()
		{
			base.Awake();
			transform.position = CameraUtils.GetCameraRig().position; // Reference position should be the camera rig root, so remove any offsets
			m_InputToEvents = ObjectUtils.AddComponent<SixenseInputToEvents>(gameObject);
		}
	}
}
#endif
