#if UNITY_EDITOR && UNITY_2017_2_OR_NEWER
using UnityEditor.Experimental.EditorVR;
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
