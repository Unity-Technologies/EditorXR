using System;
using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Gives decorated class the ability control the tool preview
    /// </summary>
    public interface IUsesPreviewInToolsMenuButton : IFunctionalitySubscriber<IProvidesPreviewInToolMenuButton>
    {
    }

    /// <summary>
    /// Extension methods for implementors of IUsesPreviewInToolsMenuButton
    /// </summary>
    public static class UsesPreviewInToolsMenuButtonMethods
    {
      /// <summary>
      /// Highlights a ToolMenuButton when a menu button is highlighted
      /// </summary>
      /// <param name="user">The functionality user</param>
      /// <param name="rayOrigin">Transform: Ray origin to check</param>
      /// <param name="toolType">Type: MenuButton's tool type to preview</param>
      /// <param name="toolDescription">String: The tool description to display as a Tooltip</param>
      public static void PreviewInToolsMenuButton(this IUsesPreviewInToolsMenuButton user, Transform rayOrigin, Type toolType, string toolDescription)
      {
#if !FI_AUTOFILL
            user.provider.PreviewInToolsMenuButton(rayOrigin, toolType, toolDescription);
#endif
      }

      /// <summary>
      /// Clears any ToolMenuButton previews that are set
      /// </summary>
      /// <param name="user">The functionality user</param>
      public static void ClearToolsMenuButtonPreview(this IUsesPreviewInToolsMenuButton user)
      {
#if !FI_AUTOFILL
            user.provider.ClearToolsMenuButtonPreview();
#endif
      }
    }
}
