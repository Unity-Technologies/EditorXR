#if UNITY_EDITOR
using System;
using UnityEditor.Experimental.EditorVR.UI;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Proxies
{
    /// <summary>
    /// Affordance model; data defining a proxy affordance (button/analog/etc)
    /// </summary>
    [Serializable]
    internal class Affordance
    {
        [SerializeField]
        VRInputDevice.VRControl m_Control;

        [SerializeField]
        Transform m_Transform;

        [SerializeField]
        Renderer m_Renderer;

        [SerializeField]
        Tooltip[] m_Tooltips;

        public VRInputDevice.VRControl control { get { return m_Control; } }
        public Transform transform { get { return m_Transform; } }
        public Renderer renderer { get { return m_Renderer; } }
        public Tooltip[] tooltips { get { return m_Tooltips; } }
    }
}
#endif
