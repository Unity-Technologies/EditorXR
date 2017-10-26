#if UNITY_EDITOR
using System;
using UnityEditor.Experimental.EditorVR.UI;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Core
{
    [Serializable]
    public class AffordanceAnimationDefinition
    {
        [FlagsProperty]
        [SerializeField]
        AxisFlags m_TranslateAxes;

        [FlagsProperty]
        [SerializeField]
        AxisFlags m_RotateAxes;

        [SerializeField]
        float m_Min;

        [SerializeField]
        float m_Max = 5f;

        [SerializeField]
        bool m_ReverseForRightHand;

        public AxisFlags translateAxes { get { return m_TranslateAxes; } set { m_TranslateAxes = value; } }
        public AxisFlags rotateAxes { get { return m_RotateAxes; } set { m_RotateAxes = value; } }
        public float min { get { return m_Min; } set { m_Min = value; } }
        public float max { get { return m_Max; } set { m_Max = value; } }
        public bool reverseForRightHand { get { return m_ReverseForRightHand; } set { m_ReverseForRightHand = value; } }
    }
}
#endif
