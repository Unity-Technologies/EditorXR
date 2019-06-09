using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Gives decorated class access to IntersectionModule.Raycast
    /// </summary>
    public interface IUsesCustomHighlight : IFunctionalitySubscriber<IProvidesCustomHighlight>
    {
    }

    /// <summary>
    /// Method which will be called when highlighting each object
    /// </summary>
    /// <param name="go">The object which will be highlighted</param>
    /// <param name="material">The material which would be used to highlight it</param>
    /// <returns>Whether to block the normal highlight method</returns>
    public delegate bool OnHighlightMethod(GameObject go, Material material);

    public static class UsesCustomHighlight
    {
        /// <summary>
        /// Subscribe to an event which will be called when trying to highlight each object
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="highlightMethod">A method which will be called before highlighting each object</param>
        public static void SubscribeToOnHighlight(this IUsesCustomHighlight user, OnHighlightMethod highlightMethod)
        {
#if !FI_AUTOFILL
            user.provider.SubscribeToOnHighlight(highlightMethod);
#endif
        }

        /// <summary>
        /// Unsubscribe from an event which will be called when trying to highlight each object
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="highlightMethod">The method which was originally subscribed</param>
        public static void UnsubscribeFromOnHighlight(this IUsesCustomHighlight user, OnHighlightMethod highlightMethod)
        {
#if !FI_AUTOFILL
            user.provider.SubscribeToOnHighlight(highlightMethod);
#endif
        }
    }
}
