using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;
using UnityEngine.EventSystems;

public class ColorPickerUI : MonoBehaviour
{

	public Transform toolRayOrigin { private get; set; }

	public Action<Color> onColorPicked { private get; set; }

	[SerializeField]
	RawImage m_ColorPicker;

	[SerializeField]
	Slider m_BrightnessSlider;

	[SerializeField]
	RawImage m_SliderBackground;

	[SerializeField]
	RectTransform m_Picker;

	Vector3 m_PickerTargetPosition;

	[SerializeField]
	ColorPickerSquareUI m_ColorPickerSquare;

	Texture2D m_BrightnessBarTexture;
	Texture2D m_ColorPickerTexture;

	void Start()
	{
		m_ColorPickerSquare.onDrag = OnDrag;

		GenerateColorPicker();
		GenerateBrightnessBar();
	}

	void OnDestroy()
	{
		DestroyImmediate(m_BrightnessBarTexture);
		DestroyImmediate(m_ColorPickerTexture);
	}

	void Update()
	{
		m_Picker.localPosition = m_PickerTargetPosition;
	}

	public void OnSliderChanged(float val)
	{
		m_ColorPicker.color = Color.Lerp(Color.black, Color.white, val);
		PositionToColor();
	}

	void OnDrag()
	{
		if (toolRayOrigin)
		{
			var worldToLocal = m_ColorPicker.rectTransform.worldToLocalMatrix;
			var rect = m_ColorPicker.rectTransform.rect;

			var localRayPos = worldToLocal.MultiplyPoint3x4(toolRayOrigin.position);
			var localRayForward = worldToLocal.MultiplyVector(toolRayOrigin.forward).normalized;
			
			float height = localRayPos.z;
			float angle = Vector3.Angle(new Vector3(0, 0, height), localRayForward);
			float sine = Mathf.Sin((90 - angle) * Mathf.Deg2Rad);

			float distance = Mathf.Abs(height / sine);
			Vector2 point = localRayPos + localRayForward * distance;
			point.x = Mathf.Clamp(point.x, -rect.width / 2f, rect.width / 2f);
			point.y = Mathf.Clamp(point.y, -rect.height / 2f, rect.height / 2f);
			
			m_PickerTargetPosition = point;
			GenerateBrightnessBar();
			PositionToColor();
		}
	}

	private void GenerateBrightnessBar()
	{
		var rect = m_SliderBackground.rectTransform.rect;
		if (!m_BrightnessBarTexture)
			m_BrightnessBarTexture = new Texture2D((int)rect.width, 1);
		
		var squareRect = m_ColorPicker.rectTransform.rect;
		var pickerPos = m_Picker.localPosition;

		float hue = (pickerPos.x + squareRect.width / 2f) / squareRect.width;
		float saturation = (pickerPos.y + squareRect.height / 2f) / squareRect.height;

		for (int y = 0; y < m_BrightnessBarTexture.height; y++)
		{
			for (int x = 0; x < m_BrightnessBarTexture.width; x++)
			{
				float brightness = x / rect.width;
				m_BrightnessBarTexture.SetPixel(x, y, Color.HSVToRGB(hue, saturation, brightness));
			}
		}
		m_BrightnessBarTexture.Apply();

		m_SliderBackground.texture = m_BrightnessBarTexture;
	}

	private void GenerateColorPicker()
	{
		var rect = m_ColorPicker.rectTransform.rect;
		if (!m_ColorPickerTexture)
			m_ColorPickerTexture = new Texture2D((int)rect.width, (int)rect.height);
		
		for (int y = 0; y < m_ColorPickerTexture.height; y++)
		{
			for (int x = 0; x < m_ColorPickerTexture.width; x++)
			{
				float hue = x / rect.width;
				float saturation = y / rect.height;
				m_ColorPickerTexture.SetPixel(x, y, Color.HSVToRGB(hue, saturation, 1));
			}
		}
		m_ColorPickerTexture.Apply();

		m_ColorPicker.texture = m_ColorPickerTexture;
	}

	private void PositionToColor()
	{
		var rect = m_ColorPicker.rectTransform.rect;

		float hue = (m_PickerTargetPosition.x + rect.width / 2f) / rect.width;
		float saturation = (m_PickerTargetPosition.y + rect.height / 2f) / rect.height;
		float brightness = m_BrightnessSlider.value;
		
		Color col = Color.HSVToRGB(hue, saturation, brightness);
		if (onColorPicked != null)
			onColorPicked(col);
	}
	
}
