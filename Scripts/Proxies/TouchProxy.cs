using System.Collections;
using UnityEditor;
using UnityEngine.Experimental.EditorVR.Input;
using UnityEngine.Experimental.EditorVR.Utilities;

namespace UnityEngine.Experimental.EditorVR.Proxies
{
	public class TouchProxy : TwoHandedProxyBase
	{
		public override void Awake()
		{
			base.Awake();
			m_InputToEvents = U.Object.AddComponent<OVRTouchInputToEvents>(gameObject);
		}

		public override IEnumerator Start()
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

			return base.Start();
		}
	}
}