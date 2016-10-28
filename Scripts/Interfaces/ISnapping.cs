using System;
using UnityEngine;

public interface ISnapping
{

	Action<Transform, Vector3, Transform[]> onSnapStarted { set; }
	Action<Transform, Vector3, Transform[]> onSnapEnded { set; }
	Action<Transform, Vector3, Transform[]> onSnapHeld { set; }
	Action<Transform> onSnapUpdate { set; }

}
