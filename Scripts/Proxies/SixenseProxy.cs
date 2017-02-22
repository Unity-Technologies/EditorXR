using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Proxies
{
	internal sealed class SixenseProxy : TwoHandedProxyBase
	{
		private SixenseInputToEvents m_InputToEvents;

		public override bool active
		{
			get
			{
				return m_InputToEvents.active;
			}
		}

		public override void Awake()
		{
			base.Awake();
			transform.position = CameraUtils.GetViewerPivot().position; // Reference position should be the viewer pivot, so remove any offsets
			m_InputToEvents = ObjectUtils.AddComponent<SixenseInputToEvents>(gameObject);
		}
	}
}
