using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.EditorXR.Menus
{
    abstract class MainMenuSelectable : MonoBehaviour
    {
        protected Selectable m_Selectable;

        [SerializeField]
        protected TextMeshProUGUI m_Description;

        [SerializeField]
        protected TextMeshProUGUI m_Title;

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
            m_Title.text = name;
            if (m_Description != null)
                m_Description.text = description;
        }
    }
}
