using UnityEditor.Experimental.EditorVR.Modules;

namespace UnityEditor.Experimental.EditorVR.Core
{
    partial class EditorVR
    {
        class SnappingModuleConnector : Nested, ILateBindInterfaceMethods<SnappingModule>
        {
            public void LateBindInterfaceMethods(SnappingModule provider)
            {
                IUsesSnappingMethods.manipulatorSnap = provider.ManipulatorSnap;
                IUsesSnappingMethods.directSnap = provider.DirectSnap;
                IUsesSnappingMethods.clearSnappingState = provider.ClearSnappingState;
            }
        }
    }
}