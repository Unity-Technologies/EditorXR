#if UNITY_2018_4_OR_NEWER
using System;
using System.IO;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEngine;
using UnityEngine.XR;

namespace UnityEditor.Experimental.EditorVR.Modules
{
    sealed class HapticsModule : MonoBehaviour, ISystemModule
    {
        public const float MaxDuration = 0.8f;

        [SerializeField]
        float m_MasterIntensity = 0.8f;

        /// <summary>
        /// Overall intensity of haptics.
        /// A value to 0 will mute haptics.
        /// A value of 1 will allow haptics to be performed at normal intensity
        /// </summary>
        public float masterIntensity { set { m_MasterIntensity = Mathf.Clamp(value, 0f, 10f); } }

        InputDevice m_LeftHand;
        InputDevice m_RightHand;
        MemoryStream m_GeneratedHapticClip;
        HapticCapabilities m_Capabilites;

        /// <summary>
        /// Allow for a single warning that informs the user of an attempted pulse with a length greater than 0.8f
        /// </summary>
        bool m_SampleLengthWarningShown;

        void Start()
        {
            m_LeftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            m_RightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            m_LeftHand.TryGetHapticCapabilities(out m_Capabilites);
            m_GeneratedHapticClip = new MemoryStream();
        }

        /// <summary>
        /// Pulse haptic feedback
        /// </summary>
        /// <param name="node">Node on which to perform the pulse.</param>
        /// <param name="hapticPulse">Haptic pulse</param>
        /// <param name="durationMultiplier">(Optional) Multiplier value applied to the hapticPulse duration</param>
        /// <param name="intensityMultiplier">(Optional) Multiplier value applied to the hapticPulse intensity</param>
        public void Pulse(Node node, HapticPulse hapticPulse, float durationMultiplier = 1f, float intensityMultiplier = 1f)
        {
            // Clip buffer can hold up to 800 milliseconds of samples
            // At 320Hz, each sample is 3.125f milliseconds
            if (Mathf.Approximately(m_MasterIntensity, 0))
                return;

            // Reset buffer
            m_GeneratedHapticClip.Seek(0, SeekOrigin.Begin);
            m_GeneratedHapticClip.SetLength(0);

            var duration = hapticPulse.duration * durationMultiplier;
            var intensity = hapticPulse.intensity * intensityMultiplier;
            var fadeIn = hapticPulse.fadeIn;
            var fadeOut = hapticPulse.fadeOut;
            if (duration > MaxDuration)
            {
                duration = Mathf.Clamp(duration, 0f, MaxDuration); // Clamp at maximum 800ms for sample buffer

                if (!m_SampleLengthWarningShown)
                    Debug.LogWarning("Pulse durations greater than 0.8f are not currently supported");

                m_SampleLengthWarningShown = true;
            }

            const int kSampleRateConversion = 490; // Samplerate conversion : 44100/90fps = 490
            const int kIntensityIncreaseMultiplier = 255; // Maximum value of 255 for intensity
            const float kFadeInProportion = 0.25f;
            var fadeInSampleCount = duration * kSampleRateConversion * kFadeInProportion;
            var fadeOutSampleCount = fadeInSampleCount * 2; // FadeOut is less apparent than FadeIn unless FadeOut duration is increased
            duration *= kSampleRateConversion;
            var durationFadeOutPosition = duration - fadeOutSampleCount;
            intensity = Mathf.Clamp(Mathf.Clamp01(intensity) * kIntensityIncreaseMultiplier * m_MasterIntensity, 0, kIntensityIncreaseMultiplier);
            var hapticClipSample = Convert.ToByte(intensity);
            for (int i = 1; i < duration; ++i)
            {
                float sampleShaped = hapticClipSample;
                if (fadeIn && i < fadeInSampleCount)
                    sampleShaped = Mathf.Lerp(0, intensity, i / fadeInSampleCount);
                else if (fadeOut && i > durationFadeOutPosition)
                    sampleShaped = Mathf.Lerp(0, intensity, (duration - i) / fadeOutSampleCount);

                var sampleByte = Convert.ToByte(sampleShaped);
                m_GeneratedHapticClip.WriteByte(sampleByte);
            }

            const float kMaxSimultaneousClipDuration = 0.25f;
            var channel = GetTargetChannel(node);
            if (duration > kMaxSimultaneousClipDuration)
            {
                // Prevent multiple long clips from playing back simultaneously
                // If the new clip has a long duration, stop playback of any existing clips in order to prevent haptic feedback noise
                var buffer = m_GeneratedHapticClip.GetBuffer();
                if (node == Node.None)
                {
                    StopPulses();
                    if (m_Capabilites.supportsBuffer)
                    {
                        m_LeftHand.SendHapticBuffer(0, buffer);
                        m_RightHand.SendHapticBuffer(0, buffer);
                    }
                    else
                    {
                        m_LeftHand.SendHapticImpulse(0, intensity, duration);
                        m_RightHand.SendHapticImpulse(0, intensity, duration);
                    }
                }
                else
                {
                    StopPulses(node);
                    if (m_Capabilites.supportsBuffer)
                        channel.SendHapticBuffer(0, buffer);
                    else
                        channel.SendHapticImpulse(0, intensity, duration);
                }
            }
        }

        public void StopPulses(Node node)
        {
            var channel = GetTargetChannel(node);
            channel.StopHaptics();
        }

        public void StopPulses()
        {
            m_LeftHand.StopHaptics();
            m_RightHand.StopHaptics();
        }

        InputDevice GetTargetChannel(Node node)
        {
            if (node == Node.LeftHand)
                return m_LeftHand;
            if (node == Node.RightHand)
                return m_RightHand;

            return default(InputDevice);
        }
    }
}
#endif
