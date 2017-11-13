#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Core
{
    /// <summary>
    /// Definition containing data that represents this affordance
    /// </summary>
    [Serializable]
    public class AffordanceDefinition
    {
        [SerializeField]
        VRInputDevice.VRControl m_Control;

        [SerializeField]
        AffordanceAnimationDefinition m_AnimationDefinition;

        [SerializeField]
        AffordanceVisibilityDefinition m_VisibilityDefinition;

        /// <summary>
        /// The control associated with this affordance
        /// </summary>
        public VRInputDevice.VRControl control
        {
            get { return m_Control; }
            set { m_Control = value; }
        }

        /// <summary>
        /// The visibility definition used to drive changes to the visual elements representing this affordance
        /// </summary>
        public AffordanceVisibilityDefinition visibilityDefinition
        {
            get { return m_VisibilityDefinition; }
            set { m_VisibilityDefinition = value; }
        }

        /// <summary>
        /// The animation definition used to drive translation/rotation changes to the visual elements representing this affordance
        /// </summary>
        public AffordanceAnimationDefinition animationDefinition
        {
            get { return m_AnimationDefinition; }
            set { m_AnimationDefinition = value; }
        }
    }
}
#endif
