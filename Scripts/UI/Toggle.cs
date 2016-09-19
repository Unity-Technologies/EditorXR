using System;

namespace UnityEngine.VR.Menus
{
	/// <summary>
	/// Interface that mandates the properties & methods that must be implemented for EditorVR Toggles
	/// </summary>
	public class Toggle
	{
		public Toggle (IHasToggles owner, string name, string item01Name, string item01Icon, string item02Name, string item02Icon, Action executeAction, string sectionName = null, int indexPosition = -1)
		{
			this.owner = owner;
			this.name = name;
			this.item01Name = item01Name;
			this.item02Name = item02Name;
			this.Execute = executeAction;
			this.sectionName = sectionName;
			this.indexPosition = indexPosition;

			if (!string.IsNullOrEmpty(item01Icon))
				this.item01Icon = Resources.Load<Sprite>(item01Icon);

			if (!string.IsNullOrEmpty(item02Icon))
				this.item02Icon = Resources.Load<Sprite>(item02Icon);
		}

		/// <summary>
		/// Reference to the this toggle's owner. The object for which this Toggle was created.
		/// When Despawning tools, this reference is checked for toggle removal from the m_AllToggles collection.
		/// </summary>
		public IHasToggles owner { get; set; }

		/// <summary>
		/// The name of this toggle
		/// </summary>
		public string name { get; set; }

		/// <summary>
		/// The icon representing the first of the two toggle states that can be displayed in menus
		/// </summary>
		public Sprite item01Icon { get; set; }

		/// <summary>
		/// The icon representing the second of the two toggle states that can be displayed in menus
		/// </summary>
		public Sprite item02Icon { get; set; }

		/// <summary>
		/// The name of the first toggle states that can be displayed in menus
		/// </summary>
		public string item01Name { get; set; }

		/// <summary>
		/// The name of the second toggle states that can be displayed in menus
		/// </summary>
		public string item02Name { get; set; }

		/// <summary>
		/// Alternate the value of this toggle
		/// </summary>
		public Action Execute { get; set; }

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
