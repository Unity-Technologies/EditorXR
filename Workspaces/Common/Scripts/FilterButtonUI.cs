#if UNITY_EDITOR
using System;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Button = UnityEditor.Experimental.EditorVR.UI.Button;

#if INCLUDE_TEXT_MESH_PRO
using TMPro;
#endif

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
    sealed class FilterButtonUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IRayEnterHandler
    {
        const float k_HoverAlpha = 1;
        const float k_NormalAlpha = 0.95f;

        public Button button
        {
            get { return m_Button; }
        }

        [SerializeField]
        Button m_Button;

        [SerializeField]
        Image m_EyePanel;

        [SerializeField]
        Image m_Eye;

        [SerializeField]
        Image m_TextPanel;

#if INCLUDE_TEXT_MESH_PRO
        [SerializeField]
        TextMeshProUGUI m_Text;
#else
        [SerializeField]
        Text m_Text;
#endif

        Transform m_InteractingRayOrigin;

#if INCLUDE_TEXT_MESH_PRO
        public TextMeshProUGUI text { get { return m_Text; } }
#else
        public Text text { get; set; }
#endif

        public Color color
        {
            set
            {
                m_Text.color = value;
                if (m_Eye)
                    m_Eye.color = value;
            }
        }

        public event Action<Transform> hovered;
        public event Action<Transform> clicked;

        void Awake()
        {
            m_Button.onClick.AddListener(OnButtonClicked);
        }

        void OnDestroy()
        {
            m_Button.onClick.RemoveAllListeners();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            var c = m_TextPanel.color;
            c.a = k_HoverAlpha;
            m_TextPanel.color = c;

            if (m_EyePanel)
                m_EyePanel.color = c;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            var c = m_TextPanel.color;
            c.a = k_NormalAlpha;
            m_TextPanel.color = c;
            if (m_EyePanel)
                m_EyePanel.color = c;
        }

        public void OnRayEnter(RayEventData eventData)
        {
            m_InteractingRayOrigin = eventData.rayOrigin;

            if (hovered != null)
                hovered(eventData.rayOrigin);
        }

        void OnButtonClicked()
        {
            if (clicked != null)
                clicked(m_InteractingRayOrigin);
        }
    }
}
#endif
