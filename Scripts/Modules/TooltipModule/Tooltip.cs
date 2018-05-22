
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.UI
{
    internal sealed class Tooltip : MonoBehaviour, ITooltip, ITooltipPlacement
    {
        public string tooltipText
        {
            get { return m_TooltipText; }
            set { m_TooltipText = value; }
        }

        [SerializeField]
        string m_TooltipText;

        public Transform tooltipTarget
        {
            get { return m_TooltipTarget; }
            set { m_TooltipTarget = value; }
        }

        [SerializeField]
        Transform m_TooltipTarget;

        public Transform tooltipSource
        {
            get { return m_TooltipSource; }
            set { m_TooltipSource = value; }
        }

        [SerializeField]
        Transform m_TooltipSource;

        public TextAlignment tooltipAlignment
        {
            get { return m_TooltipAlignment; }
            set { m_TooltipAlignment = value; }
        }

        [SerializeField]
        TextAlignment m_TooltipAlignment = TextAlignment.Center;

        void Start()
        {
            if (!m_TooltipTarget)
                m_TooltipTarget = transform;
        }
    }
}

