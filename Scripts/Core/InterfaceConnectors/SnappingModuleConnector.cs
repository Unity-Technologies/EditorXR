using UnityEditor.Experimental.EditorVR.Modules;

namespace UnityEditor.Experimental.EditorVR.Core
{
	partial class EditorVR
	{
		class SnappingModuleConnector : Nested, ILateBindInterfaceMethods<SnappingModule>
		{
			public void LateBindInterfaceMethods(SnappingModule provider)
			{
				IUsesSnappingMethods.translateWithSnapping = provider.ManipulatorSnapping;
				IUsesSnappingMethods.directTransformWithSnapping = provider.DirectSnapping;
				IUsesSnappingMethods.clearSnappingState = provider.ClearSnappingState;
			}
		}
	}
}
