using System.Collections.Generic;
using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Provide access to scene object selection
    /// </summary>
    public interface IProvidesSelectObject : IFunctionalityProvider
    {
        /// <summary>
        /// Given a hovered object, find what object would actually be selected
        /// </summary>
        /// <param name="hoveredObject">The hovered object that is being tested for selection</param>
        /// <param name="useGrouping">Use group selection</param>
        /// <returns>Returns what object would be selected by selectObject</returns>
        GameObject GetSelectionCandidate(GameObject hoveredObject, bool useGrouping = false);

        /// <summary>
        /// Select the given object using the given rayOrigin
        /// </summary>
        /// <param name="hoveredObject">The hovered object</param>
        /// <param name="rayOrigin">The rayOrigin used for selection</param>
        /// <param name="multiSelect">Whether to add the hovered object to the selection, or override the current selection</param>
        /// <param name="useGrouping">Use group selection</param>
        void SelectObject(GameObject hoveredObject, Transform rayOrigin, bool multiSelect, bool useGrouping = false);

        /// <summary>
        /// Select the given objects using the given rayOrigin
        /// </summary>
        /// <param name="hoveredObjects">The hovered objects</param>
        /// <param name="rayOrigin">The rayOrigin used for selection</param>
        /// <param name="multiSelect">Whether to add the hovered object to the selection, or override the current selection</param>
        /// <param name="useGrouping">Use group selection</param>
        void SelectObjects(List<GameObject> hoveredObjects, Transform rayOrigin, bool multiSelect, bool useGrouping = false);
    }
}
