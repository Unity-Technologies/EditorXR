using UnityEngine.VR.Utilities;

namespace UnityEngine.VR.Proxies
{
	public class TouchProxy : TwoHandedProxyBase
	{
		public override bool Active
		{
			get
			{
				return m_InputToEvents.Active;
			}
		}

		private OVRTouchInputToEvents m_InputToEvents;

		public override void Awake()
		{
			base.Awake();
			m_InputToEvents = U.Object.AddComponent<OVRTouchInputToEvents>(gameObject);
		}		
	}
}