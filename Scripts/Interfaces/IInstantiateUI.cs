using System;

namespace UnityEngine.VR.Tools
{
	/// <summary>
	/// Decorates types that need to connect interfaces for spawned objects
	/// </summary>
	public interface IInstantiateUI
	{
		/// <summary>
		/// Method provided by the system for instantiating UI
		/// </summary>
		Func<GameObject, GameObject> instantiateUI { set; }
	}
}