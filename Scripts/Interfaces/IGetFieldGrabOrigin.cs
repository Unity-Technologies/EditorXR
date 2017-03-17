#if UNITY_EDITOR
using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Implementors receive a field grab origin transform
	/// </summary>
	public interface IGetFieldGrabOrigin
	{
		/// <summary>
		/// Get the field grab transform attached to the given rayOrigin
		/// </summary>
		Func<Transform, Transform> getFieldGrabOriginForRayOrigin { set; }
	}
}
#endif
