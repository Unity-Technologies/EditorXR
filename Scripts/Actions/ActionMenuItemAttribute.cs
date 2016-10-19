namespace UnityEngine.VR.Actions
{
	/// <summary>
	/// Attribute used to tag action items that can be added to VR menus
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
		public string categoryName;

		/// <summary>
		/// Position/index/order of this action amidst other actions with the same category name
		/// </summary>
		public int priority;

		/// <summary>
		/// Construct this action's attribute
		/// </summary>
		/// <param name="name">The name of this action</param>
		/// <param name="categoryName">The name of the category in which this action should reside</param>
		/// <param name="priority">The numeric position of this action within the section it resides</param>
		public ActionMenuItemAttribute(string name, string categoryName = null, int priority = -1)
		{
			this.name = name;
			this.categoryName = categoryName;
			this.priority = priority;
		}
	}
}