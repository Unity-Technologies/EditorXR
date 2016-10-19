namespace UnityEngine.VR.Actions
{
	/// <summary>
	/// Interface that mandates the properties & methods that must be implemented for EditorVR Actions
	/// </summary>
	public interface IAction
	{
		/// <summary>
		/// The icon representing this Action that can be displayed in menus
		/// </summary>
		Sprite icon { get; }

		/// <summary>
		/// ExecuteAction this action
		/// </summary>
		bool ExecuteAction();
	}
}