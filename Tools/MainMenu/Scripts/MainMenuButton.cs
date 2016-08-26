using System;
using UnityEngine.UI;

namespace UnityEngine.VR.Menus
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
		public Action clicked;

		private void Awake()
		{
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
			if (clicked != null)
				clicked();
		}
	}
}