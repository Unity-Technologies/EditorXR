using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;
using UnityEngine.EventSystems;

public class ColorPickerUI : MonoBehaviour, IPointerExitHandler
{
	[SerializeField]
	float m_FadeTime;

	[SerializeField]
	RawImage m_ColorPicker;

	[SerializeField]
	RawImage m_SliderBackground;

	[SerializeField]
	RectTransform m_Picker;

	[SerializeField]
	ColorPickerSquareUI m_ColorPickerSquare;

	Vector3 m_PickerTargetPosition;

	Texture2D m_BrightnessBarTexture;
	Texture2D m_ColorPickerTexture;

	Coroutine m_FadeCoroutine;

	public Transform toolRayOrigin { private get; set; }

	public Action<Color> onColorPicked { private get; set; }

	public Action onHideCalled { private get; set; }

	void Start()
	{
		m_ColorPickerSquare.onDrag = OnDrag;

		m_ColorPickerTexture = m_ColorPickerSquare.GetComponent<RawImage>().texture as Texture2D;
		GenerateBrightnessBar();
	}

	void OnDestroy()
	{
		DestroyImmediate(m_BrightnessBarTexture);
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

	IEnumerator FadeCanvas(bool fadeOut)
	{
		enabled = !fadeOut;
		m_Picker.localPosition = m_PickerTargetPosition;

		var canvasGroup = GetComponentInChildren<CanvasGroup>();
		var current = canvasGroup.alpha;
		var start = fadeOut ? 1 : 0;
		var target = 1 - start;

		if (current == target)
		{
			m_FadeCoroutine = null;
			yield break;
		}

		var ratio = fadeOut ? 1 - current : current;
		while (ratio < 1)
		{
			canvasGroup.alpha = Mathf.Lerp(start, target, ratio);
			ratio += Time.unscaledDeltaTime / m_FadeTime;
			yield return null;
		}

		GetComponentInChildren<Collider>().enabled = !fadeOut;
		m_Picker.gameObject.SetActive(!fadeOut);
		canvasGroup.alpha = target;
		canvasGroup.interactable = !fadeOut;
		canvasGroup.blocksRaycasts = !fadeOut;
		m_FadeCoroutine = null;
	}

	void OnDrag()
	{
		if (toolRayOrigin && enabled)
		{
			var worldToLocal = m_ColorPicker.rectTransform.worldToLocalMatrix;
			var rect = m_ColorPicker.rectTransform.rect;

			var localRayPos = worldToLocal.MultiplyPoint3x4(toolRayOrigin.position);
			var localRayForward = worldToLocal.MultiplyVector(toolRayOrigin.forward).normalized;
			
			var height = localRayPos.z;
			var angle = Vector3.Angle(new Vector3(0, 0, height), localRayForward);
			var sine = Mathf.Sin((90 - angle) * Mathf.Deg2Rad);

			var distance = Mathf.Abs(height / sine);
			var point = localRayPos + localRayForward * distance;
			point = point.normalized * Mathf.Min(point.magnitude, rect.width / 2f);
			
			m_PickerTargetPosition = point;
			GenerateBrightnessBar();
			PositionToColor();
		}
	}

	void GenerateBrightnessBar()
	{
		var rect = m_SliderBackground.rectTransform.rect;
		if (!m_BrightnessBarTexture)
			m_BrightnessBarTexture = new Texture2D((int)rect.width, 1);

		var col = GetColorForCurrentPosition();

		for (var y = 0; y < m_BrightnessBarTexture.height; y++)
		{
			for (var x = 0; x < m_BrightnessBarTexture.width; x++)
			{
				var brightness = x / rect.width;
				var tempCol = col * brightness;
				tempCol.a = 1;
				m_BrightnessBarTexture.SetPixel(x, y, tempCol);
			}
		}
		m_BrightnessBarTexture.Apply();

		m_SliderBackground.texture = m_BrightnessBarTexture;
	}

	void PositionToColor()
	{
		if (onColorPicked != null)
		{
			var col = GetColorForCurrentPosition();
			col *= m_ColorPicker.color; // Apply brightness slider
			onColorPicked(col);
		}
	}

	Color GetColorForCurrentPosition()
	{
		var rect = m_ColorPicker.rectTransform.rect;

		var dir = m_PickerTargetPosition;
		var x = (dir.x + rect.width / 2f) / rect.width;
		var y = (dir.y + rect.height / 2f) / rect.height;
		var textureX = (int)(x * m_ColorPickerTexture.width);
		var textureY = (int)(y * m_ColorPickerTexture.height);
		var col = m_ColorPickerTexture.GetPixel(textureX, textureY);

		return col;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		if (onHideCalled != null)
			onHideCalled();
	}
}
