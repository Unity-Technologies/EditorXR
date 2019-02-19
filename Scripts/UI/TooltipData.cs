using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.UI
{
    /// <summary>
    /// Place on an object that receives UI events to set a tooltip on it
    /// </summary>
    class TooltipData : MonoBehaviour, ITooltip, ITooltipPlacement
    {
        [SerializeField]
        string m_TooltipText;
        public string tooltipText
        {
            get { return m_TooltipText; }
            protected set { m_TooltipText = value; }
        }

        [SerializeField]
        Transform m_TooltipSource;
        public Transform tooltipSource
        {
            get { return m_TooltipSource; }
            protected set { m_TooltipSource = value; }
        }

        [SerializeField]
        Transform m_TooltipTarget;
        public Transform tooltipTarget
        {
            get { return m_TooltipTarget; }
            protected set { m_TooltipTarget = value; }
        }

        [SerializeField]
        TextAlignment m_TooltipAlignment;
        public TextAlignment tooltipAlignment
        {
            get { return m_TooltipAlignment; }
            protected set { m_TooltipAlignment = value; }
        }
    }
}
