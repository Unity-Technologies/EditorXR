using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR;

namespace UnityEditor.VR.Modules
{
	public class DirectSelection
	{
		public Node node;
		public GameObject gameObject;
	}

	public interface IDirectSelection
	{
		Func<Dictionary<Transform, DirectSelection>> getDirectSelection { set; }
	}
}