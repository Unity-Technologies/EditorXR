using Unity.Labs.ModuleLoader;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Decorates types that need to connect interfaces for spawned objects
    /// </summary>
    public interface IUsesConnectInterfaces : IFunctionalitySubscriber<IProvidesConnectInterfaces>
    {
    }

    /// <summary>
    /// Extension methods for implementors of IUsesConnectInterfaces
    /// </summary>
    public static class UsesConnectInterfacesMethods
    {
        /// <summary>
        /// Method provided by the system for connecting interfaces
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="target">Object to connect interfaces on</param>
        /// <param name="userData">(Optional) extra data needed to connect interfaces on this object</param>
        public static void ConnectInterfaces(this IUsesConnectInterfaces user, object target, object userData = null)
        {
#if !FI_AUTOFILL
            user.provider.ConnectInterfaces(target, userData);
#endif
        }

        /// <summary>
        /// Method provided by the system for disconnecting interfaces
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="target">Object to disconnect interfaces on</param>
        /// <param name="userData">(Optional) extra data needed to connect interfaces on this object</param>
        public static void DisconnectInterfaces(this IUsesConnectInterfaces user, object target, object userData = null)
        {
#if !FI_AUTOFILL
            user.provider.DisconnectInterfaces(target, userData);
#endif
        }
    }
}
