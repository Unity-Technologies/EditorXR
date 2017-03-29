using UnityEditor.Experimental.EditorVR.Modules;

namespace UnityEditor.Experimental.EditorVR.Core
{
	partial class EditorVR
	{
		class SnappingModuleConnector : Nested, ILateBindInterfaceMethods<SnappingModule>
		{
			public void LateBindInterfaceMethods(SnappingModule provider)
			{
				IUsesSnappingMethods.manipulatorSnapping = provider.ManipulatorSnapping;
				IUsesSnappingMethods.directSnapping = provider.DirectSnapping;
				IUsesSnappingMethods.clearSnappingState = provider.ClearSnappingState;
			}
		}
	}
}
