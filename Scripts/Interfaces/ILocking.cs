using System;
using UnityEngine;
using System.Collections;

namespace UnityEngine.VR.Tools
{
	public interface ILocking
	{
		Func<bool> toggleLocked
		{
			set;
		}

		Func<GameObject,bool> getLocked
		{
			get;
			set;
		}

		Action<GameObject, Node?> checkHover
		{
			set;
		}
	}
}