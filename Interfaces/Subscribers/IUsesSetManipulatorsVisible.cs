using Unity.Labs.ModuleLoader;
using UnityEditor;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Gives decorated class access to viewer scale
    /// </summary>
    public interface IUsesSetManipulatorsVisible : IFunctionalitySubscriber<IProvidesSetManipulatorsVisible>
    {
    }

    public static class UsesSetManipulatorsVisibleMethods
    {
      /// <summary>
      /// Show or hide the manipulator(s)
      /// </summary>
      /// <param name="user">The functionality user</param>
      /// <param name="requester">The requesting object that is wanting to set all manipulators visible or hidden</param>
      /// <param name="visibility">Whether the manipulators should be shown or hidden</param>
      public static void SetManipulatorsVisible(this IUsesSetManipulatorsVisible user, IUsesSetManipulatorsVisible requester, bool visibility)
      {
#if !FI_AUTOFILL
            user.provider.SetManipulatorsVisible(requester, visibility);
#endif
      }
    }
}
