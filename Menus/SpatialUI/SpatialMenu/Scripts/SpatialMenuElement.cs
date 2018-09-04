#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor.Experimental.EditorVR.Menus;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Abstract class housing common SpatualMenu element functionality
    /// </summary>
    internal abstract class SpatialMenuElement : MonoBehaviour, ISpatialMenuElement, IControlHaptics,
        IRayEnterHandler, IRayExitHandler, IRayClickHandler, IPointerClickHandler
    {
        [SerializeField]
        protected TextMeshProUGUI m_Text;

        [SerializeField]
        protected Image m_Icon;

        [SerializeField]
        protected CanvasGroup m_CanvasGroup;

        [SerializeField]
        protected Button m_Button;

        [SerializeField]
        protected float m_TransitionDuration = 0.75f;

        [SerializeField]
        protected float m_FadeInZOffset = 0.05f;

        [SerializeField]
        protected float m_HighlightedZOffset = -0.005f;

        // Abstract members
        public abstract bool highlighted { get; set; }
        public abstract bool visible { get; set; }

        // SpatialMenuElement implementation
        public Action<Transform, Action, string, string> Setup { get; protected set; }
        public Action<Node> selected { get; set; }
        public Action<SpatialMenu.SpatialMenuData> highlightedAction { get; set; }
        public SpatialMenu.SpatialMenuData parentMenuData { get; set; }
        public Node spatialMenuActiveControllerNode { get; set; }
        public Node hoveringNode { get; set; }

        public void OnRayEnter(RayEventData eventData)
        {
            highlighted = true;
            hoveringNode = eventData.node;
        }

        public void OnRayExit(RayEventData eventData)
        {
            highlighted = false;
            hoveringNode = Node.None;
        }

        public void OnRayClick(RayEventData eventData)
        {
            Debug.LogError("OnRayClick called for spatial menu section title element :" + m_Text.text);
            throw new NotImplementedException();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Debug.LogError("OnPointerClick called for spatial menu section title element :" + m_Text.text);
            throw new NotImplementedException();
        }
    }
}
#endif
