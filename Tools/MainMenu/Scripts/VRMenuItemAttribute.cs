namespace UnityEngine.VR.Tools
{
	/// <summary>
	/// Attribute used to tag items (tools, actions, etc) that can be added to VR menus
	/// </summary>
	public class VRMenuItemAttribute : System.Attribute
	{
		public string Name;
		public string SectionName;
		public string Description;

		public VRMenuItemAttribute(string name, string sectionName, string description)
		{
			Name = name;
			SectionName = sectionName;
			Description = description;
		}
	}
}