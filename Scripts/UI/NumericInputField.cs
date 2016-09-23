using System;
using UnityEngine;
using UnityEngine.VR.Modules;
using UnityEngine.VR.Utilities;

public class NumericInputField : RayInputField, IRayBeginDragHandler, IRayEndDragHandler, IRayDragHandler
{
	public enum ContentType
	{
		Float,
		Int,
	}
	public ContentType numberType { get { return m_NumberType; } }
	[SerializeField]
	private ContentType m_NumberType = ContentType.Float;

	private bool m_UpdateDrag;
	private Vector3 m_StartDragPosition;
	private Vector3 m_LastPointerPosition;
	private const float kDragSensitivity = 0.01f;
	private const float kDragDeadzone = 0.01f;

	private int m_OperandCount;

	private const string kFloatFieldFormatString = "g7";
	private const string kIntFieldFormatString = "#######0";

	// We cannot round to more decimals than 15 according to docs for System.Math.Round.
	private const int kMaxDecimals = 15;
	private const string kAllowedCharactersForFloat = "inftynaeINFTYNAE0123456789.,-*/+%^()";
	private const string kAllowedCharactersForInt = "0123456789-*/+%^()";
	private const string kOperandCharacters = "-*/+%^()";

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

		if (eventData.pointerCurrentRaycast.gameObject == gameObject)
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
		var delta = GetLocalPointerPosition(eventData) - m_LastPointerPosition;

		if (numberType == ContentType.Float)
		{
			float num;
			if (!float.TryParse(text, out num))
				num = 0f;

			var dragSensitivity = CalculateFloatDragSensitivity(num);
//			num += delta * dragSensitivity;
			num += GetNicePointerDelta(delta) * dragSensitivity;
			num = RoundBasedOnMinimumDifference(num, dragSensitivity);
			m_Text = num.ToString(kFloatFieldFormatString);
		}
		else
		{
			int intNum;
			if (!int.TryParse(text, out intNum))
				intNum = 0;

			var dragSensitivity = CalculateIntDragSensitivity(intNum);
			intNum += (int)Math.Round(GetNicePointerDelta(delta) * dragSensitivity);
//			intNum += (int)Math.Round(delta * dragSensitivity);

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

	protected override void Close()
	{
		if (IsExpression())
			ParseNumberField();

		base.Close();
	}

	protected override bool IsValid(char ch)
	{
		if (!base.IsValid(ch))
			return false;

		if (m_NumberType == ContentType.Float)
		{
			if (!kAllowedCharactersForFloat.Contains(ch.ToString()))
				return false;
		}
		else if (m_NumberType == ContentType.Int)
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

	protected override void Append(char c)
	{
		var len = m_Text.Length;

		text += c;

		if (len != m_Text.Length)
		{
			if (IsOperand(c))
				m_OperandCount++;

			if (!IsExpression())
				SendOnValueChangedAndUpdateLabel();
			else
				UpdateLabel();
		}
	}

	protected override void Backspace()
	{
		if (m_Text.Length == 0) return;

		var ch = m_Text[m_Text.Length - 1];

		if (IsOperand(ch))
			m_OperandCount--;

		m_Text = m_Text.Remove(m_Text.Length - 1);

		if (!IsExpression())
			SendOnValueChangedAndUpdateLabel();
		else
			UpdateLabel();
	}

	protected override void Return()
	{
		if (IsExpression())
			ParseNumberField();
	}

	protected override void Space()
	{
		Return();
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

	private float RoundBasedOnMinimumDifference(float valueToRound, float minDifference)
	{
		if (Math.Abs(minDifference) < Mathf.Epsilon)
			return DiscardLeastSignificantDecimal(valueToRound);
		return (float)Math.Round(valueToRound, GetNumberOfDecimalsForMinimumDifference(minDifference), MidpointRounding.AwayFromZero);
	}

	private float DiscardLeastSignificantDecimal(float v)
	{
		int decimals = Mathf.Clamp((int)(5 - Mathf.Log10(Mathf.Abs(v))), 0, kMaxDecimals);
		return (float)Math.Round(v, decimals, MidpointRounding.AwayFromZero);
	}

	private int GetNumberOfDecimalsForMinimumDifference(float minDifference)
	{
		return Mathf.Clamp(-Mathf.FloorToInt(Mathf.Log10(Mathf.Abs(minDifference))), 0, kMaxDecimals);
	}

	private void ParseNumberField()
	{
		var str = m_Text;

		if (m_NumberType == ContentType.Float)
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
				m_Text = StringExpressionEvaluator.Evaluate<int>(m_Text).ToString(kIntFieldFormatString);
		}

		if (str != m_Text)
			SendOnValueChangedAndUpdateLabel();

		m_OperandCount = 0;
	}

	private bool m_UseYSign;
	private float GetNicePointerDelta(Vector3 delta)
	{
		Vector2 d = delta;
		d.y = -d.y;

		if (Mathf.Abs(Mathf.Abs(d.x) - Mathf.Abs(d.y)) / Mathf.Max(Mathf.Abs(d.x), Mathf.Abs(d.y)) > .1f)
		{
			if (Mathf.Abs(d.x) > Mathf.Abs(d.y))
				m_UseYSign = false;
			else
				m_UseYSign = true;
		}

		if (m_UseYSign)
			return Mathf.Sign(d.y) * d.magnitude * 100f;
		else
			return Mathf.Sign(d.x) * d.magnitude * 100f;
	}
}
