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
		/// Controls whether the menu is visible or not
		/// </summary>
		bool visible { get; set; }

		/// <summary>
		/// GameObject that this component is attached to
		/// </summary>
		GameObject gameObject { get; }

		/// <summary>
		/// Root GameObject for visible menu content
		/// </summary>
		GameObject menuContent { get; }

		/// <summary>
		/// If the rayOrigin this menu is attached to is hovering UI, hide it if the raycast distance is less than this
		/// </summary>
		float hideDistance { get; }
	}
}
#endif
