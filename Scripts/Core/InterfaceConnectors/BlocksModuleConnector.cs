#if UNITY_EDITOR
using UnityEditor.Experimental.EditorVR;
using UnityEditor.Experimental.EditorVR.Modules;

[assembly: OptionalDependency("PolyToolkit.PolyApi", "INCLUDE_POLY_TOOLKIT")]

#if INCLUDE_POLY_TOOLKIT
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
#endif
