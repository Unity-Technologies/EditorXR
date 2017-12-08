#if UNITY_EDITOR
using System;
using UnityEditor.Experimental.EditorVR;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[assembly: OptionalDependency("TMPro.TextMeshProUGUI", "INCLUDE_TEXT_MESH_PRO")]

namespace UnityEditor.Experimental.EditorVR.Menus
{
    sealed class MainMenuButton : MainMenuSelectable, ITooltip, IRayEnterHandler, IRayExitHandler, IPointerClickHandler
    {
        [SerializeField]
        Button m_Button;

        public Button button { get { return m_Button; } }

        public string tooltipText { get { return tooltip != null ? tooltip.tooltipText : null; } }

        public ITooltip tooltip { private get; set; }

        public event Action<Transform, Type, string> hovered;
        public event Action<Transform> clicked;

        new void Awake()
        {
            m_Selectable = m_Button;
            m_OriginalColor = m_Button.targetGraphic.color;
        }

        public void OnRayEnter(RayEventData eventData)
        {
#if INCLUDE_TEXT_MESH_PRO
            if (hovered != null)
                hovered(eventData.rayOrigin, toolType, m_Description.text);
#endif
        }

        public void OnRayExit(RayEventData eventData)
        {
            if (hovered != null)
                hovered(eventData.rayOrigin, null, null);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (clicked != null)
                clicked(null); // Pass null to perform the selection haptic pulse on both nodes
        }
    }
}
#endif
