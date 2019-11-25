using UnityEngine;

namespace Unity.Labs.EditorXR.Core
{
    /// <summary>
    /// Serialized data to describe a haptic pulse
    /// </summary>
    [CreateAssetMenu(menuName = "EditorXR/Haptic Pulse", fileName = "NewHapticPulse.asset")]
    public class HapticPulse : ScriptableObject
    {
#pragma warning disable 649
        [SerializeField]
        [Range(0.001f, 1f)]
        float m_Duration = 0.25f;

        [SerializeField]
        [Range(0f, 1f)]
        float m_Intensity = 1f;

        [SerializeField]
        bool m_FadeIn;

        [SerializeField]
        bool m_FadeOut;
#pragma warning restore 649

        /// <summary>
        /// The duration of the pulse
        /// </summary>
        public float duration
        {
            get { return m_Duration; }
            internal set { m_Duration = value; }
        }

        /// <summary>
        /// The intensity of the pulse
        /// </summary>
        public float intensity
        {
            get { return m_Intensity; }
            internal set { m_Intensity = value; }
        }

        /// <summary>
        /// Whether to fade in this pulse
        /// </summary>
        public bool fadeIn { get { return m_FadeIn; } }

        /// <summary>
        /// Whether to fade out this pulse
        /// </summary>
        public bool fadeOut { get { return m_FadeOut; } }
    }
}
