using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.VR.Tools;

public class NumericInputField : InputField, IInstantiateUI
{

	[Tooltip( "The prefab for the keyboard" )]
	[SerializeField]
	private GameObject m_KeyboardPrefab;

	private GameObject m_Keyboard;
	private Text m_Text;
	private string m_String;

	public Func<GameObject, GameObject> instantiateUI { get; set; }

	public override void OnSelect(BaseEventData eventData)
	{
		base.OnSelect(eventData);

		// Instantiate keyboard here
		if ( m_Keyboard == null )
		{
			m_Keyboard = instantiateUI( m_KeyboardPrefab );
			m_Text = m_Keyboard.GetComponentInChildren<Text>();
			foreach ( var button in m_Keyboard.GetComponentsInChildren<NumericInputButton>() )
			{
				button.OnPressAction = OnKeyPress;
			}
			var b = m_Keyboard.GetComponentInChildren<Button>();
			b.onClick.RemoveAllListeners();
			b.onClick.AddListener( () =>
			{
				m_String = "";
				m_Text.text = m_String;
			} );
			b.onClick.SetPersistentListenerState( 0, UnityEventCallState.EditorAndRuntime );
		}
	}

	void OnKeyPress( string str )
	{
		m_String += str;
		m_Text.text = m_String;
	}

	void Submit()
	{

	}

	bool IsValidString()
	{
		return false;
	}

}
