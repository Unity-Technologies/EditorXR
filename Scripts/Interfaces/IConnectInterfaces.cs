using System;

namespace UnityEngine.VR.Tools
{
	/// <summary>
	/// Decorates types that need to connect interfaces for spawned objects
	/// </summary>
	interface IConnectInterfaces
	{
		/// <summary>
		/// Method provided by the system for connecting interfaces
		/// </summary>
		Action<object> connectInterfaces { set; }
	}
}
