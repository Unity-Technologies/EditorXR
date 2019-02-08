using UnityEditor.Experimental.EditorVR.Core;

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
        internal delegate void PulseDelegate(Node node, HapticPulse hapticPulse, float durationMultiplier = 1f, float intensityMultiplier = 1f);

        internal static PulseDelegate pulse { get; set; }

        /// <summary>
        /// Perform a haptic feedback pulse
        /// </summary>
        /// <param name="node">Node on which to control the pulse</param>
        /// <param name="hapticPulse">Haptic pulse to perform</param>
        /// <param name="durationMultiplier">(Optional) Multiplier value applied to the hapticPulse duration</param>
        /// <param name="intensityMultiplier">(Optional) Multiplier value applied to the hapticPulse intensity</param>
        public static void Pulse(this IControlHaptics obj, Node node, HapticPulse hapticPulse, float durationMultiplier = 1f, float intensityMultiplier = 1f)
        {
            pulse(node, hapticPulse, durationMultiplier, intensityMultiplier);
        }

        internal delegate void StopPulsesDelegate(Node node);

        internal static StopPulsesDelegate stopPulses { get; set; }

        /// <summary>
        /// Stop all haptic feedback on a specific device, or all devices
        /// </summary>
        /// <param name="node">Device RayOrigin/Transform on which to stop all pulses. A NULL value will stop pulses on all devices</param>
        public static void StopPulses(this IControlHaptics obj, Node node)
        {
            stopPulses(node);
        }
    }
}
