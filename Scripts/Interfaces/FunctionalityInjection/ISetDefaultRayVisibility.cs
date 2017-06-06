#if UNITY_EDITOR
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Implementors can show & hide the default ray
	/// </summary>
	public interface ISetDefaultRayVisibility : IUsesRayLocking
	{
	}

	public static class ISetDefaultRayVisibilityMethods
	{
		internal delegate void DefaultRayVisibilityDelegate(Transform rayOrigin, bool visible, bool rayOnly = false);

		internal static DefaultRayVisibilityDelegate setDefaultRayVisibility { get; set; }

		/// <summary>
		/// Show the default proxy ray/cone
		/// </summary>
		/// <param name="rayOrigin">The ray to hide or show</param>
		/// <param name="visible">Show or hide</param>
		/// <param name="rayOnly">An optional parameter to hide or show only the ray and not the cone</param>
		public static void SetDefaultRayVisibility(this ISetDefaultRayVisibility customRay, Transform rayOrigin, bool visible, bool rayOnly = false)
		{
			setDefaultRayVisibility(rayOrigin, visible, rayOnly);
		}
	}
}
#endif
