using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR.Handles;
using UnityEngine.VR.Utilities;

public class KeyboardUI : MonoBehaviour
{
	const float kDragWaitTime = 0.2f;
	const float kHandleChangeColorTime = 0.1f;
	const float kHorizontalThreshold = 0.7f;

	[SerializeField]
	List<KeyboardButton> m_Buttons = new List<KeyboardButton>();

	[SerializeField]
	List<Transform> m_VerticalLayoutTransforms = new List<Transform>();

	[SerializeField]
	List<Transform> m_HorizontalLayoutTransforms = new List<Transform>();

	[SerializeField]
	DirectManipulator m_DirectManipulator;

	Color m_DragColor = UnityBrandColorScheme.green;

	/// <summary>
	/// Called when the orientation changes, parameter is whether keyboard is currently horizontal
	/// </summary>
	public event Action<bool> orientationChanged = delegate {};

	Color m_BaseColor;
	bool m_AllowDragging;
	bool m_Horizontal;
	Material m_HandleMaterial;

    KeyboardButton m_HandleButton;

	Coroutine m_ChangeDragColorsCoroutine;
	Coroutine m_MoveKeysCoroutine;
	Coroutine m_DragCoroutine;

	/// <summary>
	/// Initialize the keyboard and its buttons
	/// </summary>
	/// <param name="keyPress"></param>
	public void Setup(Action<char> keyPress)
	{
	    Debug.Log("licer");
		m_DirectManipulator.target = transform;
		m_DirectManipulator.translate = Translate;
		m_DirectManipulator.rotate = Rotate;

	    m_HandleButton = m_DirectManipulator.GetComponent<KeyboardButton>();
	    m_HandleMaterial = m_HandleButton.targetMeshMaterial;
	    m_BaseColor = m_HandleMaterial.color;

		foreach (var handle in m_DirectManipulator.GetComponentsInChildren<BaseHandle>(true))
		{
			handle.dragStarted += OnDragStarted;
			handle.dragEnded += OnDragEnded;
		}

		foreach (var button in m_Buttons)
		{
			button.Setup(keyPress, IsHorizontal);
		}
	}

	/// <summary>
	/// Activate shift mode on all buttons
	/// </summary>
	public void ActivateShiftModeOnKeys()
	{
		foreach (var button in m_Buttons)
			button.SetShiftModeActive(true);
	}

	/// <summary>
	/// Deactivate shift mode on all buttons
	/// </summary>
	public void DeactivateShiftModeOnKeys()
	{
		foreach (var button in m_Buttons)
			button.SetShiftModeActive(false);
	}

	private bool IsHorizontal()
	{
		var horizontal = Vector3.Dot(transform.up, Vector3.up) < kHorizontalThreshold;
		if (m_Horizontal != horizontal)
		{
			m_Horizontal = horizontal;

			if (m_MoveKeysCoroutine != null)
				StopCoroutine(m_MoveKeysCoroutine);
			StartCoroutine(MoveKeysToNewPosition());
		}

		return m_Horizontal;
	}

	IEnumerator MoveKeysToNewPosition()
	{
		var t = 0f;
		while (t < 0.5f)
		{
			int i = 0;
			foreach (var button in m_Buttons)
			{
				button.transform.position = Vector3.Lerp(button.transform.position, m_Horizontal
					? m_HorizontalLayoutTransforms[i].position
					: m_VerticalLayoutTransforms[i].position, t / 0.5f);

				var target = m_Horizontal
				? m_HorizontalLayoutTransforms[i]
				: m_VerticalLayoutTransforms[i];
				button.smoothMotion.SetTarget(target);

				i++;
			}
			t += Time.unscaledDeltaTime;
			yield return null;
		}

		int k = 0;
		foreach (var button in m_Buttons)
		{
			var target = m_Horizontal
				? m_HorizontalLayoutTransforms[k]
				: m_VerticalLayoutTransforms[k];
			button.smoothMotion.SetTarget(target);
		}

		m_MoveKeysCoroutine = null;
	}

	private void Translate(Vector3 deltaPosition)
	{
		if (m_AllowDragging)
			transform.position += deltaPosition;
	}

	private void Rotate(Quaternion deltaRotation)
	{
		if (m_AllowDragging)
			transform.rotation *= deltaRotation;
	}

	void OnDragStarted(BaseHandle baseHandle, HandleEventData handleEventData)
	{
		if (m_DragCoroutine != null)
			StopCoroutine(m_DragCoroutine);

		m_DragCoroutine = StartCoroutine(DragAfterDelay());
	}

	IEnumerator DragAfterDelay()
	{
		var t = 0f;
		while (t < kDragWaitTime)
		{
			t += Time.unscaledDeltaTime;
			yield return null;
		}
		StartDrag();
	}

	void StartDrag()
	{
		m_AllowDragging = true;

		if (m_ChangeDragColorsCoroutine != null)
			StopCoroutine(m_ChangeDragColorsCoroutine);
		m_ChangeDragColorsCoroutine = StartCoroutine(SetDragColors());

		int i = 0;
		foreach (var button in m_Buttons)
		{
			button.smoothMotion.enabled = true;
			i++;
		}

		m_DragCoroutine = null;
	}

	IEnumerator SetDragColors()
	{
		if (!gameObject.activeInHierarchy) yield break;

		var t = 0f;
		var startColor = m_HandleMaterial.color;
		while (t < kHandleChangeColorTime)
		{
			m_HandleMaterial.color = Color.Lerp(startColor, m_DragColor, t / kHandleChangeColorTime);
			t += Time.unscaledDeltaTime;

			foreach (var button in m_Buttons)
			{
				var color = button.textComponent.color;
				button.textComponent.color = new Color(color.r, color.g, color.b, 1f- t / kHandleChangeColorTime);
			}

			yield return null;
		}
		m_HandleMaterial.color = m_DragColor;

		m_ChangeDragColorsCoroutine = null;
	}

	IEnumerator UnsetDragColors()
	{
		if (!gameObject.activeInHierarchy) yield break;

		var t = 0f;
		var startColor = m_HandleMaterial.color;
		while (t < kHandleChangeColorTime)
		{
			m_HandleMaterial.color = Color.Lerp(startColor, m_BaseColor, t / kHandleChangeColorTime);
			t += Time.unscaledDeltaTime;

			foreach (var button in m_Buttons)
			{
				var color = button.textComponent.color;
				button.textComponent.color = new Color(color.r, color.g, color.b, t / kHandleChangeColorTime);
			}

			yield return null;
		}
		m_HandleMaterial.color = m_BaseColor;

		m_ChangeDragColorsCoroutine = null;
	}

	void OnDragEnded(BaseHandle baseHandle, HandleEventData handleEventData)
	{
		m_AllowDragging = false;
		orientationChanged(IsHorizontal());

		foreach (var button in m_Buttons)
		{
			button.smoothMotion.enabled = false;
		}

		if (m_ChangeDragColorsCoroutine != null)
			StopCoroutine(m_ChangeDragColorsCoroutine);
		m_ChangeDragColorsCoroutine = StartCoroutine(UnsetDragColors());
	}

	void OnDisable()
	{
		if (m_ChangeDragColorsCoroutine != null)
			StopCoroutine(m_ChangeDragColorsCoroutine);

		m_HandleMaterial.color = m_BaseColor;
	}

	void OnDestroy()
	{
		U.Object.Destroy(m_HandleMaterial);
	}
}