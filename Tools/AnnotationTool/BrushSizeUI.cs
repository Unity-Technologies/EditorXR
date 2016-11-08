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

	private const float kMinSize = 0.25f;
	private const float kMaxSize = 12.5f;

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

}
