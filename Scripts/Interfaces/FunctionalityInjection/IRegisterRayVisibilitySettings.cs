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
		public delegate void RegisterRayVisibilitySettingsDelegate(Transform rayOrigin, object caller, bool rayVisible, bool coneVisible, int priority = 0);

		internal static Action<Transform, object> unregisterRayVisibilitySettings { get; set; }
		public static RegisterRayVisibilitySettingsDelegate registerRayVisibilitySettings;

		/// <summary>
		/// Register visibility settings to try and show/hide the ray/cone
		/// </summary>
		/// <param name="rayOrigin">The ray to hide or show</param>
		/// <param name="caller">The object which is adding settings</param>
		/// <param name="rayVisible">Show or hide the ray</param>
		/// <param name="coneVisible">Show or hide the cone</param>
		/// <param name="priority">The priority level of this request</param>
		public static void RegisterRayVisibilitySettings(this IRegisterRayVisibilitySettings customRay, Transform rayOrigin, object caller, bool rayVisible, bool coneVisible, int priority = 0)
		{
			registerRayVisibilitySettings(rayOrigin, caller, rayVisible, coneVisible, priority);
		}

		/// <summary>
		/// Unregister visibility settings
		/// </summary>
		/// <param name="rayOrigin">The ray to remove settings from</param>
		/// <param name="caller">The object whose settings to remove</param>
		public static void UnregisterRayVisibilitySettings(this IRegisterRayVisibilitySettings customRay, Transform rayOrigin, object caller)
		{
			unregisterRayVisibilitySettings(rayOrigin, caller);
		}
	}
}
#endif
