using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.VR.Modules;
using UnityEngine.VR.Utilities;

public class NumericInputField : InputField
{
	public SelectionFlags selectionFlags { get { return m_SelectionFlags; } set { m_SelectionFlags = value; } }
	[SerializeField]
	[FlagsProperty]
	private SelectionFlags m_SelectionFlags = SelectionFlags.Ray | SelectionFlags.Direct;

	public Func<NumericKeyboardUI> keyboard;
	private NumericKeyboardUI m_Keyboard;

	private string m_String;
	private bool m_Open;

	protected override void OnEnable()
	{
		onEndEdit.AddListener(Close);
	}

	protected override void OnDisable()
	{
		onEndEdit.RemoveListener(Close);
	}

	/*
	public void SetPlaceholderText(string placeholderString)
	{
		var placeholderText = m_Placeholder.GetComponent<Text>();

		if (placeholderText != null)
		{
			placeholderText.text = placeholderString;
		}
	}
	*/

	public override void OnPointerClick(PointerEventData eventData)
	{
		var rayEventData = eventData as RayEventData;
		if (rayEventData == null || U.UI.IsValidEvent(rayEventData, selectionFlags))
		{
			base.OnPointerClick(eventData);

			if (m_Open)
				Close();
			else
				Open();
		}
	}

	public override void OnPointerEnter(PointerEventData eventData)
	{
		var rayEventData = eventData as RayEventData;
		if (rayEventData == null || U.UI.IsValidEvent(rayEventData, selectionFlags))
			base.OnPointerEnter(eventData);
	}

	public override void OnPointerExit(PointerEventData eventData)
	{
		var rayEventData = eventData as RayEventData;
		if (rayEventData == null || U.UI.IsValidEvent(rayEventData, selectionFlags))
			base.OnPointerExit(eventData);
	}

	public override void OnPointerDown(PointerEventData eventData)
	{
		var rayEventData = eventData as RayEventData;
		if (rayEventData == null || U.UI.IsValidEvent(rayEventData, selectionFlags))
			base.OnPointerDown(eventData);
	}

	public override void OnPointerUp(PointerEventData eventData)
	{
		var rayEventData = eventData as RayEventData;
		if (rayEventData == null || U.UI.IsValidEvent(rayEventData, selectionFlags))
			base.OnPointerUp(eventData);
	}

	public override void OnSubmit(BaseEventData eventData)
	{
		var rayEventData = eventData as RayEventData;
		if (rayEventData == null || U.UI.IsValidEvent(rayEventData, selectionFlags))
			base.OnSubmit(eventData);
	}

	public override void OnSelect(BaseEventData eventData)
	{
		//
	}

	public override void OnBeginDrag(PointerEventData eventData)
	{
		var rayEventData = eventData as RayEventData;
		if (rayEventData == null || U.UI.IsValidEvent(rayEventData, selectionFlags))
			base.OnBeginDrag(eventData);
	}

	public override void OnDrag(PointerEventData eventData)
	{
		var rayEventData = eventData as RayEventData;
		if (rayEventData == null || U.UI.IsValidEvent(rayEventData, selectionFlags))
		{
			base.OnDrag(eventData);
			DragNumericValue(rayEventData);
		}
	}

	public override void OnEndDrag(PointerEventData eventData)
	{
		var rayEventData = eventData as RayEventData;
		Debug.Log(rayEventData);
		if (rayEventData == null || U.UI.IsValidEvent(rayEventData, selectionFlags))
			base.OnEndDrag(eventData);
	}

	void DragNumericValue(RayEventData rayEventData)
	{
		float num;
		if (!float.TryParse(m_Text, out num))
			num = 0f;
		num += rayEventData.delta.x / 100f;
		m_Text = num.ToString();
	}

//	void UpdateLinearMapping(RayEventData rayEventData)
//	{
//		var direction = transform.right;
//		float length = direction.magnitude;
//		direction.Normalize();
//
//		var displacement = rayEventData.delta.x
//
//		float pull = Mathf.Clamp01(Vector3.Dot(displacement, direction) / length);
//
//		linearMapping.value = pull;
//
//		if (repositionGameObject)
//		{
//			transform.position = Vector3.Lerp(startPosition.position, endPosition.position, pull);
//		}
//	}

	void Open()
	{
		if (m_Open) return;

		m_Open = true;

		m_String = m_Text;

		m_Keyboard = keyboard();
		// Instantiate keyboard here
		if (m_Keyboard != null)
		{
			m_Keyboard.gameObject.SetActive(true);
			m_Keyboard.transform.SetParent(transform, false);
			m_Keyboard.transform.localPosition = Vector3.up * 0.1f;
			m_Keyboard.transform.localRotation = Quaternion.identity;

			m_Keyboard.Setup(new char[] {'0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '.'}, OnKeyPress);
		}
	}

	void Close(string inputString = "")
	{
		m_Open = false;

		if (m_Keyboard == null) return;

		m_Keyboard.gameObject.SetActive(true);
		m_Keyboard = null;
	}

	void OnKeyPress(char keyChar)
	{
		m_String += keyChar;
		m_Text = m_String;
	}
}
