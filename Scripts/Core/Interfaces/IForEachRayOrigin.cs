using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Core
{
	delegate void ForEachRayOriginCallback(Transform rayOrigin);

	internal interface IForEachRayOrigin
	{
		Action<ForEachRayOriginCallback> forEachRayOrigin { set; }
	}
}
