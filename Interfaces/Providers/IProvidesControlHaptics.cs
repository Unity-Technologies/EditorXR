using Unity.EditorXR.Core;
using Unity.XRTools.ModuleLoader;

namespace Unity.EditorXR.Interfaces
{
    /// <summary>
    /// Provides the ability to control haptic feedback
    /// </summary>
    public interface IProvidesControlHaptics : IFunctionalityProvider
    {
        /// <summary>
        /// Perform a haptic feedback pulse
        /// </summary>
        /// <param name="node">Node on which to control the pulse</param>
        /// <param name="hapticPulse">Haptic pulse to perform</param>
        /// <param name="durationMultiplier">(Optional) Multiplier value applied to the hapticPulse duration</param>
        /// <param name="intensityMultiplier">(Optional) Multiplier value applied to the hapticPulse intensity</param>
        void Pulse(Node node, HapticPulse hapticPulse, float durationMultiplier = 1f, float intensityMultiplier = 1f);

        /// <summary>
        /// Stop all haptic feedback on a specific device, or all devices
        /// </summary>
        /// <param name="node">Device RayOrigin/Transform on which to stop all pulses. A NULL value will stop pulses on all devices</param>
        void StopPulses(Node node);
    }
}
