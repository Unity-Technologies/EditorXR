using UnityEngine;

namespace Unity.EditorXR.Helpers
{
    class PlayerBody : MonoBehaviour
    {
#pragma warning disable 649
        [SerializeField]
        Collider m_OverShoulderTrigger;

        [SerializeField]
        Collider m_AboveHeadTrigger;
#pragma warning restore 649

        public Collider overShoulderTrigger { get { return m_OverShoulderTrigger; } }

        public Collider aboveHeadTrigger { get { return m_AboveHeadTrigger; } }
    }
}
