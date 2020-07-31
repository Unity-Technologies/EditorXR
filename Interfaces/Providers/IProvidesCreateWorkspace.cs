using System;
using Unity.XRTools.ModuleLoader;

namespace Unity.EditorXR.Interfaces
{
    /// <summary>
    /// Provide the ability to create workspaces
    /// </summary>
    public interface IProvidesCreateWorkspace : IFunctionalityProvider
    {
        /// <summary>
        /// Method for creating workspaces
        /// </summary>
        /// <param name="type">Type of the workspace (must inherit from Workspace)</param>
        /// <param name="createdCallback">Called once the workspace is created</param>
        void CreateWorkspace(Type type, Action<IWorkspace> createdCallback = null);
    }
}
