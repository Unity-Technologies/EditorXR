using System;

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
		Func<Transform, GameObject, GameObject> instantiateMenuUI { set; }
	}
}
