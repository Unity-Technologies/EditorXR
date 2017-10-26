#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Core
{
    [Serializable]
    public class AffordanceDefinition
    {
        [SerializeField]
        VRInputDevice.VRControl m_Control;

        [SerializeField]
        AffordanceAnimationDefinition m_AnimationDefinition;

        [SerializeField]
        AffordanceVisibilityDefinition m_VisibilityDefinition;

        public VRInputDevice.VRControl control { get { return m_Control; } set { m_Control = value; } }
        public AffordanceVisibilityDefinition visibilityDefinition { get { return m_VisibilityDefinition; } set { m_VisibilityDefinition = value; } }
        public AffordanceAnimationDefinition animationDefinition { get { return m_AnimationDefinition; } set { m_AnimationDefinition = value; } }
    }
}
#endif
