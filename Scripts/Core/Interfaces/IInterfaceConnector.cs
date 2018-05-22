
namespace UnityEditor.Experimental.EditorVR.Core
{
    interface IInterfaceConnector
    {
        void ConnectInterface(object target, object userData = null);
        void DisconnectInterface(object target, object userData = null);
    }
}

