using System;
using UnityEditor.Experimental.EditorVR;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR.Menus
{
    sealed class MainMenuButton : MainMenuSelectable, ITooltip, IRayEnterHandler, IRayExitHandler, IPointerClickHandler
    {
#pragma warning disable 649
        [SerializeField]
        Button m_Button;
#pragma warning restore 649

        CanvasGroup m_CanvasGroup;

        public Button button { get { return m_Button; } }

        public string tooltipText { get { return tooltip != null ? tooltip.tooltipText : null; } }

        public ITooltip tooltip { private get; set; }

        public event Action<Transform, Type, string> hovered;
        public event Action<Transform> clicked;

        public UnityEvent hoverStarted;
        public UnityEvent hoverEnded;

        new void Awake()
        {
            m_Selectable = m_Button;
            m_OriginalColor = m_Button.targetGraphic.color;
        }

        void Start()
        {
            m_CanvasGroup = m_Button.GetComponentInParent<CanvasGroup>();
        }

        public void OnRayEnter(RayEventData eventData)
        {
            if (m_CanvasGroup && !m_CanvasGroup.interactable)
                return;

            if (button.interactable)
            {
                var descriptionText = string.Empty;
#if INCLUDE_TEXT_MESH_PRO
                // We can't use ?? because it breaks on destroyed references
                if (m_Description)
                    descriptionText = m_Description.text;
#endif

                if (hovered != null)
                    hovered(eventData.rayOrigin, toolType, descriptionText);

                if (hoverStarted != null)
                    hoverStarted.Invoke();
            }
        }

        public void OnRayExit(RayEventData eventData)
        {
            if (m_CanvasGroup && !m_CanvasGroup.interactable)
                return;

            if (button.interactable)
            {

                if (hovered != null)
                    hovered(eventData.rayOrigin, null, null);

                if (hoverEnded != null)
                    hoverEnded.Invoke();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (m_CanvasGroup && !m_CanvasGroup.interactable)
                return;

            if (button.interactable && clicked != null)
                clicked(null); // Pass null to perform the selection haptic pulse on both nodes
        }
    }
}
