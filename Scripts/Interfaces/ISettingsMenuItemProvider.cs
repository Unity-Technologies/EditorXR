#if UNITY_EDITOR
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Decorates types that can provide a sub-menu on the Settings menu. The class should also use a MainMenuItemAttribute
	/// </summary>
	public interface ISettingsMenuItemProvider
	{
		/// <summary>
		/// The menu face prefab which will be added to the menu
		/// </summary>
		GameObject settingsMenuItemPrefab { get; }

		/// <summary>
		/// An instance of the menu face prefab that was added to the menu
		/// </summary>
		GameObject settingsMenuItemInstance { set; }
	}
}
#endif