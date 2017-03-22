#if UNITY_EDITOR
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Decorates types that need to connect interfaces for spawned objects
	/// </summary>
	interface IConnectInterfaces
	{
	}

	static class IConnectInterfacesMethods
	{
		internal delegate void ConnectInterfacesDelegate(object obj, Transform rayOrigin = null);

		internal static ConnectInterfacesDelegate connectInterfaces { get; set; }

		/// <summary>
		/// Method provided by the system for connecting interfaces
		/// </summary>
		/// <param name="obj">Object to connect interfaces on</param>
		/// <param name="rayOrigin">(Optional) ray origin (needed for connecting ray-based interfaces)</param>
		public static void ConnectInterfaces(this IConnectInterfaces ci, object obj, Transform rayOrigin = null)
		{
			if (connectInterfaces != null)
				connectInterfaces(obj, rayOrigin);
		}
	}

}
#endif
