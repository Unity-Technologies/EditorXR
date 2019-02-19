using UnityEditor.Experimental.EditorVR.Modules;

namespace UnityEditor.Experimental.EditorVR.Core
{
    partial class EditorVR
    {
        class DeviceInputModuleConnector : Nested, IInterfaceConnector
        {
            public void ConnectInterface(object target, object userData = null)
            {
                var trackedObjectMap = target as ITrackedObjectActionMap;
                if (trackedObjectMap != null)
                    trackedObjectMap.trackedObjectInput = evr.GetModule<DeviceInputModule>().trackedObjectInput;

                var processInput = target as IProcessInput;
                if (processInput != null && !(target is ITool)) // Tools have their input processed separately
                    evr.GetModule<DeviceInputModule>().AddInputProcessor(processInput, userData);
            }

            public void DisconnectInterface(object target, object userData = null)
            {
                var processInput = target as IProcessInput;
                if (processInput != null)
                    evr.GetModule<DeviceInputModule>().RemoveInputProcessor(processInput);
            }
        }
    }
}

