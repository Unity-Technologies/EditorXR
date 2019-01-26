using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Helpers
{
    public class PlayerBody : MonoBehaviour
    {
        [SerializeField]
        Collider m_OverShoulderTrigger;

        [SerializeField]
        Collider m_AboveHeadTrigger;

        public Collider overShoulderTrigger { get { return m_OverShoulderTrigger; } }

        public Collider aboveHeadTrigger { get { return m_AboveHeadTrigger; } }
    }
}
