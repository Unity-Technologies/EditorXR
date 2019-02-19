namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Used for passing action data for menu purposes
    /// </summary>
    public class ActionMenuData : ITooltip
    {
        /// <summary>
        /// The name of this action
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// The name of the section within which this action resides
        /// </summary>
        public string sectionName { get; set; }

        /// <summary>
        /// The ordinal of this action within its section
        /// </summary>
        public int priority { get; set; }

        /// <summary>
        /// An instance of the Action that can be used for execution
        /// </summary>
        public IAction action { get; set; }

        /// <summary>
        /// The tooltip text for this action
        /// </summary>
        public string tooltipText { get { return name; } }
    }
}
