#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR.Menus
{
	sealed class MainMenuActionToggle : MainMenuActionButton
	{
		[SerializeField]
		private Button m_Button2;

		[SerializeField]
		private Sprite m_Icon02;

		[SerializeField]
		private Text m_NameText2;
	}
}
#endif
