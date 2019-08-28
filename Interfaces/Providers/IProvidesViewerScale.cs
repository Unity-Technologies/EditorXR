using Unity.Labs.ModuleLoader;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Provide access to scene raycast functionality
    /// </summary>
    public interface IProvidesViewerScale : IFunctionalityProvider
    {
      /// <summary>
      /// Returns the scale of the viewer object
      /// </summary>
      float GetViewerScale();

      /// <summary>
      /// Set the scale of the viewer object
      /// </summary>
      /// <param name="scale">Uniform scale value in world space</param>
      void SetViewerScale(float scale);
    }
}
