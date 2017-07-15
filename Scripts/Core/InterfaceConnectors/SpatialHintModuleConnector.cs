#if UNITY_EDITOR && UNITY_EDITORVR
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
				IControlSpatialHintingMethods.setSpatialHintRotation = provider.SetRotation;
				IControlSpatialHintingMethods.setSpatialHintRotationTarget = provider.SetRotationTarget;
				IControlSpatialHintingMethods.setSpatialHintLookAtRotation = provider.LookAt;
				IControlSpatialHintingMethods.setSpatialHintDragThresholdTriggerPosition = provider.SetDragThresholdTriggerPosition;
				IControlSpatialHintingMethods.pulseSpatialHintScrollArrows = provider.PulseScrollArrows;
			}
		}
	}
}
#endif
