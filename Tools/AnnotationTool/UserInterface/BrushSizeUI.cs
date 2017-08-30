#if UNITY_EDITOR
using System;
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
		// HACK: Can't modify UI object without pushing an Undo state for it.
		// Without this,  we will undo changes to the handle scale while undoing annotations
		Undo.undoRedoPerformed += () => m_Slider.value = m_Slider.value;
	}

	void Start()
	{
		m_SliderHandleImage = m_SliderHandle.GetComponent<Image>();
	}

	public void OnSliderValueChanged(float value)
	{
		//ScalelHandle(value);

		if (onValueChanged != null)
			onValueChanged(value);
	}

	void ScalelHandle(float value)
	{
		m_SliderHandle.localScale = Vector3.one * Mathf.Lerp(k_MinSize, k_MaxSize, value);
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
