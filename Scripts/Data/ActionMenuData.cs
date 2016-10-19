using UnityEngine.UI;

namespace UnityEngine.VR.Actions
{
	public class ActionMenuData
	{
		/// <summary>
		/// The name of this action
		/// </summary>
		public string name { get; set; }

		/// <summary>
		/// The name of the section within which this action resides
		/// </summary>
		public string sectionName { get; set; }

		/// <summary>
		/// The numeric position of this action within its section
		/// </summary>
		public int indexPosition { get; set; }

		/// <summary>
		/// The icon representing this Action that can be displayed in menus
		/// </summary>
		public Sprite icon { get; set; }

		/// <summary>
		/// An instance of the Action that can be used for execution
		/// </summary>
		public IAction action { get; set; }
	}
}
