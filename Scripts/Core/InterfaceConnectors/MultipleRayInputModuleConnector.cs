#if UNITY_2018_4_OR_NEWER
using UnityEditor.Experimental.EditorVR.Modules;

namespace UnityEditor.Experimental.EditorVR.Core
{
    partial class EditorVR
    {
        class MultipleRayInputModuleConnector : Nested, ILateBindInterfaceMethods<MultipleRayInputModule>
        {
            public void LateBindInterfaceMethods(MultipleRayInputModule provider)
            {
                IIsHoveringOverUIMethods.isHoveringOverUI = provider.IsHoveringOverUI;
                IBlockUIInteractionMethods.setUIBlockedForRayOrigin = provider.SetUIBlockedForRayOrigin;
            }
        }
    }
}
#endif
