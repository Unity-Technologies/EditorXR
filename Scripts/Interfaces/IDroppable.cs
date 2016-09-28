using System;

namespace UnityEngine.VR.Modules
{
	public interface IDroppable
	{
		Func<Transform, IDropReciever> getCurrentDropReciever { set; }
	}
}