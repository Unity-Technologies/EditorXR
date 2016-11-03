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
	private float m_FadeTime;

	[SerializeField]
	private RawImage m_ColorPicker;

	[SerializeField]
	private Slider m_BrightnessSlider;

	[SerializeField]
	private RawImage m_SliderBackground;

	[SerializeField]
	private RectTransform m_Picker;

	private Vector3 m_PickerTargetPosition;

	[SerializeField]
	private ColorPickerSquareUI m_ColorPickerSquare;

	private Texture2D m_BrightnessBarTexture;
	private Texture2D m_ColorPickerTexture;

	private Coroutine m_FadeCoroutine;

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

	public void Show()
	{
		if (m_FadeCoroutine != null)
			StopCoroutine(m_FadeCoroutine);

		m_FadeCoroutine = StartCoroutine(FadeCanvas(false));
	}

	public void Hide()
	{
		if (m_FadeCoroutine != null)
			StopCoroutine(m_FadeCoroutine);
		
		m_FadeCoroutine = StartCoroutine(FadeCanvas(true));
	}

	public void OnSliderChanged(float val)
	{
		m_ColorPicker.color = Color.Lerp(Color.black, Color.white, val);
		PositionToColor();
	}

	private IEnumerator FadeCanvas(bool fadeOut)
	{
		enabled = !fadeOut;
		m_Picker.localPosition = m_PickerTargetPosition;

		var canvasGroup = GetComponent<CanvasGroup>();
		float current = canvasGroup.alpha;
		float start = fadeOut ? 1 : 0;
		float target = 1 - start;

		if (current == target)
		{
			m_FadeCoroutine = null;
			yield break;
		}

		float ratio = fadeOut ? 1 - current : current;
		while (ratio < 1)
		{
			canvasGroup.alpha = Mathf.Lerp(start, target, ratio);
			ratio += Time.unscaledDeltaTime / m_FadeTime;
			yield return null;
		}

		canvasGroup.alpha = target;
		m_FadeCoroutine = null;
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
