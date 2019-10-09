using UnityEditor.Experimental.EditorVR.Modules;

namespace UnityEditor.Experimental.EditorVR.Core
{
    partial class EditorVR
    {
        class SpatialHashModuleConnector : Nested, ILateBindInterfaceMethods<SpatialHashModule>
        {
            public void LateBindInterfaceMethods(SpatialHashModule provider)
            {
                IUsesSpatialHashMethods.addToSpatialHash = provider.AddObject;
                IUsesSpatialHashMethods.removeFromSpatialHash = provider.RemoveObject;
            }
        }
    }
}