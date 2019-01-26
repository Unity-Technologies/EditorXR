using System;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Menus;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR.Tools
{
    public class BrushSizeUI : MonoBehaviour, IAlternateMenu
    {
        const float k_MinSize = 0.625f;
        const float k_MaxSize = 12.5f;

        [SerializeField]
        RectTransform m_SliderHandle;

        [SerializeField]
        Slider m_Slider;

        Image m_SliderHandleImage;
        MenuHideFlags m_MenuHideFlags;

        public Action<float> onValueChanged { private get; set; }
        public Bounds localBounds { get; private set; }
        public Transform rayOrigin { get; set; }

        public MenuHideFlags menuHideFlags {
            get { return m_MenuHideFlags; }
            set
            {
                if (m_MenuHideFlags == value)
                    return;

                m_MenuHideFlags = value;
                gameObject.SetActive(m_MenuHideFlags == 0);
            }
        }

        public GameObject menuContent { get { return gameObject; } }
        public int priority { get { return 2; } }

        void Awake()
        {
            localBounds = ObjectUtils.GetBounds(transform);
#if UNITY_EDITOR
            // We record property modifications on creation and modification of these UI elements, which will look odd when undone
            Undo.postprocessModifications += PostProcessModifications;
#endif
            m_SliderHandleImage = m_SliderHandle.GetComponent<Image>();
        }

#if UNITY_EDITOR
        UndoPropertyModification[] PostProcessModifications(UndoPropertyModification[] modifications)
        {
            var modificationList = new List<UndoPropertyModification>(modifications);
            foreach (var modification in modifications)
            {
                if (modification.currentValue.target == m_SliderHandle)
                    modificationList.Remove(modification);
            }

            return modificationList.ToArray();
        }
#endif

        public void OnSliderValueChanged(float value)
        {
            m_SliderHandle.localScale = Vector3.one * Mathf.Lerp(k_MinSize, k_MaxSize, value);

            if (onValueChanged != null)
                onValueChanged(value);
        }

        public void ChangeSliderValue(float newValue)
        {
            m_Slider.value = newValue;
        }

        public void OnBrushColorChanged(Color newColor)
        {
            m_SliderHandleImage.color = newColor;
        }
    }
}
