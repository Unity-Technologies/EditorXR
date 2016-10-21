using System;

namespace UnityEngine.VR.Tools
{
	interface IConnectInterfaces
	{
		Action<object> connectInterfaces { set; }
	}
}
