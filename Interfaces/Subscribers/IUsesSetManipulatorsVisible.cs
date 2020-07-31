using Unity.XRTools.ModuleLoader;
using UnityEngine;

namespace Unity.EditorXR.Interfaces
{
    /// <summary>
    /// Gives decorated class access to viewer scale
    /// </summary>
    public interface IUsesSetManipulatorsVisible : IFunctionalitySubscriber<IProvidesSetManipulatorsVisible>
    {
    }

    /// <summary>
    /// Extension methods for implementors of IUsesSetManipulatorsVisible
    /// </summary>
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
