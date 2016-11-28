using System;
using UnityEngine.VR.Menus;

namespace UnityEngine.VR.Tools
{
	/// <summary>
	/// Provides custom menu instantiation
	/// </summary>
	public interface IInstantiateMenuUI
	{
		/// <summary>
		/// Instantiate custom menu UI on a proxy
		/// Transform = Ray origin
		/// GameObject = Prefab
		/// Returns an instantiated UI
		/// </summary>
		Func<Transform, IMenu, GameObject> instantiateMenuUI { set; }
	}
}
