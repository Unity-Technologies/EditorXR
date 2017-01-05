namespace UnityEngine.Experimental.EditorVR.Actions
{
	/// <summary>
	/// Attribute used to tag Action classes in order to be added to VR menus
	/// </summary>
	public class ActionMenuItemAttribute : System.Attribute
	{
		/// <summary>
		/// Default action section name that gets used for showing items in the alternate menu
		/// </summary>
		public const string kDefaultActionSectionName = "DefaultActions";

		/// <summary>
		/// The name of this action
		/// </summary>
		public string name;

		/// <summary>
		/// Name of section this action should be associated with
		/// </summary>
		public string sectionName;

		/// <summary>
		/// The order of this action amidst other actions within the same section
		/// </summary>
		public int priority;

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