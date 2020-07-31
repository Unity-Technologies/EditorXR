using Unity.XRTools.ModuleLoader;

namespace Unity.EditorXR.Interfaces
{
    /// <summary>
    /// Provide access to scene raycast functionality
    /// </summary>
    public interface IProvidesCustomHighlight: IFunctionalityProvider
    {
        /// <summary>
        /// Subscribe to an event which will be called when trying to highlight each object
        /// </summary>
        /// <param name="highlightMethod">A method which will be called before highlighting each object</param>
        void SubscribeToOnHighlight(OnHighlightMethod highlightMethod);

        /// <summary>
        /// Unsubscribe from an event which will be called when trying to highlight each object
        /// </summary>
        /// <param name="highlightMethod">The method which was originally subscribed</param>
        void UnsubscribeFromOnHighlight(OnHighlightMethod highlightMethod);
    }
}
