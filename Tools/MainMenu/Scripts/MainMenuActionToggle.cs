using UnityEngine.UI;

namespace UnityEngine.VR.Menus
{
	public class MainMenuActionToggle : MainMenuActionButton
	{
		[SerializeField]
		private Button m_Button2;

		[SerializeField]
		new private Sprite m_Icon02; // // Hide the parent sprite, as these are not shown in main menu toggles

		[SerializeField]
		private Text m_NameText2;
	}
}
