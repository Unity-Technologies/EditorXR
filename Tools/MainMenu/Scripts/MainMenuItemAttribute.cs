namespace UnityEngine.VR.Tools
{
	/// <summary>
	/// Attribute used to tag items (tools, actions, etc) that can be added to VR menus
	/// </summary>
	public class MainMenuItemAttribute : System.Attribute
	{
		public string name;
		public string sectionName;
		public string description;

		public MainMenuItemAttribute(string name, string sectionName, string description)
		{
			this.name = name;
			this.sectionName = sectionName;
			this.description = description;
		}
	}
}