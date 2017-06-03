#if UNITY_EDITOR
using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
	sealed class HapticsModule : MonoBehaviour, IUsesGameObjectLocking
	{
		[SerializeField]
		private float m_MasterIntensity = 1f;

		[SerializeField]
		private float m_ShortPulseDuration = 0.125f;

		[SerializeField]
		private float m_MediumPulseDuration = 0.25f;

		[SerializeField]
		private float m_LongPulseDuration = 0.5f;

		/// <summary>
		/// Overall intensity of haptics.
		/// A value to 0 will mute haptics.
		/// A value of 1 will allow haptics to be performed at normal intensity
		/// </summary>
		public float masterIntensity { set { m_MasterIntensity = Mathf.Clamp01(value); } }

		OVRHaptics.OVRHapticsChannel m_LHapticsChannel;
		OVRHaptics.OVRHapticsChannel m_RHapticsChannel;
		OVRHapticsClip m_GeneratedHapticClip;

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

		public void PerformHaptics(float duration, float intensity = 1f, bool fadeIn = false, bool fadeOut = false)
		{
			// Clip buffer can hold up to 800 milliseconds of samples
			// At 320Hz, each sample is 3.125f milliseconds
			if (Mathf.Approximately(m_MasterIntensity, 0))
				return;

			m_GeneratedHapticClip.Reset(); // TODO: Support multiple generated clips

			const float kSampleRateConversion = 490; // Samplerate conversion : 44100/90fps = 490
			var fadeInSampleCount = duration * kSampleRateConversion * 0.25f;
			const int kIntensityIncreaseMultiplier = 255; // Maximum value of 255 for intensity
			intensity = Mathf.Clamp(Mathf.Clamp01(intensity) * kIntensityIncreaseMultiplier * m_MasterIntensity, 0, 255);
			byte hapticClipSample = Convert.ToByte(intensity);
			duration *= kSampleRateConversion;
			for (int i = 1; i < duration; ++i)
			{
				float sampleShaped = hapticClipSample;
				if (fadeIn && i < fadeInSampleCount)
				{
					sampleShaped = Mathf.Lerp(0, intensity, i / fadeInSampleCount);
				}
				else if (fadeOut && i > duration - fadeInSampleCount)
				{
					sampleShaped = Mathf.Lerp(0, intensity, duration - i / fadeInSampleCount);
					Debug.LogWarning(Convert.ToByte(sampleShaped) + " - i: " + i + " - duration: " + duration + " - fadeInSampleCount: " + fadeInSampleCount);
				}

				m_GeneratedHapticClip.WriteSample(Convert.ToByte(sampleShaped));
			}

			m_RHapticsChannel.Mix(m_GeneratedHapticClip);
			m_LHapticsChannel.Mix(m_GeneratedHapticClip);
		}
	}
}
#endif
