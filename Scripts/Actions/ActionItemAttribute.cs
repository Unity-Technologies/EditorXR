namespace UnityEngine.VR.Actions
{
	/// <summary>
	/// Attribute used to tag action items that can be added to VR menus
	/// </summary>
	public class ActionItemAttribute : System.Attribute
	{
		public static string missingIconResourcePath = "ActionIcons/MissingIcon";

		public string name;
		public string iconResourcePath;
		public string sectionName;

		public ActionItemAttribute(string name, string iconResourcePath, string sectionName = null)
		{
			this.name = name;
			this.iconResourcePath = iconResourcePath;
			this.sectionName = sectionName;
		}
	}
}