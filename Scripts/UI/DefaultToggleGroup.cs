using UnityEngine;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR.UI
{
    sealed class DefaultToggleGroup : MonoBehaviour
    {
        [SerializeField]
        Toggle m_DefaultToggle;

        public Toggle defaultToggle { get { return m_DefaultToggle; } }
    }
}
