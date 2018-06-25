#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Proxies
{
    /// <summary>
    /// Affordance model; data defining a proxy affordance (button/analog/etc)
    /// </summary>
    [Serializable]
    class Affordance
    {
        [SerializeField]
        VRInputDevice.VRControl m_Control;

        [SerializeField]
        Transform[] m_Transforms;

        [SerializeField]
        Renderer[] m_Renderers;

        [Tooltip("(Optional) Specific materials for this affordance. If null, all materials are used")]
        [SerializeField]
        Material[] m_Materials;

        [SerializeField]
        AffordanceTooltip[] m_Tooltips;

        public VRInputDevice.VRControl control { get { return m_Control; } }
        public Transform[] transforms { get { return m_Transforms; } }
        public Renderer[] renderers { get { return m_Renderers; } }
        public Material[] materials { get { return m_Materials; } }
        public AffordanceTooltip[] tooltips { get { return m_Tooltips; } }
    }
}
#endif
