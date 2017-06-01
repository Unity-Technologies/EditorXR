using System;
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

	void Start()
	{
		m_SliderHandleImage = m_SliderHandle.GetComponent<Image>();
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
