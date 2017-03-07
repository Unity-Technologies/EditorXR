#if UNITY_EDITOR
using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Provides custom menu instantiation
	/// </summary>
	public interface IInstantiateMenuUI
	{
		/// <summary>
		/// Instantiate custom menu UI on a proxy
		/// Transform = Ray origin
		/// IMenu = Prefab (with IMenu component) to instantiate
		/// Returns an instantiated UI GameObject
		/// </summary>
		Func<Transform, IMenu, GameObject> instantiateMenuUI { set; }
	}
}
#endif
