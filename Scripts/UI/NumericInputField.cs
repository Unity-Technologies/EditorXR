using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.VR.Modules;
using UnityEngine.VR.Utilities;

public class NumericInputField : Selectable, ISubmitHandler, IRayBeginDragHandler, IRayDragHandler
{
	public SelectionFlags selectionFlags
	{
		get { return m_SelectionFlags; }
		set { m_SelectionFlags = value; }
	}
	[SerializeField]
	[FlagsProperty]
	protected SelectionFlags m_SelectionFlags = SelectionFlags.Ray | SelectionFlags.Direct;

	public enum InputType
	{
		Numeric,
		Alpha,
	}
	[SerializeField]
	protected InputType m_InputType;
	public InputType inputType
	{
		get { return m_InputType; }
	}

	public Func<NumericKeyboardUI> keyboard;
	private NumericKeyboardUI m_NumericKeyboard;

	public enum NumberType
	{
		Float,
		Int,
	}
	[SerializeField]
	private NumberType m_NumberType;
	
	[SerializeField]
	private Text m_TextComponent;

	[SerializeField]
	private float kDragFactor = 10f;
	private const float kDragSensitivity = .03f;
	private static float kDragDeadzone = 0.01f;
	private Vector3 m_StartDragPosition;
	private Vector3 m_LastPointerDragPosition;

	private string m_OutputString;
	private List<string> m_RawInputString = new List<string>();

	private bool m_Open;

	private bool m_PointerOverField;

	private float m_ClickThresholdTime = 0.3f;
	private float m_PressedTime;

	private static readonly string s_AllowedCharactersForFloat = "inftynaeINFTYNAE0123456789.,-*/+%^()";
	private static readonly string s_AllowedCharactersForInt = "0123456789-*/+%^()";

	public void SetTextFromInspectorField(string text)
	{
		var isValidString = true;

		foreach (var ch in text)
		{
			if (m_InputType == InputType.Numeric)
				isValidString = isValidString && IsNumericCharacter(ch);
		}

		if (isValidString)
		{
			m_TextComponent.text = m_OutputString = text;
			m_RawInputString.Clear();
			m_RawInputString.Add(m_OutputString);
		}
	}

	/// <summary>
	/// Send string to the inspector field
	/// </summary>
	public void UpdateInspectorField()
	{
	}

//	private void Press()
//	{
//		if ( !IsActive() || !IsInteractable() )
//			return;
//
//	}

	public override void OnPointerEnter(PointerEventData eventData)
	{
		var rayEventData = eventData as RayEventData;
		if (rayEventData == null || U.UI.IsValidEvent(rayEventData, selectionFlags))
		{
			base.OnPointerEnter(eventData);

			m_PointerOverField = true;

			if (eventData.dragging)
				m_LastPointerDragPosition = GetCurrentRayHitPosition(rayEventData);
		}
	}

	public override void OnPointerExit(PointerEventData eventData)
	{
		var rayEventData = eventData as RayEventData;
		if (rayEventData == null || U.UI.IsValidEvent(rayEventData, selectionFlags))
		{
			base.OnPointerExit(eventData);

			m_PointerOverField = false;
		}
	}

	public override void OnPointerDown(PointerEventData eventData)
	{
		var rayEventData = eventData as RayEventData;
		if (rayEventData == null || U.UI.IsValidEvent(rayEventData, selectionFlags))
		{
			base.OnPointerDown(eventData);

			m_PressedTime = Time.realtimeSinceStartup;
		}
	}

	public override void OnPointerUp(PointerEventData eventData)
	{
		var rayEventData = eventData as RayEventData;
		if (rayEventData == null || U.UI.IsValidEvent(rayEventData, selectionFlags))
		{
			base.OnPointerUp(eventData);

			if (Time.realtimeSinceStartup - m_PressedTime < m_ClickThresholdTime)
			{
				if (m_Open)
					Close();
				else
					Open();
			}
		}
	}

	public virtual void OnSubmit(BaseEventData eventData)
	{
		var rayEventData = eventData as RayEventData;
		if (rayEventData == null || U.UI.IsValidEvent(rayEventData, selectionFlags))
		{
//			base.OnSubmit(eventData);
		}
	}

