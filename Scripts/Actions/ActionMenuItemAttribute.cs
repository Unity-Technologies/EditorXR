#if UNITY_EDITOR
namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Attribute used to tag Action classes in order to be added to VR menus
    /// </summary>
    public class ActionMenuItemAttribute : System.Attribute, ITooltip
    {
        internal const string DefaultActionSectionName = "DefaultActions";
        internal string name;
        internal string sectionName;
        internal int priority;

        /// <summary>
        /// The tooltip string
        /// </summary>
        public string tooltipText { get { return name; } }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">The name of this action</param>
        /// <param name="sectionName">The name of the section in which this action should reside</param>
        /// <param name="priority">The ordinal of this action within the section it resides</param>
        public ActionMenuItemAttribute(string name, string sectionName = null, int priority = int.MaxValue)
        {
            this.name = name;
            this.sectionName = sectionName;
            this.priority = priority;
        }
    }
}
#endif
