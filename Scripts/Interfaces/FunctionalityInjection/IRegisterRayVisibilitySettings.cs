#if UNITY_EDITOR
using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Implementors can show & hide the default ray
	/// </summary>
	public interface IRegisterRayVisibilitySettings
	{
	}

	public static class IRegisterRayVisibilitySettingsMethods
	{
		internal delegate void RegisterRayVisibilitySettingsDelegate(Transform rayOrigin, object caller, bool rayVisible, bool coneVisible, int priority = 0);

		internal static RegisterRayVisibilitySettingsDelegate registerRayVisibilitySettings { get; set; }
		internal static Action<Transform, object> unregisterRayVisibilitySettings { get; set; }

		/// <summary>
		/// Register visibility settings to try and show/hide the ray/cone
		/// </summary>
		/// <param name="rayOrigin">The ray to hide or show</param>
		/// <param name="caller">The object which has locked the ray</param>
		/// <param name="rayVisible">Show or hide</param>
		/// <param name="coneVisible">An optional parameter to hide or show only the ray and not the cone</param>
		/// <param name="priority">The priority level of this request</param>
		public static void RegisterRayVisibilitySettings(this IRegisterRayVisibilitySettings customRay, Transform rayOrigin, object caller, bool rayVisible, bool coneVisible, int priority = 0)
		{
			registerRayVisibilitySettings(rayOrigin, caller, rayVisible, coneVisible, priority);
		}

		/// <summary>
		/// Unregister visibility settings
		/// </summary>
		/// <param name="rayOrigin">The ray to hide or show</param>
		/// <param name="caller">The object which has locked the ray</param>
		public static void UnregisterRayVisibilitySettings(this IRegisterRayVisibilitySettings customRay, Transform rayOrigin, object caller)
		{
			unregisterRayVisibilitySettings(rayOrigin, caller);
		}
	}
}
#endif