	public override void OnDeselect(BaseEventData eventData)
	{
		base.OnDeselect(eventData);
		// TODO this works but need to only deselect when something besides a key button is clicked
//		Debug.Log("Deselect callled");
//		Close();
	}

	public override void OnSelect(BaseEventData eventData)
	{
		//
	}

	private void Open()
	{
		if (m_Open) return;
		m_Open = true;

		m_NumericKeyboard = keyboard();
		// Instantiate keyboard here
		if (m_NumericKeyboard != null)
		{
			m_NumericKeyboard.gameObject.SetActive(true);
			m_NumericKeyboard.transform.SetParent(transform, true);
			m_NumericKeyboard.transform.localPosition = Vector3.up * 0.05f;
			m_NumericKeyboard.transform.localRotation = Quaternion.identity;

			m_NumericKeyboard.Setup(OnKeyPress);
		}
	}

	private void Close()
	{
		m_Open = false;

		if (m_NumericKeyboard == null) return;

		m_NumericKeyboard.gameObject.SetActive(false);
		m_NumericKeyboard = null;
	}

	private void OnKeyPress(char keyCode)
	{
		if (m_InputType == InputType.Numeric)
		{
			if (keyCode == 'r') // Return
			{

			}
			else if (keyCode == 'b') // Backspace
			{
				DeleteChar();
			}
			else if (IsOperandCharacter((char) keyCode))
			{
				if (m_RawInputString.Count > 1)
					m_RawInputString.Add(keyCode.ToString());
			}
			else if (IsNumericCharacter((char) keyCode))
			{
				m_TextComponent.text = m_OutputString += keyCode;
			}
		}
		else
		{
			m_TextComponent.text = m_OutputString += keyCode;
		}
	}

	private void DeleteChar()
	{
		if (m_OutputString.Length == 0) return;

		m_OutputString = m_OutputString.Remove(m_OutputString.Length - 1);
		m_TextComponent.text = m_OutputString;
	}

	private void ClearField()
	{
		m_OutputString = "";
		m_TextComponent.text = m_OutputString;
	}

	private bool IsNumericCharacter(char ch)
	{
		if (ch >= '0' && ch <= '9') return true;
		if (ch == '-' && (m_OutputString.Length == 0)) return true;
		if (ch == '.' && !m_OutputString.Contains(".")) return true;

		return false;
	}

	private bool IsNumericString(string str)
	{
		var valid = true;

		foreach (var ch in str)
			valid = valid && IsNumericCharacter(ch);

		return valid;
	}

	private bool IsOperandCharacter(char ch)
	{
		if (ch == '+' || ch == '-' || ch == '*' || ch == '/') return true;

		return false;
	}

	public void OnBeginDrag(RayEventData eventData)
	{
		if (!U.UI.IsValidEvent(eventData, selectionFlags))
			return;

		m_StartDragPosition = GetCurrentRayHitPosition(eventData);
		m_LastPointerDragPosition = GetCurrentRayHitPosition(eventData);
	}

	public void OnDrag(RayEventData eventData)
	{
		if (!U.UI.IsValidEvent(eventData, selectionFlags))
			return;

		if (m_InputType == InputType.Numeric && m_PointerOverField)
		{
			DragNumericValue(eventData);
			m_LastPointerDragPosition = GetCurrentRayHitPosition(eventData);
		}
	}

	private void DragNumericValue(RayEventData eventData)
	{
//		if (m_RawInputString.Count > 1) ParseNumberField(true, );

		float num;
		if (!float.TryParse(m_TextComponent.text, out num))
			num = 0f;

		var xDelta =
			(transform.InverseTransformPoint(GetCurrentRayHitPosition(eventData)) -
				transform.InverseTransformPoint(m_LastPointerDragPosition)).x;

		num += xDelta*10f;

		m_OutputString = num.ToString();
		m_TextComponent.text = m_OutputString;

		UpdateInspectorField();
	}

	private Vector3 GetCurrentRayHitPosition(RayEventData eventData)
	{
		var rayOriginPos = eventData.rayOrigin;
		return rayOriginPos.position + rayOriginPos.forward*eventData.pointerCurrentRaycast.distance;
	}

