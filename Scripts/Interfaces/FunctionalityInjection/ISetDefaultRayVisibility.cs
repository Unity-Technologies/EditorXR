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
		internal delegate void DefaultRayVisibilityDelegate(Transform rayOrigin, object caller, bool rayVisible, bool coneVisible, int priority = 0);

		internal static DefaultRayVisibilityDelegate setDefaultRayVisibility { get; set; }

		/// <summary>
		/// Show the default proxy ray/cone
		/// </summary>
		/// <param name="rayOrigin">The ray to hide or show</param>
		/// <param name="caller">The object which has locked the ray</param>
		/// <param name="rayVisible">Show or hide</param>
		/// <param name="coneVisible">An optional parameter to hide or show only the ray and not the cone</param>
		/// <param name="priority">The priority level of this request</param>
		public static void SetDefaultRayVisibility(this ISetDefaultRayVisibility customRay, Transform rayOrigin, object caller, bool rayVisible, bool coneVisible, int priority = 0)
		{
			setDefaultRayVisibility(rayOrigin, caller, rayVisible, coneVisible, priority);
		}
	}
}
#endif
