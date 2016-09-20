using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.VR.Modules;
using UnityEngine.VR.Utilities;

public class NumericInputField : Selectable, ISubmitHandler, IPointerClickHandler, IRayBeginDragHandler, IRayEndDragHandler, IRayDragHandler
{
	public SelectionFlags selectionFlags
	{
		get { return m_SelectionFlags; }
		set { m_SelectionFlags = value; }
	}
	[SerializeField]
	[FlagsProperty]
	protected SelectionFlags m_SelectionFlags = SelectionFlags.Ray | SelectionFlags.Direct;

	[Serializable]
	public class OnChangeEvent : UnityEvent<string> { }
	public OnChangeEvent onValueChanged { get { return m_OnValueChanged; } }
	[SerializeField]
	private OnChangeEvent m_OnValueChanged = new OnChangeEvent();

	public Func<NumericKeyboardUI> keyboard;
	private NumericKeyboardUI m_NumericKeyboard;

	public SerializedPropertyType contentType { get { return m_ContentType; } }
	[SerializeField]
	private SerializedPropertyType m_ContentType = SerializedPropertyType.Float;

	[SerializeField]
	private Transform m_KeyboardAnchorTransform;

	[SerializeField]
	private Text m_TextComponent;

	[SerializeField]
	private int m_CharacterLimit = 10;

	[SerializeField]
	private bool m_UpdateDrag;
	private const float kDragSensitivity = 10f;
	private const float kDragDeadzone = 0.01f;
	private Vector3 m_StartDragPosition;
	private Vector3 m_LastPointerPosition;
	private bool m_PointerOverField;

	private bool m_Open;

	private int m_OperandCount;

	private const string kFloatFieldFormatString = "g7";
	private const string kIntFieldFormatString = "#######0";

	// We cannot round to more decimals than 15 according to docs for System.Math.Round.
	private const int kMaxDecimals = 15;
	private const string kAllowedCharactersForFloat = "inftynaeINFTYNAE0123456789.,-*/+%^()";
	private const string kAllowedCharactersForInt = "0123456789-*/+%^()";
	private const string kOperandCharacters = "-*/+%^()";

	private bool m_Numeric
	{
		get { return m_ContentType == SerializedPropertyType.Float || m_ContentType == SerializedPropertyType.Integer; }
	}

	public string text
	{
		get
		{
			return m_Text;
		}
		set
		{
			if (m_Text == value)
				return;
			if (value == null)
				value = "";

			m_Text = m_CharacterLimit > 0 && value.Length > m_CharacterLimit ? value.Substring(0, m_CharacterLimit) : value;
		}
	}
	private string m_Text = string.Empty;

	protected override void OnEnable()
	{
		base.OnEnable();

		if (m_Text == null)
			m_Text = string.Empty;

		if (m_TextComponent != null)
		{
			UpdateLabel();
		}
	}

