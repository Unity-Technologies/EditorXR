using System.Collections.Generic;
using Unity.XRTools.ModuleLoader;
using UnityEngine;

namespace Unity.EditorXR.Interfaces
{
    /// <summary>
    /// Gives decorated class access to the VR Player objects
    /// </summary>
    public interface IUsesGetVRPlayerObjects : IFunctionalitySubscriber<IProvidesGetVRPlayerObjects>
    {
    }

    /// <summary>
    /// Extension methods for implementors of IUsesGetVRPlayerObjects
    /// </summary>
    public static class UsesGetVRPlayerObjectsMethods
    {
        /// <summary>
        /// Returns objects that are used to represent the VR player
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <returns>The list of VR Player objects</returns>
        public static List<GameObject> GetVRPlayerObjects(this IUsesGetVRPlayerObjects user)
        {
#if FI_AUTOFILL
            return default(List<GameObject>);
#else
            return user.provider.GetVRPlayerObjects();
#endif
        }
    }
}
