using System;
using System.Collections.Generic;
using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Gives decorated class access to direct selection
    /// </summary>
    public interface IUsesDirectSelection : IFunctionalitySubscriber<IProvidesDirectSelection>
    {
    }

    public static class UsesDirectSelectionMethods
    {
        /// <summary>
        /// Returns a dictionary of direct selections
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <returns>Dictionary (K,V) where K = rayOrigin used to select the object and V = info about the direct selection</returns>
        public static Dictionary<Transform, DirectSelectionData> GetDirectSelection(this IUsesDirectSelection user)
        {
#if FI_AUTOFILL
            return default(Dictionary<Transform, DirectSelectionData>);
#else
            return user.provider.GetDirectSelection();
#endif
        }

        /// <summary>
        /// Calls OnResetDirectSelectionState on all subscribers to ResetDirectSelectionState
        /// </summary>
        /// <param name="user">The functionality user</param>
        public static void ResetDirectSelectionState(this IUsesDirectSelection user)
        {
#if !FI_AUTOFILL
            user.provider.ResetDirectSelectionState();
#endif
        }

        /// <summary>
        /// Subscribe to ResetDirectSelectionState
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="callback">The method that will be called when resetting direct selection state</param>
        public static void SubscribeToResetDirectSelectionState(this IUsesDirectSelection user, Action callback)
        {
#if !FI_AUTOFILL
            user.provider.SubscribeToResetDirectSelectionState(callback);
#endif
        }

        /// <summary>
        /// Calls OnResetDirectSelectionState on all implementors of IUsesDirectSelection
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="callback">The method that was originally subscribed</param>
        public static void UnsubscribeFromResetDirectSelectionState(this IUsesDirectSelection user, Action callback)
        {
#if !FI_AUTOFILL
            user.provider.SubscribeToResetDirectSelectionState(callback);
#endif
        }
    }
}
