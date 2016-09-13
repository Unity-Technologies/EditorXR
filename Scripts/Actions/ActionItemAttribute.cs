namespace UnityEngine.VR.Actions
{
	/// <summary>
	/// Attribute used to tag action items that can be added to VR menus
	/// </summary>
	public class ActionItemAttribute : System.Attribute
	{
		/// <summary>
		/// The icon path utilized if no custom iconResourcePath is defined
		/// </summary>
		public static string missingIconResourcePath = "ActionIcons/MissingIcon";

		/// <summary>
		/// The name of this action
		/// </summary>
		public string name;

		/// <summary>
		/// This action's icon resource file path
		/// </summary>
		public string iconResourcePath;

		/// <summary>
		/// Name of section this action should be associated with
		/// </summary>
		public string categoryName;

		/// <summary>
		/// Position/index/order of this action amidst other actions with the same SectionName
		/// </summary>
		public int indexPosition;

		/// <summary>
		/// Construct this action's attribute
		/// </summary>
		/// <param name="name">The name of this action</param>
		/// <param name="iconResourcePath">The icon resource path for this action</param>
		/// <param name="categoryName">The name of the section in which this aciton should reside</param>
		/// <param name="position">The numeric position of this action within the section it resides</param>
		public ActionItemAttribute(string name, string iconResourcePath, string categoryName = null, int indexPosition = -1)
		{
			this.name = name;
			this.iconResourcePath = !string.IsNullOrEmpty(categoryName) ? iconResourcePath : missingIconResourcePath; // if no sectionName is passed in, assign the missing icon resource path
			this.categoryName = categoryName;
			this.indexPosition = indexPosition;
		}
	}
}