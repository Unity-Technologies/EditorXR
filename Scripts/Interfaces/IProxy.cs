using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine.VR.Proxies
{
	public interface IProxy
	{
		bool active
		{
			get;
		}

		TrackedObject trackedObjectInput
		{
			set;
		}

		Dictionary<Node, Transform> rayOrigins
		{
			get;
		}

		bool hidden
		{
			set;
		}
	}
}