	private static double CalculateFloatDragSensitivity(double value)
	{
		if (Double.IsInfinity(value) || Double.IsNaN(value))
		{
			return 0.0;
		}
		return (double) Mathf.Max(1, Mathf.Pow(Mathf.Abs((float)value), 0.5f)) * kDragSensitivity;
	}

	private static long CalculateIntDragSensitivity(long value)
	{
		return (long) Mathf.Max(1, Mathf.Pow(Mathf.Abs(value), 0.5f) * kDragSensitivity);
	}

	// Handle dragging of value
	private static void DragNumberValue(bool isDouble, ref double doubleVal,
		ref long longVal, string formatString, GUIStyle style, double dragSensitivity)
	{
//		switch (evt.GetTypeForControl(id))
//		{
//			case EventType.MouseDown:
//				if (dragHotZone.Contains(evt.mousePosition) && evt.button == 0)
//				{
//
//					s_DragCandidateState = 1;
//					s_DragStartValue = doubleVal;
//					s_DragStartIntValue = longVal;
//					s_DragStartPos = evt.mousePosition;
//					s_DragSensitivity = dragSensitivity;
//					evt.Use();
//					EditorGUIUtility.SetWantsMouseJumping(1);
//				}
//				break;
//			case EventType.MouseUp:
//				if (GUIUtility.hotControl == id && s_DragCandidateState != 0)
//				{
//					GUIUtility.hotControl = 0;
//					s_DragCandidateState = 0;
//					evt.Use();
//					EditorGUIUtility.SetWantsMouseJumping(0);
//				}
//				break;
//			case EventType.MouseDrag:
//				if (GUIUtility.hotControl == id)
//				{
//					switch (s_DragCandidateState)
//					{
//						case 1:
//							if ((Event.current.mousePosition - s_DragStartPos).sqrMagnitude > kDragDeadzone)
//							{
//								s_DragCandidateState = 2;
//								GUIUtility.keyboardControl = id;
//							}
//							evt.Use();
//							break;
//						case 2:
//							// Don't change the editor.content.text here.
//							// Instead, wait for scripting validation to enforce clamping etc. and then
//							// update the editor.content.text in the repaint event.
//							if (isDouble)
//							{
//								doubleVal += HandleUtility.niceMouseDelta*s_DragSensitivity;
//								doubleVal = MathUtils.RoundBasedOnMinimumDifference(doubleVal, s_DragSensitivity);
//							}
//							else
//							{
//								longVal += (long) Math.Round(HandleUtility.niceMouseDelta*s_DragSensitivity);
//							}
//							GUI.changed = true;
//
//							evt.Use();
//							break;
//					}
//				}
//				break;
//		}
	}

	private void ParseNumberField(ref double doubleVal, ref long longVal, string formatString)
	{
		var isFloat = m_NumberType == NumberType.Float;

		string allowedCharacters = isFloat ? s_AllowedCharactersForFloat : s_AllowedCharactersForInt;

		string str = isFloat ? doubleVal.ToString(formatString) : longVal.ToString(formatString);

		// clean up the text
		if (isFloat)
		{
			string lowered = str.ToLower();
			if (lowered == "inf" || lowered == "infinity")
			{
				doubleVal = Double.PositiveInfinity;
			}
			else if (lowered == "-inf" || lowered == "-infinity")
			{
				doubleVal = Double.NegativeInfinity;
			}
			else
			{
				// Make sure that comma & period are interchangable.
				str = str.Replace(',', '.');

				if (!double.TryParse(str, System.Globalization.NumberStyles.Float,
						System.Globalization.CultureInfo.InvariantCulture.NumberFormat, out doubleVal))
				{
					doubleVal = StringExpressionEvaluator.Evaluate<double>(str);
					return;
				}

				// Don't allow user to enter NaN - it opens a can of worms that can trigger many latent bugs,
				// and is not really useful for anything.
				if (double.IsNaN(doubleVal))
				{
					doubleVal = 0;
				}
			}
		}
		else
		{
			if (!long.TryParse(str, out longVal))
			{
				longVal = StringExpressionEvaluator.Evaluate<long>(str);
			}
		}
	}
}
