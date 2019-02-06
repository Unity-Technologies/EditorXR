#if UNITY_EDITOR
using System;
using UnityEditor.Experimental.EditorVR;
using UnityEngine;
using UnityEngine.UI;

#if INCLUDE_TEXT_MESH_PRO
using TMPro;
#endif

[assembly: OptionalDependency("TMPro.TextMeshProUGUI", "INCLUDE_TEXT_MESH_PRO")]

namespace UnityEditor.Experimental.EditorVR.Menus
{
    abstract class MainMenuSelectable : MonoBehaviour
    {
        protected Selectable m_Selectable;

#if INCLUDE_TEXT_MESH_PRO
        [SerializeField]
        protected TextMeshProUGUI m_Description;

        [SerializeField]
        protected TextMeshProUGUI m_Title;
#endif

        protected Color m_OriginalColor;

        public Type toolType { get; set; }

        public bool selected
        {
            set
            {
                if (value)
                {
                    m_Selectable.transition = Selectable.Transition.None;
                    m_Selectable.targetGraphic.color = m_Selectable.colors.highlightedColor;
                }
                else
                {
                    m_Selectable.transition = Selectable.Transition.ColorTint;
                    m_Selectable.targetGraphic.color = m_OriginalColor;
                }
            }
        }

        protected void Awake()
        {
            m_OriginalColor = m_Selectable.targetGraphic.color;
        }

        public void SetData(string name, string description)
        {
#if INCLUDE_TEXT_MESH_PRO
            m_Title.text = name;
            if (m_Description != null)
                m_Description.text = description;
#endif
        }
    }
}
#endif
