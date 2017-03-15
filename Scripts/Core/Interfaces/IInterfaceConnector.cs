using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Core
{
	public interface IInterfaceConnector
	{
		void ConnectInterface(object obj, Transform rayOrigin = null);

		void DisconnectInterface(object obj);
	}
}
