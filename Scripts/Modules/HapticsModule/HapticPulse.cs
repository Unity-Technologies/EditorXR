using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Core
{
    [CreateAssetMenu(menuName = "EditorXR/Haptic Pulse", fileName = "NewHapticPulse.asset")]
    public class HapticPulse : ScriptableObject
    {
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

        // Don't allow public setting of value; use inspector-set values
        public float duration
        {
            get { return m_Duration; }
            internal set { m_Duration = value; }
        }

        public float intensity
        {
            get { return m_Intensity; }
            internal set { m_Intensity = value; }
        }

        public bool fadeIn { get { return m_FadeIn; } }

        public bool fadeOut { get { return m_FadeOut; } }
    }
}
