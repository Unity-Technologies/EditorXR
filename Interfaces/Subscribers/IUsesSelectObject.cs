using System.Collections.Generic;
using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Gives decorated class access to scene object selection
    /// </summary>
    public interface IUsesSelectObject : IFunctionalitySubscriber<IProvidesSelectObject>
    {
    }

    /// <summary>
    /// Extension methods for implementors of IUsesSelectObject
    /// </summary>
    public static class UsesSelectObjectMethods
    {
        /// <summary>
        /// Given a hovered object, find what object would actually be selected
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="hoveredObject">The hovered object that is being tested for selection</param>
        /// <param name="useGrouping">Use group selection</param>
        /// <returns>Returns what object would be selected by selectObject</returns>
        public static GameObject GetSelectionCandidate(this IUsesSelectObject user, GameObject hoveredObject, bool useGrouping = false)
        {
#if FI_AUTOFILL
            return default(GameObject);
#else
            return user.provider.GetSelectionCandidate(hoveredObject, useGrouping);
#endif
        }

        /// <summary>
        /// Select the given object using the given rayOrigin
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="hoveredObject">The hovered object</param>
        /// <param name="rayOrigin">The rayOrigin used for selection</param>
        /// <param name="multiSelect">Whether to add the hovered object to the selection, or override the current selection</param>
        /// <param name="useGrouping">Use group selection</param>
        public static void SelectObject(this IUsesSelectObject user, GameObject hoveredObject, Transform rayOrigin, bool multiSelect, bool useGrouping = false)
        {
#if !FI_AUTOFILL
            user.provider.SelectObject(hoveredObject, rayOrigin, multiSelect, useGrouping);
#endif
        }

        /// <summary>
        /// Select the given objects using the given rayOrigin
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="hoveredObjects">The hovered objects</param>
        /// <param name="rayOrigin">The rayOrigin used for selection</param>
        /// <param name="multiSelect">Whether to add the hovered object to the selection, or override the current selection</param>
        /// <param name="useGrouping">Use group selection</param>
        public static void SelectObjects(this IUsesSelectObject user, List<GameObject> hoveredObjects, Transform rayOrigin, bool multiSelect, bool useGrouping = false)
        {
#if !FI_AUTOFILL
            user.provider.SelectObjects(hoveredObjects, rayOrigin, multiSelect, useGrouping);
#endif
        }
    }
}
