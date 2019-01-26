using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Gives decorated class access to direct selections
    /// </summary>
    public interface IUsesDirectSelection
    {
        /// <summary>
        /// Called by the system whenever any implementor calls ResetDirectSelectionState
        /// </summary>
        void OnResetDirectSelectionState();
    }

    public static class IUsesDirectSelectionMethods
    {
        internal delegate Dictionary<Transform, GameObject> GetDirectSelectionDelegate();

        internal static GetDirectSelectionDelegate getDirectSelection { get; set; }
        internal static Action resetDirectSelectionState { get; set; }

        /// <summary>
        /// Returns a dictionary of direct selections
        /// </summary>
        /// <returns>Dictionary (K,V) where K = rayOrigin used to select the object and V = info about the direct selection</returns>
        public static Dictionary<Transform, GameObject> GetDirectSelection(this IUsesDirectSelection obj)
        {
            return getDirectSelection();
        }

        /// <summary>
        /// Calls OnResetDirectSelectionState on all implementors of IUsesDirectSelection
        /// </summary>
        public static void ResetDirectSelectionState(this IUsesDirectSelection obj)
        {
            resetDirectSelectionState();
        }
    }
}
