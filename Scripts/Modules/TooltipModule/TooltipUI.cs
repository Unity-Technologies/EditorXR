#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR.Modules
{
    sealed class TooltipUI : MonoBehaviour
    {
        [SerializeField]
        Text m_Text;

        [SerializeField]
        RawImage m_DottedLine;

        [SerializeField]
        Transform[] m_Spheres;

        [SerializeField]
        Image m_Highlight;

        [SerializeField]
        Image m_Background;

        [SerializeField]
        TooltipVisibilityListener m_VisibilityListener;

        public Text text { get { return m_Text; } }
        public RawImage dottedLine { get { return m_DottedLine; } }
        public Transform[] spheres { get { return m_Spheres; } }
        public Image highlight { get { return m_Highlight; } }
        public Image background { get { return m_Background; } }

        public event Action becameVisible
        {
            add { m_VisibilityListener.becameVisible += value; }
            remove { m_VisibilityListener.becameVisible -= value; }
        }
    }
}
#endif
