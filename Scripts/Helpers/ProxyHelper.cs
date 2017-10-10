#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Proxies
{
    /// <summary>
    /// Reference container for additional content origins on a device
    /// </summary>
    sealed class ProxyHelper : MonoBehaviour
    {
        [Serializable]
        public class ButtonObject
        {
            [SerializeField]
            VRInputDevice.VRControl m_Control;

            [SerializeField]
            Transform m_Transform;

            [SerializeField]
            Renderer m_Renderer;

            public VRInputDevice.VRControl control { get { return m_Control; } }

            public Transform transform { get { return m_Transform; } }

            public Renderer renderer { get { return m_Renderer; } }
        }

        /// <summary>
        /// The transform that the device's ray contents (default ray, custom ray, etc) will be parented under
        /// </summary>
        public Transform rayOrigin
        {
            get { return m_RayOrigin; }
        }

        [SerializeField]
        private Transform m_RayOrigin;

        /// <summary>
        /// The transform that the menu content will be parented under
        /// </summary>
        public Transform menuOrigin
        {
            get { return m_MenuOrigin; }
        }

        [SerializeField]
        private Transform m_MenuOrigin;

        /// <summary>
        /// The transform that the alternate-menu content will be parented under
        /// </summary>
        public Transform alternateMenuOrigin
        {
            get { return m_AlternateMenuOrigin; }
        }

        [SerializeField]
        private Transform m_AlternateMenuOrigin;

        /// <summary>
        /// The transform that the display/preview objects will be parented under
        /// </summary>
        public Transform previewOrigin
        {
            get { return m_PreviewOrigin; }
        }

        [SerializeField]
        private Transform m_PreviewOrigin;

        /// <summary>
        /// The transform that the display/preview objects will be parented under
        /// </summary>
        public Transform fieldGrabOrigin
        {
            get { return m_FieldGrabOrigin; }
        }

        [SerializeField]
        private Transform m_FieldGrabOrigin;

        /// <summary>
        /// The root transform of the device/controller mesh-renderers/geometry
        /// </summary>
        public Transform meshRoot
        {
            get { return m_MeshRoot; }
        }

        [SerializeField]
        private Transform m_MeshRoot;

        [SerializeField]
        ButtonObject[] m_Buttons;

        public ButtonObject[] buttons { get { return m_Buttons; } }
    }
}

#endif
