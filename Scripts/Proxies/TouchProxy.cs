using System.Collections;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Proxies
{
	internal sealed class TouchProxy : TwoHandedProxyBase
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
			m_InputToEvents = ObjectUtils.AddComponent<OVRTouchInputToEvents>(gameObject);
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