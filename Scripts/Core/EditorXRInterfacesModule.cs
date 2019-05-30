#if UNITY_2018_3_OR_NEWER
using System;
using System.Collections.Generic;
using Unity.Labs.ModuleLoader;

namespace UnityEditor.Experimental.EditorVR.Core
{
    [ModuleOrder(ModuleOrders.InterfaceModule)]
    class EditorXRInterfacesModule : IInitializableModule
    {
        readonly HashSet<object> m_ConnectedInterfaces = new HashSet<object>();

        event Action<object, object> connectInterfaces;
        event Action<object, object> disconnectInterfaces;

        public int order { get { return 0; } }

        public void LoadModule()
        {
            IConnectInterfacesMethods.connectInterfaces = ConnectInterfaces;
            IConnectInterfacesMethods.disconnectInterfaces = DisconnectInterfaces;

            foreach (var module in ModuleLoaderCore.instance.modules)
            {
                var connector = module as IInterfaceConnector;
                if (connector != null)
                    AttachInterfaceConnector(connector);
            }
        }

        public void Initialize()
        {
            connectInterfaces = null;
            disconnectInterfaces = null;
            m_ConnectedInterfaces.Clear();
        }

        public void Shutdown() { }

        public void UnloadModule() { }

        void AttachInterfaceConnector(IInterfaceConnector connector)
        {
            connectInterfaces += connector.ConnectInterface;
            disconnectInterfaces += connector.DisconnectInterface;
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
