namespace UnityEngine.VR.Tools
{
	/// <summary>
	/// Attribute used to tag items (tools, actions, etc) that can be added to VR menus
	/// </summary>
	public class VRMenuItemAttribute : System.Attribute
	{
		public string Name;
		public float Order; // float to allow for custom ordering between standard integer positions
		public string SectionName;
		public string Description;

		public VRMenuItemAttribute(string name, string sectionName, string description = null, float order = -1)
		{
			Description = description;
			Order = order;
			Name = name;
			SectionName = sectionName;
		}
	}
}