#if UNITY_EDITOR
using UnityEngine;
using System;

/// <summary>
/// Provides access to transform roots for custom menus
/// </summary>
public interface IUsesCustomMenuOrigins
{
	/// <summary>
	/// Get the root transform for custom menus for a given ray origin
	/// </summary>
	Func<Transform, Transform> customMenuOrigin { set; }

	/// <summary>
	/// Get the root transform for custom alternate menus for a given ray origin
	/// </summary>
	Func<Transform, Transform> customAlternateMenuOrigin { set; }
}
#endif
