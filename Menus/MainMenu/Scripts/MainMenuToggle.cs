#if UNITY_EDITOR
using System;
using UnityEditor.Experimental.EditorVR;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if INCLUDE_TEXT_MESH_PRO
using TMPro;
#endif

[assembly: OptionalDependency("TMPro.TextMeshProUGUI", "INCLUDE_TEXT_MESH_PRO")]

namespace UnityEditor.Experimental.EditorVR.Menus
{
    sealed class MainMenuToggle : MainMenuSelectable, IRayEnterHandler, IRayExitHandler, IPointerClickHandler
    {
        [SerializeField]
        Toggle m_Toggle;

        public Toggle toggle { get { return m_Toggle; } }

        public event Action<Transform, Type, string> hovered;
        public event Action<Transform> clicked;

        new void Awake()
        {
            m_Selectable = m_Toggle;
            m_OriginalColor = m_Toggle.targetGraphic.color;
        }

        public void OnRayEnter(RayEventData eventData)
        {
#if INCLUDE_TEXT_MESH_PRO
            if (hovered != null)
                hovered(eventData.rayOrigin, toolType, null);
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
