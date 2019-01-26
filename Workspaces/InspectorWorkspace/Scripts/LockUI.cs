using System;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
    sealed class LockUI : MonoBehaviour, IUsesStencilRef
    {
        [SerializeField]
        Image m_LockImage;

        [SerializeField]
        Sprite m_LockIcon;

        [SerializeField]
        Sprite m_UnlockIcon;

        [SerializeField]
        WorkspaceButton m_Button;

        public byte stencilRef { get; set; }

        public event Action<Transform> hovered;
        public event Action<Transform> clicked;

        public void Setup()
        {
            var mr = GetComponentInChildren<MeshRenderer>();
            foreach (var sm in mr.sharedMaterials)
            {
                sm.SetInt("_StencilRef", stencilRef);
            }

            m_Button.clicked += OnClicked;
            m_Button.hovered += OnHovered;
        }

        void OnClicked(Transform rayOrigin)
        {
            if (clicked != null)
                clicked(rayOrigin);
        }

        void OnHovered(Transform rayOrigin)
        {
            if (hovered != null)
                hovered(rayOrigin);
        }

        public void UpdateIcon(bool locked)
        {
            m_LockImage.sprite = locked ? m_LockIcon : m_UnlockIcon;
        }
    }
}
