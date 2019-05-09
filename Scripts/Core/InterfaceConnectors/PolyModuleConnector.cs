#if UNITY_2018_3_OR_NEWER
using Unity.Labs.Utils;
using UnityEditor.Experimental.EditorVR.Modules;

[assembly: OptionalDependency("PolyToolkit.PolyApi", "INCLUDE_POLY_TOOLKIT")]

#if INCLUDE_POLY_TOOLKIT
namespace UnityEditor.Experimental.EditorVR.Core
{
    partial class EditorVR
    {
        class PolyModuleConnector : Nested, ILateBindInterfaceMethods<PolyModule>
        {
            public void LateBindInterfaceMethods(PolyModule provider)
            {
                IPolyMethods.getAssetList = provider.GetAssetList;
            }
        }
    }
}
#endif
#endif
