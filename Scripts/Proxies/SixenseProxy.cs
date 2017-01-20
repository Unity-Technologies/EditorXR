using UnityEngine.Experimental.EditorVR.Input;
using UnityEngine.Experimental.EditorVR.Utilities;

namespace UnityEngine.Experimental.EditorVR.Proxies
{
	public class SixenseProxy : TwoHandedProxyBase
	{
		public override void Awake()
		{
			base.Awake();
			transform.position = U.Camera.GetViewerPivot().position; // Reference position should be the viewer pivot, so remove any offsets
			m_InputToEvents = U.Object.AddComponent<SixenseInputToEvents>(gameObject);
		}
	}
}
