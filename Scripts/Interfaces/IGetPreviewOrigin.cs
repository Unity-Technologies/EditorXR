#if UNITY_EDITOR
using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Implementors receive a preview origin transform
	/// </summary>
	public interface IGetPreviewOrigin
	{
	}

	public static class IGetPreviewOriginMethods
	{
		internal static Func<Transform, Transform> getPreviewOriginForRayOrigin { get; set; }

		/// <summary>
		/// Get the preview transform attached to the given rayOrigin
		/// </summary>
		public static Transform GetPreviewOriginForRayOrigin(this IGetPreviewOrigin obj, Transform rayOrigin)
		{
			if (getPreviewOriginForRayOrigin != null)
				return getPreviewOriginForRayOrigin(rayOrigin);

			return null;
		}
	}
}
#endif
