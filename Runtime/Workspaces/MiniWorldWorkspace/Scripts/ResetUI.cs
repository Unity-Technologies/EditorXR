using UnityEngine;

namespace Unity.Labs.EditorXR.UI
{
    sealed class ResetUI : MonoBehaviour
    {
#pragma warning disable 649
        [SerializeField]
        UnityEngine.UI.Button m_ResetButton;
#pragma warning restore 649

        public UnityEngine.UI.Button resetButton
        {
            get { return m_ResetButton; }
        }
    }
}
