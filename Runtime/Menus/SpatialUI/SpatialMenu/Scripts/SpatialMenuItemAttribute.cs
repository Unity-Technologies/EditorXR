namespace Unity.EditorXR
{
    /// <summary>
    /// Attribute used to tag items (tools, actions, etc) that can be added to a "spatial menu"
    /// </summary>
    public class SpatialMenuItemAttribute : System.Attribute
    {
        internal string name;
        internal string sectionName;
        internal string description;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">Display name of this tool, action, workspace, etc.</param>
        /// <param name="sectionName">Section to place this tool, action, workspace, etc.</param>
        /// <param name="description">Description of this tool, action, workspace, etc.</param>
        public SpatialMenuItemAttribute(string name, string sectionName, string description)
        {
            this.name = name;
            this.sectionName = sectionName;
            this.description = description;
        }
    }
}
