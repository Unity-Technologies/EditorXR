using UnityEngine.VR.Utilities;

namespace UnityEngine.VR.Proxies
{
	public class SixenseProxy : TwoHandedProxyBase
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
			transform.position = U.Camera.GetViewerPivot().position; // Reference position should be the viewer pivot, so remove any offsets
			m_InputToEvents = U.Object.AddComponent<SixenseInputToEvents>(gameObject);
		}
	}
}
