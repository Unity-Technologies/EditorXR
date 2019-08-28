using System;
using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Provide the ability control the tool preview
    /// </summary>
    public interface IProvidesPreviewInToolMenuButton : IFunctionalityProvider
    {
      /// <summary>
      /// Highlights a ToolMenuButton when a menu button is highlighted
      /// <param name="rayOrigin">Transform: Ray origin to check</param>
      /// <param name="toolType">Type: MenuButton's tool type to preview</param>
      /// <param name="toolDescription">String: The tool description to display as a Tooltip</param>
      /// </summary>
      void PreviewInToolsMenuButton(Transform rayOrigin, Type toolType, string toolDescription);

      /// <summary>
      /// Clears any ToolMenuButton previews that are set
      /// </summary>
      void ClearToolsMenuButtonPreview();
    }
}