	public void ForceUpdateLabel()
	{
		UpdateLabel();
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		var rayEventData = eventData as RayEventData;
		if (rayEventData == null || U.UI.IsValidEvent(rayEventData, selectionFlags))
		{
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
		{
			base.OnPointerEnter(eventData);

			m_PointerOverField = true;
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
		}
	}

	public override void OnPointerUp(PointerEventData eventData)
	{
		var rayEventData = eventData as RayEventData;
		if (rayEventData == null || U.UI.IsValidEvent(rayEventData, selectionFlags))
		{
			base.OnPointerUp(eventData);
		}
	}

	private bool MayDrag()
	{
		return IsActive() &&
				IsInteractable() &&
				m_TextComponent != null;
	}

	public void OnBeginDrag(RayEventData eventData)
	{
		if (!U.UI.IsValidEvent(eventData, selectionFlags) && MayDrag())
			return;

		m_StartDragPosition = GetLocalPointerPosition(eventData);
	}

	public void OnDrag(RayEventData eventData)
	{
		if (!U.UI.IsValidEvent(eventData, selectionFlags) || !MayDrag())
			return;

		if (m_Numeric && m_PointerOverField)
		{
			if (!m_UpdateDrag)
			{
				if (Mathf.Abs(GetLocalPointerPosition(eventData).x - m_StartDragPosition.x) > kDragDeadzone)
					StartDrag(eventData);
			}
			else
			{
				DragNumberValue(eventData);
				m_LastPointerPosition = GetLocalPointerPosition(eventData);
			}
		}
	}

	private void StartDrag(RayEventData eventData)
	{
		if (IsExpression())
			ParseNumberField();
		m_LastPointerPosition = GetLocalPointerPosition(eventData);
		m_UpdateDrag = true;
		eventData.eligibleForClick = false;
	}

	public void OnEndDrag(RayEventData eventData)
	{
		if (!U.UI.IsValidEvent(eventData, selectionFlags) || !MayDrag())
			return;

		m_UpdateDrag = false;
	}

	private void DragNumberValue(RayEventData eventData)
	{
		var delta = GetLocalPointerPosition(eventData).x - m_LastPointerPosition.x;

		if (contentType == SerializedPropertyType.Float)
		{
			float num;
			if (!float.TryParse(text, out num))
				num = 0f;

			var dragSensitivity = CalculateFloatDragSensitivity(num);
			num += delta * dragSensitivity;
			//	floatVal += HandleUtility.niceMouseDelta*s_DragSensitivity;
			//num = RoundBasedOnMinimumDifference(num, dragSensitivity);
			m_Text = num.ToString(kFloatFieldFormatString);
		}
		else
		{
			int intNum;
			if (!int.TryParse(text, out intNum) || m_Text == "")
				intNum = 0;

			var dragSensitivity = CalculateIntDragSensitivity(intNum);
//			intNum += (int) Math.Round(HandleUtility.niceMouseDelta* dragSensitivity);
			intNum += (int)Math.Round(delta * dragSensitivity);

			m_Text = intNum.ToString(kIntFieldFormatString);
		}

		SendOnValueChangedAndUpdateLabel();
	}

	private Vector3 GetLocalPointerPosition(RayEventData eventData)
	{
		var rayOriginPos = eventData.rayOrigin;
		var hitPos = rayOriginPos.position + rayOriginPos.forward * eventData.pointerCurrentRaycast.distance;
		return transform.InverseTransformPoint(hitPos);
	}

	public virtual void OnSubmit(BaseEventData eventData)
	{
		var rayEventData = eventData as RayEventData;
		if (rayEventData == null || U.UI.IsValidEvent(rayEventData, selectionFlags))
		{
		}
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
			m_NumericKeyboard.transform.position = m_KeyboardAnchorTransform.position;
			m_NumericKeyboard.transform.rotation = m_KeyboardAnchorTransform.rotation;

			m_NumericKeyboard.Setup(OnKeyPress);
		}
	}

	private void Close()
	{
		m_Open = false;

		if (IsExpression())
			ParseNumberField();

		if (m_NumericKeyboard == null) return;

		m_NumericKeyboard.gameObject.SetActive(false);
		m_NumericKeyboard = null;
	}

	private void OnKeyPress(char keyCode)
	{
		switch ((int)keyCode)
		{
			case (int)NumericInputButton.SpecialKeyType.Backspace:
				Delete();
				return;
			case (int)NumericInputButton.SpecialKeyType.Return:
				Return();
				return;
			case (int)NumericInputButton.SpecialKeyType.Space:
				AppendWhitespace();
				return;
		}

		if (IsValid(keyCode))
			Insert(keyCode);
	}

	private bool IsValid(char ch)
	{
		if (!m_TextComponent.font.HasCharacter(ch))
			return false;

		if (m_ContentType == SerializedPropertyType.Float)
		{
			if (!kAllowedCharactersForFloat.Contains(ch.ToString()))
				return false;
		}
		else if (m_ContentType == SerializedPropertyType.Integer)
		{
			if (!kAllowedCharactersForInt.Contains(ch.ToString()))
				return false;
		}

		return true;
	}

	private bool IsExpression()
	{
		return m_OperandCount > 0;
	}

	private bool IsOperand(char ch)
	{
		return kOperandCharacters.Contains(ch.ToString()) && !(m_Text.Length == 0 && ch == '-');
	}

