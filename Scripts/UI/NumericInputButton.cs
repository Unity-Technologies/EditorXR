using System;
using System.Linq;
using UnityEngine;
using UnityEngine.VR.Handles;

public class NumericInputButton : BaseHandle
{
	[SerializeField]
	private string m_String;

	public Action<string> OnPressAction { private get; set; }

	protected virtual void OnHandleRayHover(HandleEventData eventData)
	{
		OnPressAction.Invoke(m_String);
	}
}