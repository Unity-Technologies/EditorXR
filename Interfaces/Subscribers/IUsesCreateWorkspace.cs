using System;
using System.Collections.Generic;
using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Gives decorated class the ability to create workspaces
    /// </summary>
    public interface IUsesCreateWorkspace : IFunctionalitySubscriber<IProvidesCreateWorkspace>
    {
    }

    public static class UsesCreateWorkspaceMethods
    {
      /// <summary>
      /// Method for creating workspaces
      /// </summary>
      /// <param name="user">The functionality user</param>
      /// <param name="type">Type of the workspace (must inherit from Workspace)</param>
      /// <param name="createdCallback">Called once the workspace is created</param>
      public static void CreateWorkspace(this IUsesCreateWorkspace user, Type type, Action<IWorkspace> createdCallback = null)
      {
#if !FI_AUTOFILL
            user.provider.CreateWorkspace(type, createdCallback);
#endif
      }
    }
}
