using UnityEngine;
using System;
using System.Collections.Generic;
using System.Security.AccessControl;
using UnityEngine.InputNew;

namespace UnityEngine.VR.Proxies
{
	public class SixenseProxy : TwoHandedProxyBase
	{
		public override bool Active
		{
			get
			{
				return m_InputToEvents.Active;
			}
		}

		private SixenseInputToEvents m_InputToEvents;

		public override void Awake()
		{
            base.Awake();
			m_InputToEvents = U.AddComponent<SixenseInputToEvents>(gameObject);
		}		
	}
}
