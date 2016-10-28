using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR.Handles;
using UnityEngine.VR.Utilities;

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

	/// <summary>
	/// Called when the mallet should be shown/hidden
	/// </summary>
	public event Action<bool> malletVisibilityChanged = delegate { };

	bool m_EligibleForDrag;
	bool m_CurrentlyHorizontal;
	Material m_HandleMaterial;

	Coroutine m_ChangeDragColorsCoroutine;
	Coroutine m_MoveKeysCoroutine;
	Coroutine m_DragAfterDelayCoroutine;

	Transform cachedRayOrigin;
	bool m_MalletVisible;

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
			button.Setup(keyPress, IsHorizontal);
		}

		m_HandleMaterial = handleButton.targetMeshMaterial;

		var horizontal = IsHorizontal();
		SetButtonLayoutTargets(horizontal);
		malletVisibilityChanged(horizontal);
		m_MalletVisible = horizontal;

		if (m_MoveKeysCoroutine != null)
			StopCoroutine(m_MoveKeysCoroutine);
		m_MoveKeysCoroutine = StartCoroutine(MoveKeysToLayoutPositions(kKeyExpandCollapseTime, true));
	}

	void Awake()
	{
		handleButton = m_DirectManipulator.GetComponent<KeyboardButton>();
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

	public void Collapse(Action doneCollapse)
	{
		if (m_MalletVisible)
			malletVisibilityChanged(false);
		m_MalletVisible = false;

		if (isActiveAndEnabled)
		{
			collapsing = true;
			StartCoroutine(CollapseOverTime(doneCollapse));
		}
		else
		{
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
				{
					button.transform.position = Vector3.Lerp(button.transform.position, handleButton.transform.position, t / kKeyLayoutTransitionTime);
					SetButtonTextAlpha(1f - t / kKeyLayoutTransitionTime);
				}
			}
			t += Time.unscaledDeltaTime;
			yield return null;
		}

		doneCollapse();

		collapsing = false;
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
				var horizT = m_HorizontalLayoutTransforms[i];
				var vertT = m_VerticalLayoutTransforms[i];
				button.transform.position = Vector3.Lerp(button.transform.position, horizontal
					? horizT.position
					: vertT.position, t / duration);
				i++;
			}
			t += Time.unscaledDeltaTime;
			yield return null;
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
		if (m_DragAfterDelayCoroutine != null)
			StopCoroutine(m_DragAfterDelayCoroutine);
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
		if (m_ChangeDragColorsCoroutine != null)
			StopCoroutine(m_ChangeDragColorsCoroutine);
		m_ChangeDragColorsCoroutine = StartCoroutine(SetDragColors());

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
			SetButtonTextAlpha(1f - alpha);
			t += Time.unscaledDeltaTime;
			yield return null;
		}

		SetButtonTextAlpha(0f);
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
			SetButtonTextAlpha(alpha);
			t += Time.unscaledDeltaTime;
			yield return null;
		}

		SetButtonTextAlpha(1f);
		m_HandleMaterial.color = handleButton.targetMeshBaseColor;

		m_ChangeDragColorsCoroutine = null;
	}

	void SetButtonTextAlpha(float alpha)
	{
		foreach (var button in m_Buttons)
		{
			var color = button.textComponent.color;
			button.textComponent.color = new Color(color.r, color.g, color.b, alpha);
		}
	}

	void OnDrag(BaseHandle baseHandle, HandleEventData handleEventData)
	{
		if (m_EligibleForDrag)
		{
			var horizontal = IsHorizontal();
			if (m_CurrentlyHorizontal != horizontal)
			{
				SetButtonLayoutTargets(IsHorizontal());

				if (m_MoveKeysCoroutine != null)
					StopCoroutine(m_MoveKeysCoroutine);
				m_MoveKeysCoroutine = StartCoroutine(MoveKeysToLayoutPositions(kKeyExpandCollapseTime, true));
				m_CurrentlyHorizontal = horizontal;
			}
		}
	}

	void OnDragEnded(BaseHandle baseHandle, HandleEventData handleEventData)
	{
		if (m_DragAfterDelayCoroutine != null)
		{
			StopCoroutine(m_DragAfterDelayCoroutine);
			m_DragAfterDelayCoroutine = null;
		}

		if (m_EligibleForDrag)
		{
			m_EligibleForDrag = false;

			foreach (var button in m_Buttons)
			{
				button.smoothMotion.enabled = false;
			}

			if (m_ChangeDragColorsCoroutine != null)
				StopCoroutine(m_ChangeDragColorsCoroutine);

			if (isActiveAndEnabled)
				m_ChangeDragColorsCoroutine = StartCoroutine(UnsetDragColors());

			cachedRayOrigin = handleEventData.rayOrigin;
		}
	}


	void Update()
	{
		if (IsHorizontal() && cachedRayOrigin != null)
		{
			var rayOriginPos = cachedRayOrigin.position;
			if (Vector3.Magnitude(handleButton.transform.position - rayOriginPos) < 0.03f)
			{
				if (m_MalletVisible)
					malletVisibilityChanged(false);
				m_MalletVisible = false;
			}
			else
			{
				if (!m_MalletVisible)
					malletVisibilityChanged(true);
				m_MalletVisible = true;
			}
		}
	}

	void OnDisable()
	{
		if (m_ChangeDragColorsCoroutine != null)
			StopCoroutine(m_ChangeDragColorsCoroutine);
		m_ChangeDragColorsCoroutine = null;
	}
}