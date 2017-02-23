#if UNITY_EDITOR
using System;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// In cases where you must have different input logic (e.g. button press + axis input) you can get the proxy type
	/// </summary>
	public interface IUsesProxyType
	{
		/// <summary>
		/// The Proxy Type
		/// </summary>
		Type proxyType { set;  }
	}
}
#endif
