using Unity.XRTools.ModuleLoader;

namespace Unity.EditorXR.Interfaces
{
    /// <summary>
    /// Provides the ability to connect interfaces for spawned objects
    /// </summary>
    public interface IProvidesConnectInterfaces : IFunctionalityProvider
    {
        /// <summary>
        /// Method provided by the system for connecting interfaces
        /// </summary>
        /// <param name="target">Object to connect interfaces on</param>
        /// <param name="userData">(Optional) extra data needed to connect interfaces on this object</param>
        void ConnectInterfaces(object target, object userData = null);

        /// <summary>
        /// Method provided by the system for disconnecting interfaces
        /// </summary>
        /// <param name="target">Object to disconnect interfaces on</param>
        /// <param name="userData">(Optional) extra data needed to connect interfaces on this object</param>
        void DisconnectInterfaces(object target, object userData = null);
    }
}
