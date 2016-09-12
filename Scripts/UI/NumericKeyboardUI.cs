using UnityEngine;
using System.Collections;
using UnityEngine.VR.Handles;

public class NumericKeyboardUI : MonoBehaviour
{
	[SerializeField]
	private NumericInputButton[] m_NumericButtons;

	public NumericInputButton[] numericButtons { get; private set; }

	void Start()
	{
		numericButtons = GetComponentsInChildren<NumericInputButton>();
	}
}
