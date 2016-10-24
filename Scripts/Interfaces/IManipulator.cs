using System;
using UnityEngine;
using System.Collections;
using UnityEditor;

namespace UnityEngine.VR.Tools
{
	public interface IManipulator
	{
		Action<Vector3> translate { set; }
		Action<Quaternion> rotate { set; }
		Action<Vector3> scale { set; }
		bool dragging { get; }
	}
}