using System;
using UnityEngine;
using System.Collections;

namespace UnityEngine.VR.Tools
{
	public interface IManipulator
	{
		Action<Vector3> translate { set; }
		Action<Quaternion> rotate { set; }
	}
}