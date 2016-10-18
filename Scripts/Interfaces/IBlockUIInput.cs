using System;

namespace UnityEngine.VR.Modules
{
	/// <summary>
	/// Enables this object to set a global input blocking state
	/// </summary>
	public interface IBlockUIInput
	{
		/// <summary>
		/// ConnectInterfaces provides a delegate which can set the input blocking state
		/// This will stop the inputmodule from processing and prevent active state change on direct select inputs
		/// </summary>
		Action<bool> setInputBlocked { set; }
	}
}