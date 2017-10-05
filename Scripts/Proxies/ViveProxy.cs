#if UNITY_EDITOR
using System;
using System.Collections;
using UnityEditor.Experimental.EditorVR.Input;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.VR;

namespace UnityEditor.Experimental.EditorVR.Proxies
{
	sealed class ViveProxy : TwoHandedProxyBase
	{
		[SerializeField]
		GameObject m_LeftHandTouchProxyPrefab;

		[SerializeField]
		GameObject m_RightHandTouchProxyPrefab;

		public override void Awake()
		{
			if (VRDevice.model.IndexOf("oculus", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				m_LeftHandProxyPrefab = m_LeftHandTouchProxyPrefab;
				m_RightHandProxyPrefab = m_RightHandTouchProxyPrefab;
			}
			
			base.Awake();
			m_InputToEvents = ObjectUtils.AddComponent<ViveInputToEvents>(gameObject);

#if !ENABLE_STEAMVR_INPUT
			enabled = false;
#endif
		}

#if ENABLE_STEAMVR_INPUT
		public override IEnumerator Start()
		{
			SteamVR_Render.instance.transform.parent = gameObject.transform;

			while (!active)
				yield return null;

			yield return base.Start();
		}
#endif
	}
}
#endif
