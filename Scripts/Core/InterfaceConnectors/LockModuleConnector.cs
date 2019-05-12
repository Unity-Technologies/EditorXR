#if UNITY_2018_3_OR_NEWER
using UnityEditor.Experimental.EditorVR.Modules;

namespace UnityEditor.Experimental.EditorVR.Core
{
    partial class EditorVR
    {
        class LockModuleConnector : Nested, ILateBindInterfaceMethods<LockModule>
        {
            public void LateBindInterfaceMethods(LockModule provider)
            {
                IUsesGameObjectLockingMethods.setLocked = provider.SetLocked;
                IUsesGameObjectLockingMethods.isLocked = provider.IsLocked;
            }
        }
    }
}
#endif
