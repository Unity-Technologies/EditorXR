using Unity.Labs.ModuleLoader;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Gives decorated class the ability to request a stencil reference
    /// </summary>
    public interface IUsesRequestStencilRef : IFunctionalitySubscriber<IProvidesRequestStencilRef>
    {
    }

    /// <summary>
    /// Extension methods for implementors of IUsesRequestStencilRef
    /// </summary>
    public static class UsesRequestStencilRefMethods
    {
        /// <summary>
        /// Get a new unique stencil ref value
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <returns>A unique stencil reference value</returns>
        public static byte RequestStencilRef(this IUsesRequestStencilRef user)
        {
#if FI_AUTOFILL
            return default(byte);
#else
            return user.provider.RequestStencilRef();
#endif
        }
    }
}
