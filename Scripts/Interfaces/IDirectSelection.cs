using System;
using System.Collections.Generic;

namespace UnityEngine.VR.Modules
{
	public interface IDirectSelection
	{
		Func<Dictionary<Transform, DirectSelection>> getDirectSelection { set; }
	}
}