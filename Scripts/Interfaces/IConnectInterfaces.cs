namespace UnityEngine.Experimental.EditorVR.Tools
{
	/// <summary>
	/// Method signature for connecting interfaces
	/// <param name="obj">Object to connect interfaces on</param>
	/// <param name="rayOrigin">(Optional) ray origin (needed for connecting ray-based interfaces)</param>
	/// </summary>
	public delegate void ConnectInterfacesDelegate(object obj, Transform rayOrigin = null);

	/// <summary>
	/// Decorates types that need to connect interfaces for spawned objects
	/// </summary>
	interface IConnectInterfaces
	{
		/// <summary>
		/// Method provided by the system for connecting interfaces
		/// </summary>
		ConnectInterfacesDelegate connectInterfaces { set; }
	}
}
