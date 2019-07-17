using System;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR.Menus
{
    sealed class MainMenuToggle : MainMenuSelectable, IRayEnterHandler, IRayExitHandler, IPointerClickHandler
    {
#pragma warning disable 649
        [SerializeField]
        Toggle m_Toggle;
#pragma warning restore 649

        CanvasGroup m_CanvasGroup;

        public Toggle toggle { get { return m_Toggle; } }

        public event Action<Transform> hovered;
        public event Action<Transform> clicked;

        new void Awake()
        {
            m_Selectable = m_Toggle;
            m_OriginalColor = m_Toggle.targetGraphic.color;
        }

        void Start()
        {
            m_CanvasGroup = m_Toggle.GetComponentInParent<CanvasGroup>();
        }

        public void OnRayEnter(RayEventData eventData)
        {
            if (m_CanvasGroup && !m_CanvasGroup.interactable)
                return;

            var rayOrigin = eventData.rayOrigin;
            if (rayOrigin == null)
                rayOrigin = eventData.camera.transform;

            if (m_Toggle.interactable && hovered != null)
                hovered(rayOrigin);
        }

        public void OnRayExit(RayEventData eventData)
        {
            if (m_CanvasGroup && !m_CanvasGroup.interactable)
                return;

            var rayOrigin = eventData.rayOrigin;
            if (rayOrigin == null)
                rayOrigin = eventData.camera.transform;

            if (m_Toggle.interactable && hovered != null)
                hovered(rayOrigin);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (m_CanvasGroup && !m_CanvasGroup.interactable)
                return;

            if (m_Toggle.interactable && clicked != null)
                clicked(null); // Pass null to perform the selection haptic pulse on both nodes
        }
    }
}
