using System;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace UnityEngine.VR.Tools
{
	public class MainMenuButton : MonoBehaviour
	{
		[SerializeField]
		private Button m_Button;
		[SerializeField]
		private Text m_ButtonDescription;
		[SerializeField]
		private Text m_ButtonTitle;

		public Button button { get { return m_Button; } }
		public Action ButtonClicked;

		private void Awake()
		{
			Assert.IsNotNull(m_Button, "m_Button is not assigned!");
			Assert.IsNotNull(m_ButtonDescription, "m_ButtonDescription is not assigned!");
			Assert.IsNotNull(m_ButtonTitle, "m_ButtonTitle is not assigned!");

			m_Button.onClick.AddListener(OnButtonClicked);
		}

		private void OnDestroy()
		{
			m_Button.onClick.RemoveListener(OnButtonClicked);
		}

		public void SetData(string name, string description)
		{
			m_ButtonTitle.text = name;
			m_ButtonDescription.text = description;
		}

		private void OnButtonClicked()
		{
			Action m_ButtonActionHandler = ButtonClicked;
			if (m_ButtonActionHandler != null)
				m_ButtonActionHandler();
		}
	}
}