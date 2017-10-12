#if UNITY_EDITOR && UNITY_2017_2_OR_NEWER
using UnityEditor.Experimental.EditorVR.Modules;

namespace UnityEditor.Experimental.EditorVR.Core
{
    partial class EditorVR
    {
        class TooltipModuleConnector : Nested, ILateBindInterfaceMethods<TooltipModule>
        {
            public void LateBindInterfaceMethods(TooltipModule provider)
            {
                ISetTooltipVisibilityMethods.showTooltip = provider.ShowTooltip;
                ISetTooltipVisibilityMethods.hideTooltip = provider.HideTooltip;
            }
        }
    }
}
#endif
