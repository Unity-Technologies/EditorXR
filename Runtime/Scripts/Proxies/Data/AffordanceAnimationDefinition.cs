using System;
using Unity.Labs.EditorXR.Interfaces;
using Unity.Labs.Utils.GUI;
using UnityEngine;

namespace Unity.Labs.EditorXR.Core
{
    /// <summary>
    /// Definition containing data utilized to change the translation/rotation of an affordance
    /// </summary>
    [Serializable]
    public class AffordanceAnimationDefinition
    {
#pragma warning disable 649
        [FlagsProperty]
        [SerializeField]
        AxisFlags m_TranslateAxes;

        [FlagsProperty]
        [SerializeField]
        AxisFlags m_RotateAxes;

        [SerializeField]
        float m_Min;

        [SerializeField]
        float m_Max;

        [SerializeField]
        bool m_ReverseForRightHand;
#pragma warning restore 649

        /// <summary>
        /// The axes on which to perform translation of an affordance
        /// </summary>
        public AxisFlags translateAxes { get { return m_TranslateAxes; } }

        /// <summary>
        /// The axes on which to perform rotation of an affordance
        /// </summary>
        public AxisFlags rotateAxes { get { return m_RotateAxes; } }

        /// <summary>
        /// The minimum value to which an affordance can be translated/rotated
        /// </summary>
        public float min { get { return m_Min; } }

        /// <summary>
        /// The maximum value to which an affordance can be translated/rotated
        /// </summary>
        public float max { get { return m_Max; } }

        /// <summary>
        /// Bool denoting that the defined translation/rotation should be reversed for the right hand
        /// </summary>
        public bool reverseForRightHand { get { return m_ReverseForRightHand; } }
    }
}
