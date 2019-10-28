namespace UnityEditor.Experimental.EditorVR.Core
{
    public interface IInterfaceConnector
    {
        int connectInterfaceOrder { get; }
        void ConnectInterface(object target, object userData = null);
        void DisconnectInterface(object target, object userData = null);
    }
}
