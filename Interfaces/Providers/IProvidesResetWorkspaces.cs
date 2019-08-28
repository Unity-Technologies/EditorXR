using Unity.Labs.ModuleLoader;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Provide access to the ability to reset workspaces
    /// </summary>
    public interface IProvidesResetWorkspaces : IFunctionalityProvider
    {
        /// <summary>
        /// Reset all open workspaces
        /// </summary>
        void ResetWorkspaceRotations();
    }
}
