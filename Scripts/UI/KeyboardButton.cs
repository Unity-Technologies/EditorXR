using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.VR.Modules;
using UnityEngine.VR.Utilities;

/// <summary>
/// Set either the button's text field or the ASCII value
/// </summary>
public class KeyboardButton : RayButton, IRayBeginDragHandler, IRayDragHandler
{
	[SerializeField]
	private char m_Character;

	public Text textComponent { get { return m_TextComponent; } set { m_TextComponent = value; } }

	[SerializeField]
	private Text m_TextComponent;

	[SerializeField]
	private bool m_MatchButtonTextToCharacter;

	[SerializeField]
	private Image m_ButtonIcon;

	[SerializeField]
	private GameObject m_ButtonMesh;

	[SerializeField]
	private bool m_RepeatOnHold;

	[SerializeField]
	private float m_RepeatTime = 0.5f;

	private float m_HoldStartTime;
	private float m_RepeatWaitTime;

	private Action<char> m_KeyPress;

	public UnityEvent trigger { get; set; }
	private UnityEvent m_Trigger = new UnityEvent();

	public void Setup(Action<char> keyPress, bool pressOnHover)
	{
		m_Trigger.RemoveAllListeners();

		m_KeyPress = keyPress;

		m_Trigger = pressOnHover ? onEnter : onDown;

		m_Trigger.AddListener(NumericKeyPressed);
	}

	protected override void OnDisable()
	{
		m_Trigger.RemoveListener(NumericKeyPressed);

		base.OnDisable();
	}

	private void NumericKeyPressed()
	{
		if (m_KeyPress != null)
			m_KeyPress(m_Character);
	}

	public void OnBeginDrag(RayEventData eventData)
	{
		if (U.UI.IsValidEvent(eventData, selectionFlags) && m_RepeatOnHold)
		{
			m_HoldStartTime = Time.realtimeSinceStartup;
			m_RepeatWaitTime = m_RepeatTime;
		}
	}

	public void OnDrag(RayEventData eventData)
	{
		if (U.UI.IsValidEvent(eventData, selectionFlags) && m_RepeatOnHold)
		{
			if (m_HoldStartTime + m_RepeatWaitTime < Time.realtimeSinceStartup)
			{
				NumericKeyPressed();
				m_HoldStartTime = Time.realtimeSinceStartup;
				m_RepeatWaitTime *= 0.75f;
			}
		}
	}
}
