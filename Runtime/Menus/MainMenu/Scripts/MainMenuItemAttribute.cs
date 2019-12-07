using System;
using Unity.Labs.EditorXR.Interfaces;

namespace Unity.Labs.EditorXR
{
    /// <summary>
    /// Attribute used to tag items (tools, actions, etc) that can be added to VR menus
    /// </summary>
    public class MainMenuItemAttribute : System.Attribute
    {
        internal string name;
        internal string sectionName;
        internal string description;
        internal bool shown;
        internal ITooltip tooltip;

        /// <summary>
        /// Custom constructor for hiding item from the main menu
        /// </summary>
        /// <param name="shown"></param>
        public MainMenuItemAttribute(bool shown)
        {
            this.shown = shown;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">Display name of this tool, action, workspace, etc.</param>
        /// <param name="sectionName">Section to place this tool, action, workspace, etc.</param>
        /// <param name="description">Description of this tool, action, workspace, etc.</param>
        /// <param name="tooltipType">(Optional) Tooltip type if a tooltip is needed</param>
        public MainMenuItemAttribute(string name, string sectionName, string description, Type tooltipType = null)
        {
            this.name = name;
            this.sectionName = sectionName;
            this.description = description;
            this.shown = true;
            this.tooltip = tooltipType != null && typeof(ITooltip).IsAssignableFrom(tooltipType) ? (ITooltip)Activator.CreateInstance(tooltipType) : null;
        }
    }
}
