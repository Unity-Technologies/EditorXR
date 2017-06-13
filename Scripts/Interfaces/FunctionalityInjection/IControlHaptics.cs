#if UNITY_EDITOR
using UnityEditor.Experimental.EditorVR.Core;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Gives decorated class ability to control haptic feedback
	/// </summary>
	public interface IControlHaptics
	{
	}

	public static class IControlHapticsMethods
	{
		internal delegate void PulseDelegate(Transform rayOrigin, HapticPulse hapticPulse);

		internal static PulseDelegate pulse { get; set; }

		/// <summary>
		/// Perform a haptic feedback pulse
		/// </summary>
		/// <param name="rayOrigin">Device RayOrigin/Transform on which to control the pulse. A NULL value will pulse on all devices</param>
		public static void Pulse(this IControlHaptics obj, Transform rayOrigin, HapticPulse hapticPulse)
		{
			pulse(rayOrigin, hapticPulse);
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
