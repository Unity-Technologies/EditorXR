using System.Collections;
using UnityEngine.Experimental.EditorVR.Utilities;

namespace UnityEngine.Experimental.EditorVR.Proxies
{
	public class ViveProxy : TwoHandedProxyBase
	{
		private ViveInputToEvents m_InputToEvents;

#if ENABLE_STEAMVR_INPUT
		SteamVR_RenderModel m_RightModel;
		SteamVR_RenderModel m_LeftModel;
#endif

		public override bool active
		{
			get { return m_InputToEvents.active; }
		}

		public override void Awake()
		{
			base.Awake();
			m_InputToEvents = U.Object.AddComponent<ViveInputToEvents>(gameObject);
		}

		public override IEnumerator Start()
		{
#if ENABLE_STEAMVR_INPUT
			SteamVR_Render.instance.transform.parent = gameObject.transform;

			while (!active)
				yield return null;

			m_LeftModel = m_LeftHand.GetComponentInChildren<SteamVR_RenderModel>(true);
			m_LeftModel.enabled = true;
			m_RightModel = m_RightHand.GetComponentInChildren<SteamVR_RenderModel>(true);
			m_RightModel.enabled = true;

			yield return base.Start();
#else
			yield break;
#endif
		}

#if ENABLE_STEAMVR_INPUT
		public override void Update()
		{
			if (active && m_LeftModel && m_RightModel)
			{
				//If proxy is not mapped to a physical input device, check if one has been assigned
				if ((int) m_LeftModel.index == -1 && m_InputToEvents.steamDevice[0] != -1)
				{
					// HACK set device index individually instead of calling SetDeviceIndex because loading device mesh dynamically does not work in editor. Prefab has Model Override set and mesh generated, calling SetDeviceIndex clears the model.
					m_LeftModel.index = (SteamVR_TrackedObject.EIndex) m_InputToEvents.steamDevice[0];
				}
				if ((int) m_RightModel.index == -1 && m_InputToEvents.steamDevice[1] != -1)
				{
					m_RightModel.index = (SteamVR_TrackedObject.EIndex) m_InputToEvents.steamDevice[1];
				}
			}

			base.Update();
		}
#endif
	}
}