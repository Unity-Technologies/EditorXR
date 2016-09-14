using System;
using System.Globalization;
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
	protected SelectionFlags m_SelectionFlags = SelectionFlags.Ray | SelectionFlags.Direct;

	public Func<NumericKeyboardUI> keyboard;
	private NumericKeyboardUI m_NumericKeyboard;

	private string m_String;
	private bool m_Open;

	protected override void OnEnable()
	{
		Debug.Log("Enable input field");

		onEndEdit.AddListener(Close);

		characterLimit = 100;
		characterValidation = CharacterValidation.Decimal;

		base.OnEnable();
	}

	protected override void OnDisable()
	{
		Debug.Log("Disable input field");

		base.OnDisable();

		onEndEdit.RemoveListener(Close);
	}

	public override void OnPointerClick(PointerEventData eventData)
	{
		var rayEventData = eventData as RayEventData;
		if (rayEventData == null || U.UI.IsValidEvent(rayEventData, selectionFlags))
		{
//			base.OnPointerClick(eventData);

			if (m_Open)
			{
				Debug.Log("Click to close");
				Close();

			}
			else
			{
				Debug.Log("Click to open");
				Open();
			}
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
		{
//			if (!IsInteractable())
//				return;
		}
	}

	public override void OnDeselect(BaseEventData eventData)
	{
		base.OnDeselect(eventData);
		// TODO this works but need to only deselect when something besides a key button is clicked
//	    Debug.Log("Deselect callled");
//		Close();
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
//			DragNumericValue(rayEventData);
		}
	}

	public override void OnEndDrag(PointerEventData eventData)
	{
		var rayEventData = eventData as RayEventData;
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

		m_NumericKeyboard = keyboard();
		// Instantiate keyboard here
		if (m_NumericKeyboard != null)
		{
			m_NumericKeyboard.gameObject.SetActive(true);
			m_NumericKeyboard.transform.SetParent(transform, true);
			m_NumericKeyboard.transform.localPosition = Vector3.up * 0.2f;
			m_NumericKeyboard.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

			m_NumericKeyboard.Setup(new char[] {'0', '1', '2', '3', '4', '5', '6', '7', '8', '*', '.'}, OnKeyPress);
		}
	}

	void Close(string inputString = "")
	{
		m_Open = false;

		if (m_NumericKeyboard == null) return;

		m_NumericKeyboard.gameObject.SetActive(false);
//		m_NumericKeyboard = null;
	}

	void OnKeyPress(char keyChar)
	{
//		if (char.IsNumber(keyChar))
//		{
            m_String += keyChar;
            text = m_String;
//        }
//		else
//		{
//			switch (keyChar)
//			{
//				case 'r':
//					trigger.AddListener(SubmitButtonPressed);
//					break;
//				case '*':
//					trigger.AddListener(MultiplyButtonPressed);
//					break;
//				case '/':
//					trigger.AddListener(DivideButtonPressed);
//					break;
//			}
//		}
		
	}

	void SubmitTextToField()
	{
		SendOnSubmit();
	}

//	protected new void OnFocus()
//	{
//		
//	}
//    public void OnBeginDrag(RayEventData eventData)
//    {
//        throw new NotImplementedException();
//    }
}
