using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine.VR.Proxies
{
	public interface IProxy
	{
        bool Active
        {
            get;
        }

        TrackedObject TrackedObjectInput
		{
			set;
		}

	    Dictionary<Node, Transform> RayOrigins
	    {
	        get;
	    }

	    bool Hidden
	    {
	        set;
	    }
	}
}
