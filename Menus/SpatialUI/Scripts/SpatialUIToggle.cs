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
    public class SpatialUIToggle : IRayEnterHandler, IRayExitHandler, IPointerClickHandler
    {
        [SerializeField]
        Toggle m_Toggle;

        public Toggle toggle { get { return m_Toggle; } }

        public event Action<Transform> hovered;
        public event Action<Transform> clicked;

        void IRayEnterHandler.OnRayEnter(RayEventData eventData)
        {
            if (m_Toggle.interactable && hovered != null)
                hovered(eventData.rayOrigin);
        }

        void IRayExitHandler.OnRayExit(RayEventData eventData)
        {
            if (m_Toggle.interactable && hovered != null)
                hovered(eventData.rayOrigin);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (m_Toggle.interactable && clicked != null)
                clicked(null); // Pass null to perform the selection haptic pulse on both nodes
        }
    }
}
#endif
