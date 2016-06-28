using UnityEngine;
using System;
using System.Collections.Generic;
using System.Security.AccessControl;
using UnityEngine.InputNew;

namespace UnityEngine.VR.Proxies
{
	public class TouchProxy : TwoHandedProxyBase
	{
		public override void Awake()
		{
			base.Awake();
			U.AddComponent<OVRTouchInputToEvents>(gameObject);
		}		
	}
}
