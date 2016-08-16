using System;
using UnityEngine;
using System.Collections;

namespace UnityEngine.VR.Tools
{
	public delegate GameObject SelectionRayCast(Transform rayOrigin, out float distance, out float directRayLength);
	public interface IRaycaster
	{
		SelectionRayCast getFirstGameObject { set; }
	}
}
