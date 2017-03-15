using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Core
{
	partial class EditorVR
	{
		class LockModuleConnector : Nested, IInterfaceConnector
		{
			public void ConnectInterface(object obj, Transform rayOrigin = null)
			{
				var evrLockModule = evr.m_LockModule;

				var locking = obj as IUsesGameObjectLocking;
				if (locking != null)
				{
					locking.setLocked = evrLockModule.SetLocked;
					locking.isLocked = evrLockModule.IsLocked;
				}
			}

			public void DisconnectInterface(object obj)
			{
			}
		}
	}

}
