using UnityEditor;
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

	    private void Start()
	    {
            // Touch controllers should be spawned under a pivot that corresponds to the head with no offsets, since the
            // local positions of the controllers will be provided that way.
#if UNITY_EDITOR
	        EditorApplication.delayCall += () =>
	        {
	            transform.localPosition = Vector3.zero;
	        };
#else
            transform.localPosition = Vector3.zero;
#endif
        }
    }
}