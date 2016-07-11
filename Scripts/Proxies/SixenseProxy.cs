using UnityEngine.VR.Utilities;

namespace UnityEngine.VR.Proxies
{
	public class SixenseProxy : TwoHandedProxyBase
	{
		public override bool active
		{
			get
			{
				return m_InputToEvents.active;
			}
		}

		private SixenseInputToEvents m_InputToEvents;

		public override void Awake()
		{
			base.Awake();
			m_InputToEvents = U.Object.AddComponent<SixenseInputToEvents>(gameObject);
		}		
	}
}
