using System;
using System.Collections.Generic;
using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Provide access to direct selection
    /// </summary>
    public interface IProvidesDirectSelection : IFunctionalityProvider
    {
        /// <summary>
        /// Returns a dictionary of direct selections
        /// </summary>
        /// <returns>Dictionary (K,V) where K = rayOrigin used to select the object and V = info about the direct selection</returns>
        Dictionary<Transform, DirectSelectionData> GetDirectSelection();

        /// <summary>
        /// Calls OnResetDirectSelectionState on all subscribers to ResetDirectSelectionState
        /// </summary>
        void ResetDirectSelectionState();

        /// <summary>
        /// Subscribe to ResetDirectSelectionState
        /// </summary>
        /// <param name="callback">The method that will be called when resetting direct selection state</param>
        void SubscribeToResetDirectSelectionState(Action callback);

        /// <summary>
        /// Unsubscribe from ResetDirectSelectionState
        /// </summary>
        /// <param name="callback">The method that was originally subscribed</param>
        void UnsubscribeFromResetDirectSelectionState(Action callback);
    }
}
