using System;

namespace UnityEngine.VR.Workspaces
{
	public interface IWorkspace : IVacuumable
	{
		/// <summary>
		/// First-time setup; will be called after Awake and ConnectInterfaces
		/// </summary>
		void Setup();

		/// <summary>
		/// Call this in OnDestroy to inform the system
		/// </summary>
		event Action<IWorkspace> destroyed;
	}
}