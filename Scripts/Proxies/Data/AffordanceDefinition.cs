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
        AffordanceAnimationDefinition[] m_AnimationDefinitions;

        [SerializeField]
        AffordanceVisibilityDefinition[] m_VisibilityDefinitions;

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
        public AffordanceVisibilityDefinition[] visibilityDefinitions
        {
            get { return m_VisibilityDefinitions; }
            set { m_VisibilityDefinitions = value; }
        }

        /// <summary>
        /// The animation definition used to drive translation/rotation changes to the visual elements representing this affordance
        /// </summary>
        public AffordanceAnimationDefinition[] animationDefinitions
        {
            get { return m_AnimationDefinitions; }
            set { m_AnimationDefinitions = value; }
        }
    }
}
