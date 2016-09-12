using System;
using UnityEngine;

public class NumericInputButton : RayButton
{
	[SerializeField]
	private GameObject m_Mesh;

	private Action<char> m_KeyPress;

	private char m_KeyChar;

	private bool m_RequireClick;

	public void Setup(char keyChar, Action<char> keyPress, bool pressOnHover)
	{
		m_KeyChar = keyChar;
		m_KeyPress = keyPress;
		m_RequireClick = !pressOnHover;

		if (m_RequireClick)
		{
			onClick.AddListener(KeyPressed);
		}
		else
		{
			onEnter.AddListener(KeyPressed);
		}
	}

	protected override void OnDisable()
	{
		onClick.RemoveListener(KeyPressed);
		onEnter.RemoveListener(KeyPressed);

		base.OnDisable();
	}

	private void KeyPressed()
	{
		m_KeyPress(m_KeyChar);
	}

	/*
	public override void OnPointerEnter(PointerEventData eventData)
	{
		var rayEventData = eventData as RayEventData;
		if (rayEventData == null || U.UI.IsValidEvent(rayEventData, selectionFlags))
		{
			base.OnPointerEnter(eventData);

			if (!m_RequireClick)
			{
				m_KeyPress(m_KeyChar);
			}
		}
	}
	*/

	/*
	protected override void OnHandleBeginDrag(HandleEventData eventData)
	{
		// Prevent button from being moved by tool
		base.OnHandleBeginDrag(new HandleEventData(transform, true));
	}

	protected override void OnHandleDrag(HandleEventData eventData)
	{
		// Prevent button from being moved by tool
		base.OnHandleBeginDrag(new HandleEventData( transform, true ));
	}

	protected override void OnHandleEndDrag(HandleEventData eventData)
	{
		// Prevent button from being moved by tool
		base.OnHandleBeginDrag(new HandleEventData(transform, true));
	}
	*/
}
