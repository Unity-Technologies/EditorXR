#if UNITY_EDITOR
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Gives decorated class ability to perform haptic feedback
	/// </summary>
	public interface IPerformHaptics
	{
	}

	public static class IPerformHapticsMethods
	{
		internal delegate void PerformHapticsDelegate(Transform rayOrigin, float duration, float intensity = 1f, bool fadeIn = false, bool fadeOut = false);

		internal static PerformHapticsDelegate performHaptics { get; set; }

		/// <summary>
		/// Perform haptic feedback
		/// </summary>
		/// <param name="rayOrigin">Device RayOrigin Transform on which to perform the pulse. A NULL value will pulse on all devices</param>
		/// <param name="duration">Duration of haptic feedback</param>
		/// <param name="intensity">Intensity of haptic feedback (optional)</param>
		/// <param name="fadeIn">Fade the pulse in</param>
		/// <param name="fadeOut">Fade the pulse out</param>
		public static void Pulse(this IPerformHaptics obj, Transform rayOrigin, float duration, float intensity = 1f, bool fadeIn = false, bool fadeOut = false)
		{
			performHaptics(rayOrigin, duration, intensity, fadeIn, fadeOut);
		}
	}
}
#endif
