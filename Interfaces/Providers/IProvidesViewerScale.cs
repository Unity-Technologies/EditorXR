using Unity.XRTools.ModuleLoader;

namespace Unity.EditorXR.Interfaces
{
    /// <summary>
    /// Provide access to scene raycast functionality
    /// </summary>
    public interface IProvidesViewerScale : IFunctionalityProvider
    {
        /// <summary>
        /// Get the scale of the viewer object
        /// </summary>
        /// <returns>The viewer scale</returns>
        float GetViewerScale();

        /// <summary>
        /// Set the scale of the viewer object
        /// </summary>
        /// <param name="scale">Uniform scale value in world space</param>
        void SetViewerScale(float scale);
    }
}
