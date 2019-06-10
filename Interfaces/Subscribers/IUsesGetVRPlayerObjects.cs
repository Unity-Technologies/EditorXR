using Unity.Labs.ModuleLoader;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Gives decorated class access to the VR Player objects
    /// </summary>
    public interface IUsesGetVRPlayerObjects : IFunctionalitySubscriber<IProvidesGetVRPlayerObjects>
    {
    }

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
