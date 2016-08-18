using System;
using UnityEngine;
using UnityEngine.UI;

public class ChessboardUI : MonoBehaviour
{
	public Slider zoomSlider;
	public Action<float> OnZoomSlider { private get; set; }

	public void ZoomSlider(float value)
	{
		OnZoomSlider(value);
	}
}