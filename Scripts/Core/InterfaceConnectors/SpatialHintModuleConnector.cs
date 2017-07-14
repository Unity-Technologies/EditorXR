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
				IControlSpatialHintingMethods.pulseScrollArrows = provider.PulseScrollArrows;
			}
		}
	}
}
#endif
