using UnityEngine.VR.Utilities;

namespace UnityEngine.VR.Proxies
{
	public class TouchProxy : TwoHandedProxyBase
	{
		private OVRTouchInputToEvents m_InputToEvents;

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
			m_InputToEvents = U.Object.AddComponent<OVRTouchInputToEvents>(gameObject);
		}
	}
}