using UnityEngine;
using UnityEngine.UI;

namespace UnityEngine.VR.Actions
{
	/// <summary>
	/// Attribute used to tag action items that can be added to VR menus
	/// </summary>
	public class ToggleActionItemAttribute : ActionItemAttribute
	{
		string groupName { get; set; }

		/// <summary>
		/// The name of this action
		/// </summary>
		public string name02;

		/// <summary>
		/// This action's icon resource file path
		/// </summary>
		public string iconResourcePath02;

		public ToggleActionItemAttribute (string groupName, string name01, string iconResourcePath01, string name02, string iconResourcePath02, string categoryName = null, int indexPosition = -1)
			: base(name01, iconResourcePath01, categoryName = categoryName, indexPosition = indexPosition)
		{
			this.groupName = groupName;
			this.name02 = name02;
			this.iconResourcePath02 = iconResourcePath02;
		}
		
		// second sprite
		// second icon/sprite display name
	}
}
 