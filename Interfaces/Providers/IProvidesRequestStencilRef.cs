using Unity.XRTools.ModuleLoader;

namespace Unity.EditorXR.Interfaces
{
    /// <summary>
    /// Provide the ability to request a stencil reference
    /// </summary>
    public interface IProvidesRequestStencilRef : IFunctionalityProvider
    {
        /// <summary>
        /// Get a new unique stencil ref value
        /// </summary>
        /// <returns>A unique stencil reference value</returns>
        byte RequestStencilRef();
    }
}
