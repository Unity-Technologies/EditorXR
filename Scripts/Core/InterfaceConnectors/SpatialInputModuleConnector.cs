#if UNITY_EDITOR && UNITY_2017_2_OR_NEWER
using UnityEditor.Experimental.EditorVR.Modules;

namespace UnityEditor.Experimental.EditorVR.Core
{
    partial class EditorVR
    {
        class SpatialInputModuleConnector : Nested, ILateBindInterfaceMethods<SpatialInputModule>
        {
            public void LateBindInterfaceMethods(SpatialInputModule provider)
            {
                IDetectSpatialInputTypeMethods.getSpatialInputTypeForNode = provider.GetSpatialInputTypeForNode;
                IProcessSpatialInputTypeMethods.performSpatialScroll = provider.PerformSpatialScroll;
            }
        }
    }
}
#endif
