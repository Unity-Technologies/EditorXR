using System;
using UnityEngine;
using System.Collections;

namespace UnityEngine.VR.Tools
{
	public interface ILocking
	{
		Action<GameObject,bool> setLocked
		{
			set;
		}

		Func<GameObject,bool> getLocked
		{
			get;
			set;
		}
	}
}