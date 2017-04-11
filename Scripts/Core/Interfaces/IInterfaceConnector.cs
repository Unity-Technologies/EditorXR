#if UNITY_EDITOR
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Core
{
	interface IInterfaceConnector
	{
		void ConnectInterface(object obj, Transform rayOrigin = null);
		void DisconnectInterface(object obj);
	}
}
#endif
