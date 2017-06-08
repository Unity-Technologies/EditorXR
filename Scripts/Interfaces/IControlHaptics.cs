#if UNITY_EDITOR
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Gives decorated class ability to perform haptic feedback
	/// </summary>
	public interface IControlHaptics
	{
	}

	public static class IControlHapticsMethods
	{
		internal delegate void PulseDelegate(Transform rayOrigin, float duration, float intensity = 1f, bool fadeIn = false, bool fadeOut = false);

		internal static PulseDelegate pulse { get; set; }

		/// <summary>
		/// Perform haptic feedback
		/// </summary>
		/// <param name="rayOrigin">Device RayOrigin/Transform on which to perform the pulse. A NULL value will pulse on all devices</param>
		/// <param name="duration">Duration of haptic feedback</param>
		/// <param name="intensity">Intensity of haptic feedback (optional)</param>
		/// <param name="fadeIn">Fade the pulse in</param>
		/// <param name="fadeOut">Fade the pulse out</param>
		public static void Pulse(this IControlHaptics obj, Transform rayOrigin, float duration, float intensity = 1f, bool fadeIn = false, bool fadeOut = false)
		{
			pulse(rayOrigin, duration, intensity, fadeIn, fadeOut);
		}

		internal delegate void StopPulsesDelegate(Transform rayOrigin);

		internal static StopPulsesDelegate stopPulses { get; set; }

		/// <summary>
		/// Stop all haptic feedback on a specific device, or all devices
		/// </summary>
		/// <param name="rayOrigin">Device RayOrigin/Transform on which to stop all pulses. A NULL value will stop pulses on all devices</param>
		public static void StopPulses(this IControlHaptics obj, Transform rayOrigin)
		{
			stopPulses(rayOrigin);
		}
	}
}
#endif
