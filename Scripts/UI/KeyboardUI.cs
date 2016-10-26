using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.VR.Handles;
using UnityEngine.VR.Utilities;

public class KeyboardUI : MonoBehaviour
{
	float kDragWaitTime = 0.2f;
	float kHorizontalThreshold = 0.7f;

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

	Color m_SavedColor;
	bool m_AllowDragging;
	bool m_Horizontal;
	Material m_HandleMaterial;

	/// <summary>
	/// Initialize the keyboard and its buttons
	/// </summary>
	/// <param name="keyPress"></param>
	public void Setup(Action<char> keyPress)
	{
		m_DirectManipulator.target = transform;
		m_DirectManipulator.translate = Translate;
		m_DirectManipulator.rotate = Rotate;

		m_HandleMaterial = U.Material.GetMaterialClone(m_DirectManipulator.GetComponentInChildren<Renderer>(true));
		m_SavedColor = m_HandleMaterial.color;

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

			MoveKeysToNewPosition();
		}

		return m_Horizontal;
	}

	void MoveKeysToNewPosition()
	{

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
		StartCoroutine(WaitForDrag());
	}

	IEnumerator WaitForDrag()
	{
		var t = 0f;
		while (t < kDragWaitTime)
		{
			t += Time.unscaledDeltaTime;
			yield return null;
		}
		m_AllowDragging = true;
//        m_HandleMaterial.color = m_DragColor;

		if (m_IncreaseEmissionCoroutine != null)
			StopCoroutine(m_IncreaseEmissionCoroutine);

		if (m_DecreaseEmissionCoroutine != null)
			StopCoroutine(m_DecreaseEmissionCoroutine);

		m_IncreaseEmissionCoroutine = StartCoroutine(SetHandleColor());
	}

	Coroutine m_IncreaseEmissionCoroutine;
	Coroutine m_DecreaseEmissionCoroutine;

	private IEnumerator SetHandleColor()
	{
		if (!gameObject.activeInHierarchy) yield break;

		var t = 0f;
		Color startColor = m_HandleMaterial.color;
		while (t < 0.05f)
		{
			m_HandleMaterial.color = Color.Lerp(startColor, m_DragColor, t / 0.05f);
			t += Time.unscaledDeltaTime;

			yield return null;
		}
		m_HandleMaterial.color = m_DragColor;

		m_IncreaseEmissionCoroutine = null;
	}

	private IEnumerator UnsetHandleColor()
	{
		if (!gameObject.activeInHierarchy) yield break;

		var t = 0f;
		Color startColor = m_HandleMaterial.color;
		while (t < 0.05f)
		{
			m_HandleMaterial.color = Color.Lerp(startColor, m_SavedColor, t / 0.05f);
			t += Time.unscaledDeltaTime;

			yield return null;
		}
		m_HandleMaterial.color = m_SavedColor;

		m_DecreaseEmissionCoroutine = null;
	}

	void OnDragEnded(BaseHandle baseHandle, HandleEventData handleEventData)
	{
		m_AllowDragging = false;
		orientationChanged(IsHorizontal());

		if (m_IncreaseEmissionCoroutine != null)
			StopCoroutine(m_IncreaseEmissionCoroutine);

		if (m_DecreaseEmissionCoroutine != null)
			StopCoroutine(m_DecreaseEmissionCoroutine);

		m_DecreaseEmissionCoroutine = StartCoroutine(UnsetHandleColor());
	}

	void OnDestroy()
	{
		U.Object.Destroy(m_HandleMaterial);
	}
}