#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Abstract class housing common SpatialMenu element functionality
    /// </summary>
    public abstract class SpatialMenuElement : MonoBehaviour, IControlHaptics,
        IRayEnterHandler, IRayExitHandler
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

        public Node hoveringNode { get; set; }

        /// <summary>
        /// Bool denoting that this element is currently highlighted
        /// </summary>
        public virtual bool highlighted { get; set; }

        /// <summary>
        /// Bool denoting that this element is currently visible
        /// </summary>
        public virtual bool visible { get; set; }

        /// <summary>
        /// Sets up the model and view for this particular element
        /// </summary>
        public Action<Transform, Action, string, string> Setup { get; set; }

        /// <summary>
        /// Action performed when this element is selected
        /// The node denotes either the controlling SpatialMenu's node,
        /// or the node of a hovering proxy (which takes precedence over the menu control node)
        /// The main purpose of the node is to allow a selected action to perform a
        /// rayOriginal dependent actions (selecting & assigning a tools to a given proxy, etc)
        /// </summary>
        public Action<Node> selected { get; set; }

        /// <summary>
        /// Action performed when this element is highlighted
        /// </summary>
        public Action<SpatialMenu.SpatialMenuData> highlightedAction { get; set; }

        /// <summary>
        /// Reference to the data defining the parent menu of this element
        /// Used to display certain relevant visual elements relating to the parent menu
        /// </summary>
        public SpatialMenu.SpatialMenuData parentMenuData { get; set; }

        /// <summary>
        /// If the menu element isn't being hovered, utilize this node for performing any node-dependent logic
        /// </summary>
        public Node spatialMenuActiveControllerNode { get; set; }

        void IRayEnterHandler.OnRayEnter(RayEventData eventData)
        {
            highlighted = true;
            hoveringNode = eventData.node;
        }

        void IRayExitHandler.OnRayExit(RayEventData eventData)
        {
            highlighted = false;
            hoveringNode = Node.None;
        }
    }
}
#endif
