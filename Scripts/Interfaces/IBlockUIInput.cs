using System;

namespace UnityEngine.VR.Modules
{
	public interface IBlockUIInput
	{
		Action<bool> setInputBlocked { set; }
	}
}