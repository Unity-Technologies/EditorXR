using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR.Handles;
using UnityEngine.VR.Utilities;
using UnityEngine.VR.Extensions;

public class KeyboardUI : MonoBehaviour
{
	const float kDragWaitTime = 0.2f;
	const float kKeyLayoutTransitionTime = 0.5f;
	const float kKeyExpandCollapseTime = 0.25f;
	const float kHandleChangeColorTime = 0.1f;
	const float kHorizontalThreshold = 0.7f;
	static Color sHandleDragColor = UnityBrandColorScheme.green;

	[SerializeField]
	List<KeyboardButton> m_Buttons = new List<KeyboardButton>();

	[SerializeField]
	List<Transform> m_VerticalLayoutTransforms = new List<Transform>();

	[SerializeField]
	List<Transform> m_HorizontalLayoutTransforms = new List<Transform>();

	[SerializeField]
	DirectManipulator m_DirectManipulator;

	bool m_EligibleForDrag;
	bool m_CurrentlyHorizontal;
	Material m_HandleMaterial;

	Coroutine m_ChangeDragColorsCoroutine;
	Coroutine m_MoveKeysCoroutine;
	Coroutine m_DragAfterDelayCoroutine;
	Coroutine m_SetButtonTextAlphaCoroutine;
	float m_CurrentButtonAlpha;

	public KeyboardButton handleButton { get; set; }

	public bool collapsing { get; set; }

	/// <summary>
	/// Initialize the keyboard and its buttons
	/// </summary>
	/// <param name="keyPress"></param>
	public void Setup(Action<char> keyPress)
	{
		m_DirectManipulator.target = transform;
		m_DirectManipulator.translate = Translate;
		m_DirectManipulator.rotate = Rotate;

		foreach (var handle in m_DirectManipulator.GetComponentsInChildren<BaseHandle>(true))
		{
			handle.dragStarted += OnDragStarted;
			handle.dragging += OnDrag;
			handle.dragEnded += OnDragEnded;
		}

		foreach (var button in m_Buttons)
		{
			button.Setup(keyPress, IsHorizontal, InTransition);
		}

		m_HandleMaterial = handleButton.targetMeshMaterial;

		var horizontal = IsHorizontal();
		SetButtonLayoutTargets(horizontal);

		this.StopCoroutine(ref m_MoveKeysCoroutine);
		m_MoveKeysCoroutine = StartCoroutine(MoveKeysToLayoutPositions(kKeyExpandCollapseTime, true));
	}

	void Awake()
	{
		handleButton = m_DirectManipulator.GetComponent<KeyboardButton>();
	}

	void OnEnable()
	{
		m_EligibleForDrag = false;

		this.StopCoroutine(ref m_SetButtonTextAlphaCoroutine);
		m_SetButtonTextAlphaCoroutine = StartCoroutine(SetButtonTextAlpha());
	}

	bool InTransition()
	{
		return collapsing || m_EligibleForDrag;
	}

	void SetButtonLayoutTargets(bool horizontal)
	{
		int i = 0;
		foreach (var button in m_Buttons)
		{
			var hT = m_HorizontalLayoutTransforms[i];
			var vT = m_VerticalLayoutTransforms[i];

			var target = horizontal
				? hT
				: vT;
			button.smoothMotion.SetTarget(target);

			i++;
		}
	}

	public void Expand()
	{
		
	}

	public void Collapse(Action doneCollapse)
	{
		//		EnableMallet(false);

		this.StopCoroutine(ref m_SetButtonTextAlphaCoroutine);
		m_SetButtonTextAlphaCoroutine = StartCoroutine(ClearButtonTextAlpha());

		if (isActiveAndEnabled)
		{
			collapsing = true;
			StartCoroutine(CollapseOverTime(doneCollapse));
		}
		else
		{
			collapsing = false;
			doneCollapse();
		}
	}

	IEnumerator CollapseOverTime(Action doneCollapse)
	{
		var t = 0f;
		while (t < kKeyLayoutTransitionTime)
		{
			foreach (var button in m_Buttons)
			{
				if (button != handleButton)
					button.transform.position = Vector3.Lerp(button.transform.position, handleButton.transform.position, 
						t / kKeyLayoutTransitionTime);
			}
			t += Time.unscaledDeltaTime;
			yield return null;
		}

		collapsing = false;
		doneCollapse();
	}

