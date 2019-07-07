#if UNITY_2018_3_OR_NEWER
using System;
using System.Collections.Generic;
using Unity.Labs.EditorXR.Interfaces;
using Unity.Labs.ModuleLoader;

namespace UnityEditor.Experimental.EditorVR.Core
{
    [ImmortalModule]
    [ModuleOrder(ModuleOrders.InterfaceModule)]
    class EditorXRInterfacesModule : IDelayedInitializationModule, IProvidesConnectInterfaces
    {
        readonly HashSet<object> m_ConnectedInterfaces = new HashSet<object>();

        event Action<object, object> connectInterfaces;
        event Action<object, object> disconnectInterfaces;

        public int initializationOrder { get { return -4; } }
        public int shutdownOrder { get { return 0; } }

        public void LoadModule()
        {
            var modules = ModuleLoaderCore.instance.modules;
            var interfaceConnectors = new List<IInterfaceConnector>();
            foreach (var module in modules)
            {
                var connector = module as IInterfaceConnector;
                if (connector != null)
                    interfaceConnectors.Add(connector);
            }

            interfaceConnectors.Sort((a, b) => a.connectInterfaceOrder.CompareTo(b.connectInterfaceOrder));

            foreach (var connector in interfaceConnectors)
            {
                AttachInterfaceConnector(connector);
            }

            // TODO: Remove when replacing FI
            foreach (var module in modules)
            {
                ConnectInterfaces(module);
            }
        }

        public void Initialize()
        {
            m_ConnectedInterfaces.Clear();
        }

        public void Shutdown() { }

        public void UnloadModule()
        {
            // TODO: Remove when replacing FI
            foreach (var module in ModuleLoaderCore.instance.modules)
            {
                DisconnectInterfaces(module);
            }

            connectInterfaces = null;
            disconnectInterfaces = null;
        }

        void AttachInterfaceConnector(IInterfaceConnector connector)
        {
            connectInterfaces += connector.ConnectInterface;
            disconnectInterfaces += connector.DisconnectInterface;
        }

        public void ConnectInterfaces(object target, object userData = null)
        {
            if (!m_ConnectedInterfaces.Add(target))
                return;

            if (connectInterfaces != null)
                connectInterfaces(target, userData);
        }

        public void DisconnectInterfaces(object target, object userData = null)
        {
            m_ConnectedInterfaces.Remove(target);

            if (disconnectInterfaces != null)
                disconnectInterfaces(target, userData);
        }

        public void LoadProvider() { }

        public void ConnectSubscriber(object obj)
        {
#if !FI_AUTOFILL
            var connectInterfacesSubscriber = obj as IFunctionalitySubscriber<IProvidesConnectInterfaces>;
            if (connectInterfacesSubscriber != null)
                connectInterfacesSubscriber.provider = this;
#endif
        }

        public void UnloadProvider() { }
    }
}
#endif
