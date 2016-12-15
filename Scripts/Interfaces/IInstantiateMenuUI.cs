using System;
using UnityEngine.Experimental.EditorVR.Menus;

namespace UnityEngine.Experimental.EditorVR.Tools
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
