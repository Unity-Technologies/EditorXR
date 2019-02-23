using UnityEngine;
using Button = UnityEditor.Experimental.EditorVR.UI.Button;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
    sealed class InspectorArrayHeaderItem : InspectorPropertyItem
    {
        const float k_ExpandArrowRotateSpeed = 0.4f;
        static readonly Quaternion k_ExpandedRotation = Quaternion.AngleAxis(90f, Vector3.forward);
        static readonly Quaternion k_NormalRotation = Quaternion.identity;

        [SerializeField]
        Button m_ExpandArrow;

        public override void UpdateSelf(float width, int depth, bool expanded)
        {
            base.UpdateSelf(width, depth, expanded);

            // Rotate arrow for expand state
            m_ExpandArrow.transform.localRotation = Quaternion.Lerp(m_ExpandArrow.transform.localRotation,
                expanded ? k_ExpandedRotation : k_NormalRotation,
                k_ExpandArrowRotateSpeed);
        }
    }
}
