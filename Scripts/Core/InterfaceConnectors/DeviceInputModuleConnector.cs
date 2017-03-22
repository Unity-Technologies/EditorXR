using UnityEditor.Experimental.EditorVR.Modules;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Core
{
	partial class EditorVR
	{
		class DeviceInputModuleConnector : Nested, IInterfaceConnector
		{
			public void ConnectInterface(object obj, Transform rayOrigin = null)
			{
				var evrDeviceInputModule = evr.GetModule<DeviceInputModule>();

				// Tracked Object action maps shouldn't block each other so we share an instance
				var trackedObjectMap = obj as ITrackedObjectActionMap;
				if (trackedObjectMap != null)
					trackedObjectMap.trackedObjectInput = evrDeviceInputModule.trackedObjectInput;
			}

			public void DisconnectInterface(object obj)
			{
			}
		}
	}
}