	private void Insert(char ch)
	{
		var len = m_Text.Length;

		text += ch;

		if (len != m_Text.Length)
		{
			if (m_Numeric)
			{
				if (m_Numeric && IsOperand(ch))
					m_OperandCount++;
			}

			if (!IsExpression())
				SendOnValueChangedAndUpdateLabel();
			else
				UpdateLabel();
		}
	}

	private void Delete()
	{
		if (m_Text.Length == 0) return;

		var ch = m_Text[m_Text.Length - 1];

		if (m_Numeric && IsOperand(ch))
			m_OperandCount--;

		m_Text = m_Text.Remove(m_Text.Length - 1);

		if (!IsExpression())
			SendOnValueChangedAndUpdateLabel();
		else
			UpdateLabel();
	}

	private void Return()
	{
		if (m_Numeric)
			ParseNumberField();
		// TODO check multiline
	}

	private void AppendWhitespace()
	{
		var len = m_Text.Length;

		text += " ";

		if (len != m_Text.Length)
			SendOnValueChangedAndUpdateLabel();
	}

	private void ClearField()
	{
		m_Text = "";
		m_OperandCount = 0;

		SendOnValueChangedAndUpdateLabel();
	}

	private void SendOnValueChangedAndUpdateLabel()
	{
		SendOnValueChanged();
		UpdateLabel();
	}

	private void SendOnValueChanged()
	{
		if (onValueChanged != null)
			onValueChanged.Invoke(text);
	}

	protected void UpdateLabel()
	{
		if (m_TextComponent != null && m_TextComponent.font != null)
			m_TextComponent.text = m_Text;
	}

	private float CalculateFloatDragSensitivity(float value)
	{
		if (float.IsInfinity(value) || float.IsNaN(value))
			return 0f;

		return Mathf.Max(1, Mathf.Pow(Mathf.Abs(value), 0.5f)) * kDragSensitivity;
	}

	private int CalculateIntDragSensitivity(int value)
	{
		return (int)Mathf.Max(1, Mathf.Pow(Mathf.Abs(value), 0.5f) * kDragSensitivity);
	}

	// From Editor/Mono/Utils/MathUtils.cs
	private float RoundBasedOnMinimumDifference(float valueToRound, float minDifference)
	{
		if (minDifference == 0)
			return DiscardLeastSignificantDecimal(valueToRound);
		return (float)Math.Round(valueToRound, GetNumberOfDecimalsForMinimumDifference(minDifference), MidpointRounding.AwayFromZero);
	}

	// From Editor/Mono/Utils/MathUtils.cs
	float DiscardLeastSignificantDecimal(float v)
	{
		int decimals = Mathf.Clamp((int)(5 - Mathf.Log10(Mathf.Abs(v))), 0, kMaxDecimals);
		return (float)Math.Round(v, decimals, MidpointRounding.AwayFromZero);
	}

	// From Editor/Mono/Utils/MathUtils.cs
	int GetNumberOfDecimalsForMinimumDifference(float minDifference)
	{
		return Mathf.Clamp(-Mathf.FloorToInt(Mathf.Log10(Mathf.Abs(minDifference))), 0, kMaxDecimals);
	}

	private void ParseNumberField()
	{
		var isFloat = m_ContentType == SerializedPropertyType.Float;
		var str = m_Text;

		if (isFloat)
		{
			float floatVal;

			// Make sure that comma & period are interchangable.
			m_Text = m_Text.Replace(',', '.');

			if (!float.TryParse(m_Text, System.Globalization.NumberStyles.Float,
					System.Globalization.CultureInfo.InvariantCulture.NumberFormat, out floatVal))
				floatVal = StringExpressionEvaluator.Evaluate<float>(m_Text);

			if (float.IsNaN(floatVal))
				floatVal = 0;

			m_Text = floatVal.ToString(kFloatFieldFormatString);
		}
		else
		{
			int intVal;
			if (!int.TryParse(m_Text, out intVal))
				m_Text = StringExpressionEvaluator.Evaluate<long>(m_Text).ToString(kIntFieldFormatString);
		}

		if (str != m_Text)
			SendOnValueChangedAndUpdateLabel();

		m_OperandCount = 0;
	}
}
