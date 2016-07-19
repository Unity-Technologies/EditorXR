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
			m_InputToEvents = U.Object.AddComponent<SixenseInputToEvents>(gameObject);
		}
	}
}
