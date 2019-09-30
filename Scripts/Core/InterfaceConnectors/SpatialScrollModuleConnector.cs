#if UNITY_2018_4_OR_NEWER
using UnityEditor.Experimental.EditorVR.Modules;

namespace UnityEditor.Experimental.EditorVR.Core
{
    partial class EditorVR
    {
        class SpatialScrollModuleConnector : Nested, ILateBindInterfaceMethods<SpatialScrollModule>
        {
            public void LateBindInterfaceMethods(SpatialScrollModule provider)
            {
                IControlSpatialScrollingMethods.performSpatialScroll = provider.PerformScroll;
                IControlSpatialScrollingMethods.endSpatialScroll = provider.EndScroll;
            }
        }
    }
}
#endif
