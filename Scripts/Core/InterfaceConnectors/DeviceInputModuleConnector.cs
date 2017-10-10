#if UNITY_EDITOR && UNITY_2017_2_OR_NEWER
using UnityEditor.Experimental.EditorVR.Modules;

namespace UnityEditor.Experimental.EditorVR.Core
{
	partial class EditorVR
	{
		class DeviceInputModuleConnector : Nested, IInterfaceConnector
		{
			public void ConnectInterface(object @object, object userData = null)
			{
				var trackedObjectMap = @object as ITrackedObjectActionMap;
				if (trackedObjectMap != null)
					trackedObjectMap.trackedObjectInput = evr.GetModule<DeviceInputModule>().trackedObjectInput;

				var processInput = @object as IProcessInput;
				if (processInput != null && !(@object is ITool)) // Tools have their input processed separately
					evr.GetModule<DeviceInputModule>().AddInputProcessor(processInput, userData);
			}

			public void DisconnectInterface(object @object, object userData = null)
			{
				var processInput = @object as IProcessInput;
				if (processInput != null)
					evr.GetModule<DeviceInputModule>().RemoveInputProcessor(processInput);
			}
		}
	}
}
#endif
