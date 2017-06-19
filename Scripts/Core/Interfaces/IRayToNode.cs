#if UNITY_EDITOR
using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Provide the ability to request a corresponding node for a ray origin
	/// </summary>
	interface IRayToNode
	{
		/// <summary>
		/// Get the corresponding node for a given ray origin
		/// </summary>
		Func<Transform, Node?> requestNodeFromRayOrigin { set; }
	}
}
#endif
