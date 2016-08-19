using System;
using UnityEngine;
using UnityEngine.UI;

public class ZoomSliderUI : MonoBehaviour
{
	public Slider zoomSlider { get { return m_ZoomSlider; } }
	public Slider m_ZoomSlider;
	public Action<float> OnZoomSlider { private get; set; }

	public void ZoomSlider(float value)
	{
		OnZoomSlider(value);
	}
}