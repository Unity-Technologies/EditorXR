#if UNITY_EDITOR && UNITY_2017_2_OR_NEWER
using UnityEditor.Experimental.EditorVR.Modules;

namespace UnityEditor.Experimental.EditorVR.Core
{
    partial class EditorVR
    {
        class SpatialScrollModuleConnector : Nested, ILateBindInterfaceMethods<SpatialScrollModule>
        {
            public void LateBindInterfaceMethods(SpatialScrollModule provider)
            {
                IControlSpatialScrollingMethods.performSpatialScrollDeprecated = provider.PerformScroll;
                IControlSpatialScrollingMethods.endSpatialScroll = provider.EndScroll;
            }
        }
    }
}
#endif
