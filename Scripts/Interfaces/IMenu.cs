namespace UnityEngine.VR.Menus
{
	/// <summary>
	/// Declares a class as a system-level menu
	/// </summary>
	public interface IMenu
	{
		/// <summary>
		/// Controls whether the menu is visible or not
		/// </summary>
		bool visible { get; set; }

		/// <summary>
		/// GameObject that this component is attached to
		/// </summary>
		GameObject gameObject { get; }
	}
}
