#if UNITY_EDITOR
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Declares a class as a system-level menu
	/// </summary>
	public interface IMenu
	{
		/// <summary>
		/// Set whether the menu is visible or not
		/// </summary>
		void SetVisible(bool visible, bool temporary = false);

		/// <summary>
		/// Get whether the menu is visible or not
		/// </summary>
		bool GetVisible();

		/// <summary>
		/// GameObject that this component is attached to
		/// </summary>
		GameObject gameObject { get; }

		/// <summary>
		/// Root GameObject for visible menu content
		/// </summary>
		GameObject menuContent { get; }

		/// <summary>
		/// The local bounds of this menu
		/// </summary>
		Bounds localBounds { get; }
	}
}
#endif
