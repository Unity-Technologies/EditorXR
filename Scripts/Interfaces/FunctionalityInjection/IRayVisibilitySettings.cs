#if UNITY_EDITOR
using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Implementors can show & hide the default ray
	/// </summary>
	public interface IRayVisibilitySettings
	{
	}

	public static class IRayVisibilitySettingsMethods
	{
		public delegate void AddRayVisibilitySettingsDelegate(Transform rayOrigin, object caller, bool rayVisible, bool coneVisible, int priority = 0);

		internal static Action<Transform, object> removeRayVisibilitySettings { get; set; }
		public static AddRayVisibilitySettingsDelegate addRayVisibilitySettings;

		/// <summary>
		/// Add visibility settings to try and show/hide the ray/cone
		/// </summary>
		/// <param name="rayOrigin">The ray to hide or show</param>
		/// <param name="caller">The object which is adding settings</param>
		/// <param name="rayVisible">Show or hide the ray</param>
		/// <param name="coneVisible">Show or hide the cone</param>
		/// <param name="priority">The priority level of this request</param>
		public static void AddRayVisibilitySettings(this IRayVisibilitySettings customRay, Transform rayOrigin, object caller, bool rayVisible, bool coneVisible, int priority = 0)
		{
			addRayVisibilitySettings(rayOrigin, caller, rayVisible, coneVisible, priority);
		}

		/// <summary>
		/// Remove visibility settings
		/// </summary>
		/// <param name="rayOrigin">The ray to remove settings from</param>
		/// <param name="caller">The object whose settings to remove</param>
		public static void RemoveRayVisibilitySettings(this IRayVisibilitySettings customRay, Transform rayOrigin, object caller)
		{
			removeRayVisibilitySettings(rayOrigin, caller);
		}
	}
}
#endif
