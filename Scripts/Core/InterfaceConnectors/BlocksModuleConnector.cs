#if UNITY_EDITOR
using UnityEditor.Experimental.EditorVR.Modules;

namespace UnityEditor.Experimental.EditorVR.Core
{
    partial class EditorVR
    {
        class BlocksModuleConnector : Nested, ILateBindInterfaceMethods<BlocksModule>
        {
            public void LateBindInterfaceMethods(BlocksModule provider)
            {
                IBlocksMethods.getFeaturedModels = provider.GetFeaturedModels;
            }
        }
    }
}
#endif
