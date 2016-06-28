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
				return false;
			}
		}

		public override void Awake()
		{
			U.AddComponent<SixenseInputToEvents>(gameObject);
		}		
	}
}
