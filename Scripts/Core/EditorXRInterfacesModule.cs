#if UNITY_2018_3_OR_NEWER
using System;
using System.Collections.Generic;
using Unity.Labs.ModuleLoader;

namespace UnityEditor.Experimental.EditorVR.Core
{
    [ModuleOrder(ModuleOrders.InterfaceModule)]
    class EditorXRInterfacesModule : IModule
    {
        readonly HashSet<object> m_ConnectedInterfaces = new HashSet<object>();

        event Action<object, object> connectInterfaces;
        event Action<object, object> disconnectInterfaces;

        public void LoadModule()
        {
            IConnectInterfacesMethods.connectInterfaces = ConnectInterfaces;
            IConnectInterfacesMethods.disconnectInterfaces = DisconnectInterfaces;
        }

        public void UnloadModule() { }

        internal void AttachInterfaceConnectors(object target)
        {
            var connector = target as IInterfaceConnector;
            if (connector != null)
            {
                connectInterfaces += connector.ConnectInterface;
                disconnectInterfaces += connector.DisconnectInterface;
            }
        }

        void ConnectInterfaces(object target, object userData = null)
        {
            if (!m_ConnectedInterfaces.Add(target))
                return;

            if (connectInterfaces != null)
                connectInterfaces(target, userData);
        }

        void DisconnectInterfaces(object target, object userData = null)
        {
            m_ConnectedInterfaces.Remove(target);

            if (disconnectInterfaces != null)
                disconnectInterfaces(target, userData);
        }
    }
}
#endif
