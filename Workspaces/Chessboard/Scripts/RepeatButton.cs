using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class RepeatButton : MonoBehaviour
{
	public bool repeat { private get; set; }

	[SerializeField]
	private Button m_Button = null;

	void Update()
	{
		if (repeat)
			m_Button.onClick.Invoke();
	}
}
