using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;

public class BrushSizeUI : MonoBehaviour
{

	public Action<float> onValueChanged { private get; set; }

	[SerializeField]
	private RectTransform m_SliderHandle;

	[SerializeField]
	private Slider m_Slider;

	private Image m_SliderHandleImage;

	private const float kMinSize = 0.25f;
	private const float kMaxSize = 12.5f;

	void Start()
	{
		m_SliderHandleImage = m_SliderHandle.GetComponent<Image>();
	}

	public void OnSliderValueChanged(float value)
	{
		m_SliderHandle.localScale = Vector3.one * Mathf.Lerp(kMinSize, kMaxSize, value);

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
