#if UNITY_EDITOR
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Declares a class as a menu UI element that can be represented via an icon/sprite
	/// </summary>
	public interface IMenuIcon
	{
		/// <summary>
		/// The icon representing this Action that can be displayed in menus
		/// </summary>
		Sprite icon { get; }
	}
}
#endif
