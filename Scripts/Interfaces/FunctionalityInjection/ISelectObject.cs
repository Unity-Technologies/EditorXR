using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Gives access to the selection module
    /// </summary>
    public interface ISelectObject
    {
    }

    public static class ISelectObjectMethods
    {
        internal static Func<GameObject, bool, GameObject> getSelectionCandidate { get; set; }
        internal static Action<GameObject, Transform, bool, bool> selectObject { get; set; }
        internal static Action<List<GameObject>, Transform, bool, bool> selectObjects { get; set; }

        /// <summary>
        /// Given a hovered object, find what object would actually be selected
        /// </summary>
        /// <param name="hoveredObject">The hovered object that is being tested for selection</param>
        /// <param name="useGrouping">Use group selection</param>
        /// <returns>Returns what object would be selected by selectObject</returns>
        public static GameObject GetSelectionCandidate(this ISelectObject obj, GameObject hoveredObject, bool useGrouping = false)
        {
            return getSelectionCandidate(hoveredObject, useGrouping);
        }

        /// <summary>
        /// Select the given object using the given rayOrigin
        /// </summary>
        /// <param name="hoveredObject">The hovered object</param>
        /// <param name="rayOrigin">The rayOrigin used for selection</param>
        /// <param name="multiSelect">Whether to add the hovered object to the selection, or override the current selection</param>
        /// <param name="useGrouping">Use group selection</param>
        public static void SelectObject(this ISelectObject obj, GameObject hoveredObject, Transform rayOrigin, bool multiSelect, bool useGrouping = false)
        {
            selectObject(hoveredObject, rayOrigin, multiSelect, useGrouping);
        }

        /// <summary>
        /// Select the given objects using the given rayOrigin
        /// </summary>
        /// <param name="hoveredObjects">The hovered objects</param>
        /// <param name="rayOrigin">The rayOrigin used for selection</param>
        /// <param name="multiSelect">Whether to add the hovered object to the selection, or override the current selection</param>
        /// <param name="useGrouping">Use group selection</param>
        public static void SelectObjects(this ISelectObject obj, List<GameObject> hoveredObjects, Transform rayOrigin, bool multiSelect, bool useGrouping = false)
        {
            selectObjects(hoveredObjects, rayOrigin, multiSelect, useGrouping);
        }
    }
}
