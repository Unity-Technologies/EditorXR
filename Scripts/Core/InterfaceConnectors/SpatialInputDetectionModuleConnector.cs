#if UNITY_EDITOR && UNITY_2017_2_OR_NEWER
using UnityEditor.Experimental.EditorVR.Modules;

namespace UnityEditor.Experimental.EditorVR.Core
{
    partial class EditorVR
    {
        class SpatialInputDetectionModuleConnector : Nested, ILateBindInterfaceMethods<SpatialInputDetectionModule>
        {
            public void LateBindInterfaceMethods(SpatialInputDetectionModule provider)
            {
                IDetectSpatialInputTypeMethods.getSpatialInputTypeForNode = provider.GetSpatialInputTypeForNode;
            }
        }
    }
}
#endif
