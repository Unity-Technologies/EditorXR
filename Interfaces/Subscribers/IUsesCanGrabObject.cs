using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Gives decorated class the ability to check if an object can be grabbed
    /// </summary>
    public interface IUsesCanGrabObject : IFunctionalitySubscriber<IProvidesCanGrabObject>
    {
    }

    public static class UsesCanGrabObjectMethods
    {
        /// <summary>
        /// Returns true if the object can be grabbed
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="gameObject">The selection</param>
        /// <param name="rayOrigin">The rayOrigin of the proxy that is looking to grab</param>
        /// <returns>True if the object can be grabbed</returns>
        public static bool CanGrabObject(this IUsesCanGrabObject user, GameObject gameObject, Transform rayOrigin)
        {
#if FI_AUTOFILL
            return default(bool);
#else
            return user.provider.CanGrabObject(gameObject, rayOrigin);
#endif
        }
    }
}
