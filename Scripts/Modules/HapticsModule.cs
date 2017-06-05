#if UNITY_EDITOR
using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
	sealed class HapticsModule : MonoBehaviour, IUsesGameObjectLocking
	{
		[SerializeField]
		float m_MasterIntensity = 1f;

		/// <summary>
		/// Overall intensity of haptics.
		/// A value to 0 will mute haptics.
		/// A value of 1 will allow haptics to be performed at normal intensity
		/// </summary>
		public float masterIntensity { set { m_MasterIntensity = Mathf.Clamp01(value); } }

		OVRHaptics.OVRHapticsChannel m_LHapticsChannel;
		OVRHaptics.OVRHapticsChannel m_RHapticsChannel;
		OVRHapticsClip m_GeneratedHapticClip;

		bool m_SampleLengthWarningShown;

		void Start()
		{
			m_LHapticsChannel = OVRHaptics.LeftChannel;
			m_RHapticsChannel = OVRHaptics.RightChannel;
			m_GeneratedHapticClip = new OVRHapticsClip();
		}

		void LateUpdate()
		{
			// Perform a manual update of OVR haptics
			OVRHaptics.Process();
		}

		public void Pulse(float duration, float intensity = 1f, bool fadeIn = false, bool fadeOut = false)
		{
			// Clip buffer can hold up to 800 milliseconds of samples
			// At 320Hz, each sample is 3.125f milliseconds
			if (Mathf.Approximately(m_MasterIntensity, 0))
				return;

			m_GeneratedHapticClip.Reset(); // TODO: Support multiple generated clips

			const float kMaxDuration = 0.8f;
			if (duration > kMaxDuration)
			{
				duration = Mathf.Clamp(duration, 0f, kMaxDuration); // Clamp at maxiumum 800ms for sample buffer

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

				m_GeneratedHapticClip.WriteSample(Convert.ToByte(sampleShaped));
			}

			const float kMaxSimultaneousClipDuration = 0.125f;
			if (duration > kMaxSimultaneousClipDuration)
			{
				// Prevent multiple long clips from playing back simultaneously
				// If the new clip has a long duration, stop playback of any existing clips
				m_RHapticsChannel.Preempt(m_GeneratedHapticClip);
				m_LHapticsChannel.Preempt(m_GeneratedHapticClip);
			}
			else
			{
				// Allow multiple short clips to play simultaneously
				m_RHapticsChannel.Mix(m_GeneratedHapticClip);
				m_LHapticsChannel.Mix(m_GeneratedHapticClip);
			}
		}
	}
}
#endif
