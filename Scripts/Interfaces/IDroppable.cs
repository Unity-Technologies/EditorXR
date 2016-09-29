using System;

namespace UnityEngine.VR.Modules
{
	public delegate IDropReciever GetDropRecieverDelegate(Transform rayOrigin, out GameObject target);
	public interface IDroppable
	{
		GetDropRecieverDelegate getCurrentDropReciever { set; }
		Action<Transform, object> setCurrentDropObject { set; }
	}
}