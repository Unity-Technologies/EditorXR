using System;
using UnityEngine;
using UnityEngine.UI;

public class ZoomSliderUI : MonoBehaviour
{
	public Slider zoomSlider { get { return m_ZoomSlider; } }
	[SerializeField]
	private Slider m_ZoomSlider;

	public Action<float> sliding { private get; set; }

	public void ZoomSlider(float value)
	{
		if(sliding != null)
			sliding(value);
	}
}