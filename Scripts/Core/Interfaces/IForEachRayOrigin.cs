using System;

namespace UnityEngine.Experimental.EditorVR.Core
{
	delegate void ForEachRayOriginCallback(Transform rayOrigin);

	internal interface IForEachRayOrigin
	{
		Action<ForEachRayOriginCallback> forEachRayOrigin { set; }
	}
}
