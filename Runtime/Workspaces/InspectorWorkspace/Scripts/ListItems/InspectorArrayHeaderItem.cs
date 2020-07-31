using UnityEngine;
using Button = Unity.EditorXR.UI.Button;

namespace Unity.EditorXR.Workspaces
{
    sealed class InspectorArrayHeaderItem : InspectorPropertyItem
    {
        const float k_ExpandArrowRotateSpeed = 0.4f;
        static readonly Quaternion k_ExpandedRotation = Quaternion.AngleAxis(90f, Vector3.forward);
        static readonly Quaternion k_NormalRotation = Quaternion.identity;

#pragma warning disable 649
        [SerializeField]
        Button m_ExpandArrow;
#pragma warning restore 649

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
