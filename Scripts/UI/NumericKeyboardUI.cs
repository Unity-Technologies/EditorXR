using UnityEngine;
using System.Collections;
using UnityEngine.VR.Handles;

public class NumericKeyboardUI : MonoBehaviour
{
	[SerializeField]
	private BaseHandle[] m_NumericButtons;
	[SerializeField]
	private BaseHandle m_EnterButton;
	[SerializeField]
	private BaseHandle m_DeleteButton;
	[SerializeField]
	private BaseHandle m_MultiplyButton;
	[SerializeField]
	private BaseHandle m_DivideButton;
	[SerializeField]
	private BaseHandle m_DecimelButton;

	void OnEnable()
	{
//		m_EnterButton.on;
	}

	public void KeyPressed(string str)
	{
		
	}
}
