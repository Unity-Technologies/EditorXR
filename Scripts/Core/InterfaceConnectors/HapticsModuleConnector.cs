#if ENABLE_EDITORXR
using UnityEditor.Experimental.EditorVR.Modules;

namespace UnityEditor.Experimental.EditorVR.Core
{
    partial class EditorVR
    {
        class HapticsModuleConnector : Nested, ILateBindInterfaceMethods<HapticsModule>
        {
            public void LateBindInterfaceMethods(HapticsModule provider)
            {
                IControlHapticsMethods.pulse = provider.Pulse;
                IControlHapticsMethods.stopPulses = provider.StopPulses;
            }
        }
    }
}
#endif
