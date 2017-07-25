using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	public interface IRaycast
	{
	}

	public static class IRaycastMethods
	{
		public delegate bool RaycastDelegate(Ray ray, out RaycastHit hit, out GameObject go, float maxDistance = Mathf.Infinity, List<GameObject> ignoreList = null);

		public static RaycastDelegate raycast { get; set; }

		public static bool Raycast(this IRaycast obj, Ray ray, out RaycastHit hit, out GameObject go, float maxDistance = Mathf.Infinity, List<GameObject> ignoreList = null)
		{
			return raycast(ray, out hit, out go, maxDistance, ignoreList);
		}
	}
}
