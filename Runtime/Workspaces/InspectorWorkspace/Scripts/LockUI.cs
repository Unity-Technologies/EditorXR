using System;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.Labs.EditorXR.Workspaces
{
    sealed class LockUI : MonoBehaviour, IUsesStencilRef
    {
        const string k_MaterialStencilRefProperty = "_StencilRef";

#pragma warning disable 649
        [SerializeField]
        Image m_LockImage;

        [SerializeField]
        Sprite m_LockIcon;

        [SerializeField]
        Sprite m_UnlockIcon;

        [SerializeField]
        WorkspaceButton m_Button;
#pragma warning restore 649

        public byte stencilRef { get; set; }

        public event Action<Transform> hovered;
        public event Action<Transform> clicked;

        public void Setup()
        {
            var mr = GetComponentInChildren<MeshRenderer>();
            foreach (var sm in mr.sharedMaterials)
            {
                sm.SetInt(k_MaterialStencilRefProperty, stencilRef);
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
