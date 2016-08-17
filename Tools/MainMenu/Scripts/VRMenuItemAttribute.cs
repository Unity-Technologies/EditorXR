namespace UnityEngine.VR.Tools
{
	/// <summary>
	/// Attribute used to tag items (tools, actions, etc) that can be added to VR menus
	/// </summary>
	public class VRMenuItemAttribute : System.Attribute
	{
		public string SectionName;
		public string Description;

		public VRMenuItemAttribute(string sectionName, string description)
		{
			SectionName = sectionName;
			Description = description;
		}
	}
}