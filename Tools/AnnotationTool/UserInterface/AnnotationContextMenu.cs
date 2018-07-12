#if UNITY_EDITOR && UNITY_2017_2_OR_NEWER
using System;
using UnityEditor.Experimental.EditorVR.Menus;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR.Tools
{
    sealed class AnnotationContextMenu : MonoBehaviour, IMenu
    {
        [SerializeField]
        Button m_CloseButton;

        [SerializeField]
        Toggle m_GroupToggleMesh;

        [SerializeField]
        Toggle m_GroupToggleTransform;

        [SerializeField]
        Toggle m_PressureToggleOn;

        [SerializeField]
        Toggle m_PressureToggleOff;

        [SerializeField]
        Slider m_PressureSlider;

        [SerializeField]
        ColorPickerUI m_ColorPicker;

        [SerializeField]
        Slider m_ColorSlider;

        AnnotationTool.Preferences m_Preferences;
        bool m_InsideUIUpdate;

        public Action close;
        public Action<Color> colorChanged;

        public Transform toolRayOrigin { set { m_ColorPicker.toolRayOrigin = value; } }

        public Bounds localBounds { get; private set; }
        public int priority { get { return 1; } }

        public AnnotationTool.Preferences preferences
        {
            set
            {
                m_Preferences = value;
                UpdatePreferences();
            }
        }

        public MenuHideFlags menuHideFlags
        {
            get { return gameObject.activeSelf ? 0 : MenuHideFlags.Hidden; }
            set { gameObject.SetActive(value == 0); }
        }

        public GameObject menuContent { get { return gameObject; } }

        public void Close()
        {
            close();
        }

        void Awake()
        {
            localBounds = ObjectUtils.GetBounds(transform);

            // Link up the UI Controls
            m_CloseButton.onClick.AddListener(Close);
            m_GroupToggleMesh.onValueChanged.AddListener(GroupToggleEventMesh);
            m_GroupToggleTransform.onValueChanged.AddListener(GroupToggleEventTransform);
            m_PressureToggleOn.onValueChanged.AddListener(PressureToggleEventOn);
            m_PressureToggleOff.onValueChanged.AddListener(PressureToggleEventOff);
            m_PressureSlider.onValueChanged.AddListener(PressureSliderChanged);
            m_ColorPicker.onColorPicked = ColorPickerChanged;
        }

        void UpdatePreferences()
        {
            if (m_Preferences == null)
            {
                return;
            }

            UpdateGroupToggle();
            UpdatePressureToggle();
            UpdatePressureSlider();
            UpdateColorPicker();
        }

        void UpdateGroupToggle()
        {
            m_InsideUIUpdate = true;
            m_GroupToggleMesh.isOn = m_Preferences.meshGroupingMode;
            m_GroupToggleTransform.isOn = !m_Preferences.meshGroupingMode;
            m_InsideUIUpdate = false;
        }

        void UpdatePressureToggle()
        {
            m_InsideUIUpdate = true;
            m_PressureToggleOn.isOn = m_Preferences.pressureSensitive;
            m_PressureToggleOff.isOn = !m_Preferences.pressureSensitive;
            m_InsideUIUpdate = false;
        }

        void UpdatePressureSlider()
        {
            m_InsideUIUpdate = true;
            m_PressureSlider.value = m_Preferences.pressureSmoothing;
            m_InsideUIUpdate = false;
        }

        void UpdateColorPicker()
        {
            m_InsideUIUpdate = true;
            m_ColorSlider.value = m_Preferences.annotationColor.maxColorComponent;
            m_InsideUIUpdate = false;
        }

        void GroupToggleEventMesh(bool isOn)
        {
            if (m_InsideUIUpdate)
                return;

            if (m_Preferences != null)
            {
                m_Preferences.meshGroupingMode = isOn;
            }

            UpdateGroupToggle();
        }

        void GroupToggleEventTransform(bool isOn)
        {
            if (m_InsideUIUpdate)
                return;

            if (m_Preferences != null)
            {
                m_Preferences.meshGroupingMode = !isOn;
            }

            UpdateGroupToggle();
        }

        void PressureToggleEventOn(bool isOn)
        {
            if (m_InsideUIUpdate)
                return;

            if (m_Preferences != null)
            {
                m_Preferences.pressureSensitive = isOn;
            }

            UpdatePressureToggle();
        }

        void PressureToggleEventOff(bool isOn)
        {
            if (m_InsideUIUpdate)
                return;

            if (m_Preferences != null)
            {
                m_Preferences.pressureSensitive = !isOn;
            }

            UpdatePressureToggle();
        }

        void PressureSliderChanged(float value)
        {
            if (m_InsideUIUpdate)
                return;

            if (m_Preferences != null)
            {
                m_Preferences.pressureSmoothing = value;
            }

            UpdatePressureSlider();
        }

        void ColorPickerChanged(Color color)
        {
            if (m_InsideUIUpdate)
                return;

            if (m_Preferences != null)
            {
                m_Preferences.annotationColor = color;
            }

            colorChanged(color);
        }
    }
}
#endif
