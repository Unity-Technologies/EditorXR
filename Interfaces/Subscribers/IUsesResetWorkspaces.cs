using Unity.XRTools.ModuleLoader;

namespace Unity.EditorXR.Interfaces
{
    /// <summary>
    /// Gives decorated class access to the ability to reset workspaces
    /// </summary>
    public interface IUsesResetWorkspaces : IFunctionalitySubscriber<IProvidesResetWorkspaces>
    {
    }

    /// <summary>
    /// Extension methods for implementors of IUsesResetWorkspaces
    /// </summary>
    public static class UsesResetWorkspacesMethods
    {
        /// <summary>
        /// Reset all open workspaces
        /// </summary>
        /// <param name="user">The functionality user</param>
        public static void ResetWorkspaceRotations(this IUsesResetWorkspaces user)
        {
#if !FI_AUTOFILL
            user.provider.ResetWorkspaceRotations();
#endif
        }
    }
}
