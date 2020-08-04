using UnityEngine;

namespace Unity.EditorXR
{
    /// <summary>
    /// A convenience class for simple action implementations
    /// </summary>
    abstract class BaseAction : MonoBehaviour, IAction
    {
#pragma warning disable 649
        [SerializeField]
        Sprite m_Icon;
#pragma warning restore 649

        public Sprite icon
        {
            get { return m_Icon; }
        }

        public abstract void ExecuteAction();
    }
}
