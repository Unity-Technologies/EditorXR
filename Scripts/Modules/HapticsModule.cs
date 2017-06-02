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

		public void PerformHaptics(float duration, float intensity = 1f)
		{
			if (Mathf.Approximately(m_MasterIntensity, 0))
				return;

			m_GeneratedHapticClip.Reset(); // TODO: Support multiple generated clips

			const int kIntensityIncreaseMultiplier = 25;
			intensity = Mathf.Clamp(intensity * kIntensityIncreaseMultiplier * m_MasterIntensity, 0, 255);
			byte hapticClipSample = Convert.ToByte(intensity);
			var clipLength = 25;
			for (int i = 0; i < clipLength; ++i)
				m_GeneratedHapticClip.WriteSample(hapticClipSample);

			m_RHapticsChannel.Mix(m_GeneratedHapticClip);
			m_LHapticsChannel.Mix(m_GeneratedHapticClip);
		}
	}
}
#endif
