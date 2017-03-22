#if UNITY_EDITOR
using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Provides access to checks that can test against the viewer's body
	/// </summary>
	public interface IUsesViewerBody
	{
	}

	public static class IUsesViewerBodyMethods
	{
		internal static Func<Transform, bool> isOverShoulder { get; set; }

		/// <summary>
		/// Returns whether the specified transform is over the viewer's shoulders and behind the head
		/// </summary>
		public static bool IsOverShoulder(this IUsesViewerBody obj, Transform transform)
		{
			if (isOverShoulder != null)
				return isOverShoulder(transform);

			return false;
		}
	}
}
#endif
