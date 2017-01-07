using System;
using UnityEngine.UI;

namespace UnityEngine.Experimental.EditorVR.Menus
{
	public class MainMenuActionButton : MonoBehaviour
	{
		[SerializeField]
		private Button m_Button;

		[SerializeField]
		private Sprite m_Icon;

		[SerializeField]
		private Text m_NameText;

		public Func<Action, bool> buttonPressed { get; set; } 
	}
}
