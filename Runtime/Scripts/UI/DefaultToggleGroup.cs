using UnityEngine;
using UnityEngine.UI;

namespace Unity.Labs.EditorXR.UI
{
    sealed class DefaultToggleGroup : MonoBehaviour
    {
#pragma warning disable 649
        [SerializeField]
        Toggle m_DefaultToggle;
#pragma warning restore 649

        public Toggle defaultToggle { get { return m_DefaultToggle; } }
    }
}
