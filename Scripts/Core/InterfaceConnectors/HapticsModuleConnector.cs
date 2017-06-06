#if UNITY_EDITOR && UNITY_EDITORVR
using UnityEditor.Experimental.EditorVR.Modules;

namespace UnityEditor.Experimental.EditorVR.Core
{
	partial class EditorVR
	{
		class HapticsModuleConnector : Nested, ILateBindInterfaceMethods<HapticsModule>
		{
			public void LateBindInterfaceMethods(HapticsModule provider)
			{
				IPerformHapticsMethods.pulse = provider.Pulse;
				IPerformHapticsMethods.stopPulses = provider.StopPulses;
			}
		}
	}
}
#endif
