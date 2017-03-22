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
	}

	public static class IGetFieldGrabOriginMethods
	{
		internal static Func<Transform, Transform> getFieldGrabOriginForRayOrigin { get; set; }

		/// <summary>
		/// Get the field grab transform attached to the given rayOrigin
		/// </summary>
		public static Transform GetFieldGrabOriginForRayOrigin(this IGetFieldGrabOrigin obj, Transform rayOrigin)
		{
			if (getFieldGrabOriginForRayOrigin != null)
				return getFieldGrabOriginForRayOrigin(rayOrigin);

			return null;
		}
	}
}
#endif