	/// <summary>
	/// Activate shift mode on a button
	/// </summary>
	public void ActivateShiftModeOnKey(KeyboardButton key)
	{
		foreach (var button in m_Buttons)
		{
			if (button == key)
				button.SetShiftModeActive(true);
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

	/// <summary>
	/// Deactivate shift mode on a button
	/// </summary>
	public void DeactivateShiftModeOnKey(KeyboardButton key)
	{
		foreach (var button in m_Buttons)
		{
			if (button == key)
				button.SetShiftModeActive(false);
		}
	}

	/// <summary>
	/// Check if the keyboard is horizontal
	/// </summary>
	/// <returns>Is the keyboard horizontal?</returns>
	bool IsHorizontal()
	{
		return Vector3.Dot(transform.up, Vector3.up) < kHorizontalThreshold
			&& Vector3.Dot(transform.forward, Vector3.up) < 0f;
	}

	IEnumerator MoveKeysToLayoutPositions(float duration = kKeyLayoutTransitionTime, bool setKeyTextAlpha = false)
	{
		var horizontal = IsHorizontal();
		var t = 0f;
		while (t < duration)
		{
			int i = 0;
			foreach (var button in m_Buttons)
			{
				var targetPos = horizontal
					? m_HorizontalLayoutTransforms[i].position
					: m_VerticalLayoutTransforms[i].position;
				button.transform.position = Vector3.Lerp(button.transform.position, targetPos, t / duration);
				i++;
			}
			t += Time.unscaledDeltaTime;
			yield return null;
		}

		int k = 0;
		foreach (var button in m_Buttons)
		{
			var targetPos = horizontal
				? m_HorizontalLayoutTransforms[k].position
				: m_VerticalLayoutTransforms[k].position;
			button.transform.position = targetPos;
			k++;
		}

		m_MoveKeysCoroutine = null;
	}

	/// <summary>
	/// Instantly move all keys to vertical layout positions
	/// </summary>
	public void ForceMoveButtonsToVerticalLayout()
	{
		int i = 0;
		foreach (var button in m_Buttons)
		{
			var t = m_VerticalLayoutTransforms[i];
			if (t)
			{
				button.transform.position = m_VerticalLayoutTransforms[i].position;
			}
			i++;
		}
	}

	/// <summary>
	/// Instantly move all keys to horizontal layout positions
	/// </summary>
	public void ForceMoveButtonsToHorizontalLayout()
	{
		int i = 0;
		foreach (var button in m_Buttons)
		{
			var t = m_HorizontalLayoutTransforms[i];
			if (t)
			{
				button.transform.position = t.position;
			}
			i++;
		}
	}

	private void Translate(Vector3 deltaPosition)
	{
		if (m_EligibleForDrag)
			transform.position += deltaPosition;
	}

	private void Rotate(Quaternion deltaRotation)
	{
		if (m_EligibleForDrag)
			transform.rotation *= deltaRotation;
	}

	void OnDragStarted(BaseHandle baseHandle, HandleEventData handleEventData)
	{
		this.StopCoroutine(ref m_DragAfterDelayCoroutine);
		m_DragAfterDelayCoroutine = StartCoroutine(DragAfterDelay());
	}

	IEnumerator DragAfterDelay()
	{
		var t = 0f;
		while (t < kDragWaitTime)
		{
			t += Time.unscaledDeltaTime;
			yield return null;
		}

		m_EligibleForDrag = true;
		m_DragAfterDelayCoroutine = null;

		StartDrag();
	}

	void StartDrag()
	{
		this.StopCoroutine(ref m_ChangeDragColorsCoroutine);
		m_ChangeDragColorsCoroutine = StartCoroutine(SetDragColors());

		this.StopCoroutine(ref m_SetButtonTextAlphaCoroutine);
		m_SetButtonTextAlphaCoroutine = StartCoroutine(ClearButtonTextAlpha());

		foreach (var button in m_Buttons)
		{
			button.smoothMotion.enabled = true;
		}
	}

	IEnumerator SetDragColors()
	{
		if (!gameObject.activeInHierarchy) yield break;
		var t = 0f;
		var startColor = m_HandleMaterial.color;
		while (t < kHandleChangeColorTime)
		{
			var alpha = t / kHandleChangeColorTime;
			m_HandleMaterial.color = Color.Lerp(startColor, sHandleDragColor, alpha);
			t += Time.unscaledDeltaTime;
			yield return null;
		}

		m_HandleMaterial.color = sHandleDragColor;

		m_ChangeDragColorsCoroutine = null;
	}

	IEnumerator UnsetDragColors()
	{
		if (!gameObject.activeInHierarchy) yield break;

		var t = 0f;
		var startColor = m_HandleMaterial.color;
		while (t < kHandleChangeColorTime)
		{
			var alpha = t / kHandleChangeColorTime;
			m_HandleMaterial.color = Color.Lerp(startColor, handleButton.targetMeshBaseColor, alpha);
			t += Time.unscaledDeltaTime;
			yield return null;
		}

		m_HandleMaterial.color = handleButton.targetMeshBaseColor;

		m_ChangeDragColorsCoroutine = null;
	}

	IEnumerator SetButtonTextAlpha()
	{
		float[] startingAlphas = new float[m_Buttons.Count];

		var i = 0;
		foreach (var button in m_Buttons)
		{
			startingAlphas[i] = button.canvasGroup.alpha;
			i++;
		}

		var t = 0f;
		var finalAlpha = 1f;
		while (t < kHandleChangeColorTime)
		{
			i = 0;
			var alpha = t / kHandleChangeColorTime;
			foreach (var button in m_Buttons)
			{
				if (button.canvasGroup.alpha > finalAlpha * alpha)
					button.canvasGroup.alpha = Mathf.Lerp(startingAlphas[i], finalAlpha * alpha, alpha);
				i++;
			}
			t += Time.unscaledDeltaTime;
			yield return null;
		}

		foreach (var button in m_Buttons)
		{
			button.canvasGroup.alpha = finalAlpha;
		}

		m_SetButtonTextAlphaCoroutine = null;
	}

	IEnumerator ClearButtonTextAlpha()
	{
		var t = 0f;
		while (t < kHandleChangeColorTime)
		{
			var alpha = 1f - t / kHandleChangeColorTime;
			foreach (var button in m_Buttons)
			{
				button.canvasGroup.alpha = alpha;
			}
			t += Time.unscaledDeltaTime;
			yield return null;
		}

		foreach (var button in m_Buttons)
		{
			button.canvasGroup.alpha = 0f;
		}

		m_SetButtonTextAlphaCoroutine = null;
	}

	void OnDrag(BaseHandle baseHandle, HandleEventData handleEventData)
	{
		if (m_EligibleForDrag)
		{
			var horizontal = IsHorizontal();
			if (m_CurrentlyHorizontal != horizontal)
			{
				SetButtonLayoutTargets(IsHorizontal());

				this.StopCoroutine(ref m_MoveKeysCoroutine);
				m_MoveKeysCoroutine = StartCoroutine(MoveKeysToLayoutPositions(kKeyExpandCollapseTime, false));
				
				m_CurrentlyHorizontal = horizontal;
			}
		}
	}

	void OnDragEnded(BaseHandle baseHandle, HandleEventData handleEventData)
	{
		this.StopCoroutine(ref m_DragAfterDelayCoroutine);
		m_DragAfterDelayCoroutine = null;

		if (m_EligibleForDrag)
		{
			m_EligibleForDrag = false;

			foreach (var button in m_Buttons)
			{
				button.smoothMotion.enabled = false;
			}

			this.StopCoroutine(ref m_SetButtonTextAlphaCoroutine);
			m_SetButtonTextAlphaCoroutine = StartCoroutine(SetButtonTextAlpha());

			this.StopCoroutine(ref m_ChangeDragColorsCoroutine);
			if (isActiveAndEnabled)
				m_ChangeDragColorsCoroutine = StartCoroutine(UnsetDragColors());
		}
	}

	void OnDisable()
	{
		this.StopCoroutine(ref m_ChangeDragColorsCoroutine);
		m_ChangeDragColorsCoroutine = null;
	}

	public bool ShouldShowMallet(Transform rayOrigin)
	{
		if (!isActiveAndEnabled || !IsHorizontal())
			return false;

		var rayOriginPos = rayOrigin.position;

		var grabbingHandle = false;
		var far = false;
		var invalidOrientation = false;

		const float nearDist = 0.06f;
		const float handleGrabAngle = 0.5f;
		if ((transform.position - rayOriginPos).magnitude < nearDist
			&& Vector3.Dot(handleButton.transform.up, rayOrigin.forward) < handleGrabAngle)
			grabbingHandle = true;

		const float farDist = 0.18f;
		if ((transform.position - rayOriginPos).magnitude > farDist)
			far = true;

		if (Vector3.Dot(handleButton.transform.up, rayOrigin.forward) < 0.5f)
			invalidOrientation = true;

		return !(grabbingHandle || far || invalidOrientation);
	}
}