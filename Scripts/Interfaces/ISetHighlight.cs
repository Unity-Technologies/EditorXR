using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Gives decorated class ability to highlight a given GameObject
	/// </summary>
	public interface ISetHighlight
	{
		/// <summary>
		/// GameObject = Object to highlight
		/// Bool = If true, highlight the GameObject; if false, disable highlight on the GameObject
		/// </summary>
		Action<GameObject, bool> setHighlight { set; }
	}
}