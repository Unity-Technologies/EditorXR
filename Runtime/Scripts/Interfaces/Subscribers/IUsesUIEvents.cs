using System;
using Unity.EditorXR.Modules;
using Unity.XRTools.ModuleLoader;
using UnityEngine;

namespace Unity.EditorXR.Interfaces
{
    /// <summary>
    /// Gives decorated class access to UI events
    /// </summary>
    interface IUsesUIEvents : IFunctionalitySubscriber<IProvidesUIEvents>
    {
    }

    static class UseUIEventsMethods
    {
        /// <summary>
        /// Subscribe to the pointerDown event
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="action">The action which will be called when the event occurs</param>
        public static void SubscribeToPointerDown(this IUsesUIEvents user, Action<GameObject, RayEventData> action)
        {
#if !FI_AUTOFILL
            user.provider.pointerDown += action;
#endif
        }

        /// <summary>
        /// Unsubscribe from the pointerDown event
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="action">The action which will be unsubscribed from the event</param>
        public static void UnsubscribeFromPointerDown(this IUsesUIEvents user, Action<GameObject, RayEventData> action)
        {
#if !FI_AUTOFILL
            user.provider.pointerDown -= action;
#endif
        }

        /// <summary>
        /// Subscribe to the dragStarted event
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="action">The action which will be called when the event occurs</param>
        public static void SubscribeToDragStarted(this IUsesUIEvents user, Action<GameObject, RayEventData> action)
        {
#if !FI_AUTOFILL
            user.provider.dragStarted += action;
#endif
        }

        /// <summary>
        /// Unsubscribe from the dragStarted event
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="action">The action which will be unsubscribed from the event</param>
        public static void UnsubscribeFromDragStarted(this IUsesUIEvents user, Action<GameObject, RayEventData> action)
        {
#if !FI_AUTOFILL
            user.provider.dragStarted -= action;
#endif
        }

        /// <summary>
        /// Subscribe to the dragEnded event
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="action">The action which will be called when the event occurs</param>
        public static void SubscribeToDragEnded(this IUsesUIEvents user, Action<GameObject, RayEventData> action)
        {
#if !FI_AUTOFILL
            user.provider.dragEnded += action;
#endif
        }

        /// <summary>
        /// Unsubscribe from the dragEnded event
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="action">The action which will be unsubscribed from the event</param>
        public static void UnsubscribeFromDragEnded(this IUsesUIEvents user, Action<GameObject, RayEventData> action)
        {
#if !FI_AUTOFILL
            user.provider.dragEnded -= action;
#endif
        }

        /// <summary>
        /// Subscribe to the pointerUp event
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="action">The action which will be called when the event occurs</param>
        public static void SubscribeToPointerUp(this IUsesUIEvents user, Action<GameObject, RayEventData> action)
        {
#if !FI_AUTOFILL
            user.provider.pointerUp += action;
#endif
        }

        /// <summary>
        /// Unsubscribe from the pointerUp event
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="action">The action which will be unsubscribed from the event</param>
        public static void UnsubscribeFromPointerUp(this IUsesUIEvents user, Action<GameObject, RayEventData> action)
        {
#if !FI_AUTOFILL
            user.provider.pointerUp -= action;
#endif
        }

        /// <summary>
        /// Subscribe to the rayEntered event
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="action">The action which will be called when the event occurs</param>
        public static void SubscribeToRayEntered(this IUsesUIEvents user, Action<GameObject, RayEventData> action)
        {
#if !FI_AUTOFILL
            user.provider.rayEntered += action;
#endif
        }

        /// <summary>
        /// Unsubscribe from the rayEntered event
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="action">The action which will be unsubscribed from the event</param>
        public static void UnsubscribeFromRayEntered(this IUsesUIEvents user, Action<GameObject, RayEventData> action)
        {
#if !FI_AUTOFILL
            user.provider.rayEntered -= action;
#endif
        }

        /// <summary>
        /// Subscribe to the rayExited event
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="action">The action which will be called when the event occurs</param>
        public static void SubscribeToRayExited(this IUsesUIEvents user, Action<GameObject, RayEventData> action)
        {
#if !FI_AUTOFILL
            user.provider.rayExited += action;
#endif
        }

        /// <summary>
        /// Unsubscribe from the rayExited event
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="action">The action which will be unsubscribed from the event</param>
        public static void UnsubscribeFromRayExited(this IUsesUIEvents user, Action<GameObject, RayEventData> action)
        {
#if !FI_AUTOFILL
            user.provider.rayExited -= action;
#endif
        }

        /// <summary>
        /// Subscribe to the rayHovering event
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="action">The action which will be called when the event occurs</param>
        public static void SubscribeToRayHovering(this IUsesUIEvents user, Action<GameObject, RayEventData> action)
        {
#if !FI_AUTOFILL
            user.provider.rayHovering += action;
#endif
        }

        /// <summary>
        /// Unsubscribe from the rayHovering event
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="action">The action which will be unsubscribed from the event</param>
        public static void UnsubscribeFromRayHovering(this IUsesUIEvents user, Action<GameObject, RayEventData> action)
        {
#if !FI_AUTOFILL
            user.provider.rayHovering -= action;
#endif
        }

        /// <summary>
        /// Subscribe to the clicked event
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="action">The action which will be called when the event occurs</param>
        public static void SubscribeToClicked(this IUsesUIEvents user, Action<GameObject, RayEventData> action)
        {
#if !FI_AUTOFILL
            user.provider.clicked += action;
#endif
        }

        /// <summary>
        /// Unsubscribe from the clicked event
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="action">The action which will be unsubscribed from the event</param>
        public static void UnsubscribeFromClicked(this IUsesUIEvents user, Action<GameObject, RayEventData> action)
        {
#if !FI_AUTOFILL
            user.provider.clicked -= action;
#endif
        }
    }
}
