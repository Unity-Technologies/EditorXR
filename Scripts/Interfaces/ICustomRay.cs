#if UNITY_EDITOR
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Method signature for hiding or showing the default ray
	/// <param name="rayOrigin">The ray to hide or show</param>
	/// <param name="onlyRay">An optional parameter to hide or show only the ray</param>
	/// </summary>
	
	/// <summary>
	/// Implementors can show & hide the default ray
	/// </summary>
	public interface ICustomRay : IUsesRayLocking
	{
		//DefaultRayVisibilityDelegate ShowDefaultRay { set; }

		//DefaultRayVisibilityDelegate HideDefaultRay { set; }
	}

	public static class ICustomRayMethods
	{
		internal delegate void DefaultRayVisibilityDelegate(Transform rayOrigin, bool onlyRay = false);

		internal static DefaultRayVisibilityDelegate showDefaultRay { get; set; }
		internal static DefaultRayVisibilityDelegate hideDefaultRay { get; set; }

		/// <summary>
		/// Show the default proxy ray/cone
		/// </summary>
		/// <param name="rayOrigin">The ray to hide or show</param>
		/// <param name="rayOnly">An optional parameter to hide or show only the ray</param>
		public static void ShowDefaultRay(this ICustomRay customRay, Transform rayOrigin, bool rayOnly = false)
		{
			if (showDefaultRay != null)
				showDefaultRay(rayOrigin, rayOnly);
		}

		/// <summary>
		/// Hide the default proxy ray/cone
		/// </summary>
		/// <param name="rayOrigin">The ray to hide or show</param>
		/// <param name="rayOnly">An optional parameter to hide or show only the ray</param>
		public static void HideDefaultRay(this ICustomRay customRay, Transform rayOrigin, bool rayOnly = false)
		{
			if (hideDefaultRay != null)
				hideDefaultRay(rayOrigin, rayOnly);
		}
	}
}
#endif
