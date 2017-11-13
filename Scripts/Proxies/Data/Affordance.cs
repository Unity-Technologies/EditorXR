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
        Transform m_Transform;

        [SerializeField]
        Renderer m_Renderer;

        [Tooltip("(Optional) Specific material for this affordance. If null, all materials are used")]
        [SerializeField]
        Material m_Material;

        [SerializeField]
        AffordanceTooltip[] m_Tooltips;

        public VRInputDevice.VRControl control { get { return m_Control; } }
        public Transform transform { get { return m_Transform; } }
        public Renderer renderer { get { return m_Renderer; } }
        public Material material { get { return m_Material; } }
        public AffordanceTooltip[] tooltips { get { return m_Tooltips; } }
    }
}
#endif
