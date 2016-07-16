using System;
using UnityEngine;
using System.Collections;

namespace UnityEngine.VR.Tools
{
	public interface IRaycaster
	{
		Func<Transform, GameObject> GetGameObjectOver { set; }
	}
}
