namespace UnityEngine.VR.Actions
{
	/// <summary>
	/// Interface that mandates the properties & methods that must be implemented for EditorVR Actions
	/// </summary>
	public interface IAction
	{
		/// <summary>
		/// The name of this action
		/// </summary>
		string name { get; set; }

		/// <summary>
		/// The name of the section within which this action resides
		/// </summary>
		string sectionName { get; set; }

		/// <summary>
		/// The numeric position of this action within its section
		/// </summary>
		int indexPosition { get; set; }

		/// <summary>
		/// The icon representing this Action that can be displayed in menus
		/// </summary>
		Sprite icon { get; set; }

		/// <summary>
		/// Execute this action
		/// </summary>
		bool Execute();
	}
}