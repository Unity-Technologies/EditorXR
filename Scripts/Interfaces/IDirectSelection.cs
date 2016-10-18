using System;
using System.Collections.Generic;
using UnityEngine.InputNew;

namespace UnityEngine.VR.Modules
{
	public interface IDirectSelection
	{
		Func<Dictionary<Transform, DirectSelection>> getDirectSelection { set; }

		//Dictionary<Node, DirectSelectInput> directSelectInputs { set; }
	}
}