#if UNITY_EDITOR
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Menus
{
    /// <summary>
    /// Mandates that derived classes implement core SpatialUI implementation
    /// The SpatialMenu is the first robust implementation, SpatialContextUI is planned to derive from core
    /// </summary>
    public abstract class SpatialUICore : MonoBehaviour
    {
        public enum SpatialInterfaceInputMode
        {
            Translation,
            Rotation,
            GhostRay,
            ExternalInputRay,
            BCI
        }

        [Header("Prefabs")]
        [SerializeField]
        protected GameObject m_SectionTitleElementPrefab;

        [SerializeField]
        protected GameObject m_SubMenuElementPrefab;

        protected SpatialUIToggle m_SpatialPinToggle { get; set; }
    }
}
#endif
