using System;
using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Gives decorated class the ability to add raycast sources to the system
    /// </summary>
    interface IUsesAddRaycastSource : IFunctionalitySubscriber<IProvidesAddRaycastSource>
    {
    }

    /// <summary>
    /// Extension methods for implementors of IUsesAddRaycastSource
    /// </summary>
    static class UsesAddRaycastSourceMethods
    {
        public static void AddRaycastSource(this IUsesAddRaycastSource user, IProxy proxy, Node node, Transform rayOrigin, Func<IRaycastSource, bool> validationCallback = null)
        {
#if !FI_AUTOFILL
            user.provider.AddRaycastSource(proxy, node, rayOrigin, validationCallback);
#endif
        }
    }
}
