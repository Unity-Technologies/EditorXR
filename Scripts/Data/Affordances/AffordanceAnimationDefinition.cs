#if UNITY_EDITOR
using System;
using UnityEditor.Experimental.EditorVR.UI;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Core
{
    /// <summary>
    /// Definition containing data utilized to change the translation/rotation of an affordance
    /// </summary>
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

        /// <summary>
        /// The axes on which to perform translation of an affordance
        /// </summary>
        public AxisFlags translateAxes { get { return m_TranslateAxes; } set { m_TranslateAxes = value; } }

        /// <summary>
        /// The axes on which to perform rotation of an affordance
        /// </summary>
        public AxisFlags rotateAxes { get { return m_RotateAxes; } set { m_RotateAxes = value; } }

        /// <summary>
        /// The minimum value to which an affordance can be translated/rotated
        /// </summary>
        public float min { get { return m_Min; } set { m_Min = value; } }

        /// <summary>
        /// The maximum value to which an affordance can be translated/rotated
        /// </summary>
        public float max { get { return m_Max; } set { m_Max = value; } }

        /// <summary>
        /// Bool denoting that the defined translation/rotation should be reversed for the right hand
        /// </summary>
        public bool reverseForRightHand { get { return m_ReverseForRightHand; } set { m_ReverseForRightHand = value; } }

        public AffordanceAnimationDefinition(AffordanceAnimationDefinition definitionToCopy)
        {
            translateAxes = definitionToCopy.translateAxes;
            rotateAxes = definitionToCopy.rotateAxes;
            min = definitionToCopy.min;
            max = definitionToCopy.max;
            reverseForRightHand = definitionToCopy.reverseForRightHand;
        }
    }
}
#endif
