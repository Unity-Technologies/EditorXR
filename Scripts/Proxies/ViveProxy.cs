#if UNITY_EDITOR
using System;
using System.Collections;
using UnityEditor.Experimental.EditorVR.Input;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.XR;

namespace UnityEditor.Experimental.EditorVR.Proxies
{
    sealed class ViveProxy : TwoHandedProxyBase
    {
        [SerializeField]
        GameObject m_LeftHandTouchProxyPrefab;

        [SerializeField]
        GameObject m_RightHandTouchProxyPrefab;

#if ENABLE_STEAMVR_INPUT
		SteamVR_RenderModel m_LeftModel;
		SteamVR_RenderModel m_RightModel;
#endif

        public override void Awake()
        {
#if UNITY_2017_2_OR_NEWER
            if (XRDevice.model.IndexOf("oculus", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                m_LeftHandProxyPrefab = m_LeftHandTouchProxyPrefab;
                m_RightHandProxyPrefab = m_RightHandTouchProxyPrefab;
            }

            base.Awake();
            m_InputToEvents = ObjectUtils.AddComponent<ViveInputToEvents>(gameObject);

#if !ENABLE_STEAMVR_INPUT
            enabled = false;
#endif
#endif
        }

#if ENABLE_STEAMVR_INPUT
		public override IEnumerator Start()
		{
			SteamVR_Render.instance.transform.parent = gameObject.transform;

			while (!active)
				yield return null;

			m_LeftModel = m_LeftHand.GetComponentInChildren<SteamVR_RenderModel>(true);
			m_LeftModel.enabled = true;
			m_RightModel = m_RightHand.GetComponentInChildren<SteamVR_RenderModel>(true);
			m_RightModel.enabled = true;

			yield return base.Start();
		}

		public override void Update()
		{
			if (active && m_LeftModel && m_RightModel)
			{
				var viveInputToEvents = (ViveInputToEvents)m_InputToEvents;

				//If proxy is not mapped to a physical input device, check if one has been assigned
				if ((int) m_LeftModel.index == -1 && viveInputToEvents.steamDevice[0] != -1)
				{
					// HACK set device index individually instead of calling SetDeviceIndex because loading device mesh dynamically does not work in editor. Prefab has Model Override set and mesh generated, calling SetDeviceIndex clears the model.
					m_LeftModel.index = (SteamVR_TrackedObject.EIndex)viveInputToEvents.steamDevice[0];
				}

				if ((int) m_RightModel.index == -1 && viveInputToEvents.steamDevice[1] != -1)
				{
					m_RightModel.index = (SteamVR_TrackedObject.EIndex)viveInputToEvents.steamDevice[1];
				}
			}

			base.Update();
		}
#endif
    }
}

#endif
