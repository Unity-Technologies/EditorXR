#if ENABLE_EDITORXR
using UnityEditor.Experimental.EditorVR.Modules;

namespace UnityEditor.Experimental.EditorVR.Core
{
    partial class EditorVR
    {
        class SpatialHintModuleConnector : Nested, ILateBindInterfaceMethods<SpatialHintModule>
        {
            public void LateBindInterfaceMethods(SpatialHintModule provider)
            {
                IControlSpatialHintingMethods.setSpatialHintState = provider.SetState;
                IControlSpatialHintingMethods.setSpatialHintPosition = provider.SetPosition;
                IControlSpatialHintingMethods.setSpatialHintContainerRotation = provider.SetContainerRotation;
                IControlSpatialHintingMethods.setSpatialHintShowHideRotationTarget = provider.SetShowHideRotationTarget;
                IControlSpatialHintingMethods.setSpatialHintLookAtRotation = provider.LookAt;
                IControlSpatialHintingMethods.setSpatialHintDragThresholdTriggerPosition = provider.SetDragThresholdTriggerPosition;
                IControlSpatialHintingMethods.pulseSpatialHintScrollArrows = provider.PulseScrollArrows;
                IControlSpatialHintingMethods.setSpatialHintControlNode = provider.SetSpatialHintControlNode;
            }
        }
    }
}
#endif
