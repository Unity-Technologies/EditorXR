#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class BrushSizeUI : MonoBehaviour
{
    const float k_MinSize = 0.625f;
    const float k_MaxSize = 12.5f;

    [SerializeField]
    RectTransform m_SliderHandle;

    [SerializeField]
    Slider m_Slider;

    Image m_SliderHandleImage;

    public Action<float> onValueChanged { private get; set; }

    void Awake()
    {
        // We record property modifications on creation and modification of these UI elements, which will look odd when undone
        Undo.postprocessModifications += PostProcessModifications;
        m_SliderHandleImage = m_SliderHandle.GetComponent<Image>();
    }

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
#endif
