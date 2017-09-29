#if UNITY_EDITOR
namespace UnityEditor.Experimental.EditorVR.Core
{
	interface IInterfaceConnector
	{
		void ConnectInterface(object @object, object userData = null);
		void DisconnectInterface(object @object, object userData = null);
	}
}
#endif
