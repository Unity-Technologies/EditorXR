#if UNITY_EDITOR
using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Provides access to checks that can test whether a ray is hovering over a UI element
	/// </summary>
	public interface IUsesUIBlocking
	{
		/// <summary>
		/// Returns whether the specified ray origin is hovering over a UI element
		/// </summary>
		Func<Transform, bool> hoveringUI { set; }
	}
}
#endif