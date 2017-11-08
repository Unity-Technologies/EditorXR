#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UnityEditor.Experimental.EditorVR.Menus
{
    sealed class MainMenuActionToggle : MainMenuActionButton
    {
        [SerializeField]
        Button m_Button2;

        [SerializeField]
        Sprite m_Icon02;

        [SerializeField]
        TextMeshProUGUI m_NameText2;
    }
}
#endif
