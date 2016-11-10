using System;
using UnityEngine;
using System.Collections;


namespace UnityEngine.VR.Tools
{
	public interface ISetHighlight
	{
		Action<GameObject, bool> setHighlight { set; }
	}
}