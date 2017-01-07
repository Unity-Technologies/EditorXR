namespace UnityEngine.Experimental.EditorVR.Menus
{
	/// <summary>
	/// Attribute used to tag items (tools, actions, etc) that can be added to VR menus
	/// </summary>
	public class MainMenuItemAttribute : System.Attribute
	{
		public string name;
		public string sectionName;
		public string description;
		public bool shown;

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
		public MainMenuItemAttribute(string name, string sectionName, string description)
		{
			this.name = name;
			this.sectionName = sectionName;
			this.description = description;
			this.shown = true;
		}
	}
}