using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.EditorXR.Menus
{
    class MainMenuActionButton : MonoBehaviour
    {
        [SerializeField]
        Button m_Button;

        [SerializeField]
        Sprite m_Icon;

        [SerializeField]
        TextMeshProUGUI m_NameText;

        public Func<Action, bool> buttonPressed { get; set; }
    }
}
