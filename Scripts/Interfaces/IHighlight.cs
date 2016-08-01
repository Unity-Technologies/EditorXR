using System;
using UnityEngine;
using System.Collections;


namespace UnityEngine.VR.Tools
{
	public interface IHighlight
	{
		Action<GameObject, bool> setHighlight { set; }
	}
}