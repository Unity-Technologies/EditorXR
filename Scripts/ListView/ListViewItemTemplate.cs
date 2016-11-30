using UnityEngine;
using System.Collections.Generic;

namespace ListView
{
	public class ListViewItemTemplate
	{
		public readonly GameObject prefab;
		public readonly List<MonoBehaviour> pool = new List<MonoBehaviour>();

		public ListViewItemTemplate(GameObject prefab)
		{
			if (prefab == null)
				Debug.LogError("Template prefab cannot be null");
			this.prefab = prefab;
		}
	}
}