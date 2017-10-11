#if UNITY_EDITOR
using System;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Decorates types that need to connect interfaces for spawned objects
    /// </summary>
    interface IConnectInterfaces
   {
   }

    static class IConnectInterfacesMethods
    {
        internal static Action<object, object> connectInterfaces { get; set; }
        internal static Action<object, object> disconnectInterfaces { get; set; }

        /// <summary>
        /// Method provided by the system for connecting interfaces
        /// </summary>
        /// <param name="object">Object to connect interfaces on</param>
        /// <param name="userData">(Optional) extra data needed to connect interfaces on this object</param>
        public static void ConnectInterfaces(this IConnectInterfaces @this, object @object, object userData = null)
        {
            connectInterfaces(@object, userData);
        }

        /// <summary>
        /// Method provided by the system for disconnecting interfaces
        /// </summary>
        /// <param name="object">Object to disconnect interfaces on</param>
        /// <param name="userData">(Optional) extra data needed to connect interfaces on this object</param>
        public static void DisconnectInterfaces(this IConnectInterfaces @this, object @object, object userData = null)
        {
            disconnectInterfaces(@object, userData);
        }
    }
}
#endif
