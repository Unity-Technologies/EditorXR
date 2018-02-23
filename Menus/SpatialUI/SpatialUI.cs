#if UNITY_EDITOR
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
    public class SpatialUI : MonoBehaviour, IAdaptPosition
    {
        [SerializeField] CanvasGroup m_MainCanvasGroup;

        bool m_BeingMoved;

        public Transform transform { get; set; }
        public float allowedGazeDivergence { get; private set; }

        public bool beingMoved
        {
            get { return m_BeingMoved; }
            set
            {
                m_BeingMoved = value;
                m_MainCanvasGroup.alpha = m_BeingMoved ? 0.25f : 1f;
            }
        }
    }
}
#endif
