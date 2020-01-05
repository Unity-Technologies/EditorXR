using Unity.Labs.ModuleLoader;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Gives decorated class access to viewer scale
    /// </summary>
    public interface IUsesViewerScale : IFunctionalitySubscriber<IProvidesViewerScale>
    {
    }

    /// <summary>
    /// Extension methods for implementors of IUsesViewerScale
    /// </summary>
    public static class UsesViewerScaleMethods
    {
        /// <summary>
        /// Returns the scale of the viewer object
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <returns>The viewer scale</returns>
        public static float GetViewerScale(this IUsesViewerScale user)
        {
#if FI_AUTOFILL
            return default(float);
#else
            return user.provider.GetViewerScale();
#endif
        }

        /// <summary>
        /// Set the scale of the viewer object
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="scale">Uniform scale value in world space</param>
        public static void SetViewerScale(this IUsesViewerScale user, float scale)
        {
#if !FI_AUTOFILL
            user.provider.SetViewerScale(scale);
#endif
        }
    }
}
