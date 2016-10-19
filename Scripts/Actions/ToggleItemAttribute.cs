using System;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEngine.VR.Menus
{
	/// <summary>
	/// Attribute used to tag action items that can be added to VR menus
	/// </summary>
	public class ToggleItemAttribute : System.Attribute
	{
		//public ToggleActionMenuItemAttribute (string groupName, string name01, string iconResourcePath01, string name02, string iconResourcePath02, string categoryName = null, int indexPosition = -1)
		//	: base(name01, iconResourcePath01, categoryName = categoryName, indexPosition = indexPosition)
		//{
		//	this.groupName = groupName;
		//	this.name02 = name02;
		//	this.iconResourcePath02 = iconResourcePath02;
		//}
		
		// second sprite
		// second icon/sprite display name

		public ToggleItemAttribute (string toggleGroupName, string item01Name, string item01Icon, string item02Name, string item02Icon, string sectionName = null, int indexPosition = -1)
		{
			this.name = toggleGroupName;
			this.item01Icon = item01Icon;
			this.item01Name = item01Name;
			this.item02Icon = item02Icon;
			this.item02Name = item02Name;
			this.sectionName = sectionName;
			this.indexPosition = indexPosition;
		}

		/// <summary>
		/// The name of this toggle
		/// </summary>
		public string name { get; set; }

		/// <summary>
		/// The icon representing the first of the two toggle states that can be displayed in menus
		/// </summary>
		public string item01Icon { get; set; }

		/// <summary>
		/// The icon representing the second of the two toggle states that can be displayed in menus
		/// </summary>
		public string item02Icon { get; set; }

		/// <summary>
		/// The name of the first toggle states that can be displayed in menus
		/// </summary>
		public string item01Name { get; set; }

		/// <summary>
		/// The name of the second toggle states that can be displayed in menus
		/// </summary>
		public string item02Name { get; set; }

		/// <summary>
		/// The name of the section within which this toggle resides
		/// </summary>
		public string sectionName { get; set; }

		/// <summary>
		/// The numeric position of this toggle within its section
		/// </summary>
		public int indexPosition { get; set; }
	}
}
