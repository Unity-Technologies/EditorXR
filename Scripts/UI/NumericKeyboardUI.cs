using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.VR.Utilities;

public class NumericKeyboardUI : MonoBehaviour
{
	[SerializeField]
	private NumericInputButton m_ButtonTemplate;

	[SerializeField]
	private int m_Rows;
	[SerializeField]
	private int m_Columns;
	[SerializeField]
	private float m_ButtonSpacing;

	[SerializeField]
	private Transform[] m_CustomButtonLocations;

	private List<NumericInputButton> m_Buttons = new List<NumericInputButton>();

//	private char[] m_KeyChars;

	public void Setup(char[] keyChars, Action<char> keyPress, bool pressOnHover = false)
	{
//		m_KeyChars = keyChars;

		var row = 0;
		var col = 0;

		for (int i = 0; i < keyChars.Length; i++)
		{
			var button = U.Object.Instantiate(m_ButtonTemplate.gameObject).GetComponent<NumericInputButton>();
			m_Buttons.Add(button);

			if (m_CustomButtonLocations != null && m_CustomButtonLocations.Length > 0)
			{
				button.transform.SetParent(m_CustomButtonLocations[i]);
				button.transform.localPosition = Vector3.zero;
				button.transform.localRotation = Quaternion.identity;
			}
			else
			{
				// TODO ensure correct number of rows/columns
				// Set up using rows and columns
				var rowIndex = row % m_Rows;
				var colIndex = col % m_Columns;

				button.transform.SetParent(transform);
				// Rows filled from top to bottom
				button.transform.position = new Vector3(rowIndex * m_ButtonSpacing, -colIndex * m_ButtonSpacing);
				button.transform.rotation = Quaternion.identity;
			}

			button.Setup(keyChars[i], keyPress, pressOnHover);
		}
	}
}
