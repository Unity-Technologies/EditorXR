using System.Collections.Generic;
using System.Collections;
using UnityEngine.Experimental.EditorVR.Data;

namespace UnityEngine.Experimental.EditorVR.Modules
{
	public class SpatialHashModule : MonoBehaviour
	{
		readonly List<Renderer> m_ChangedObjects = new List<Renderer>();
		public SpatialHash<Renderer> spatialHash { get; private set; }

		void Awake()
		{
			spatialHash = new SpatialHash<Renderer>();
		}

		internal void Setup()
		{
			SetupObjects();
			StartCoroutine(UpdateDynamicObjects());
		}

		void SetupObjects()
		{
			MeshFilter[] meshFilters = FindObjectsOfType<MeshFilter>();
			foreach (var mf in meshFilters)
			{
				if (mf.sharedMesh)
				{
					// Exclude EditorVR objects
					if (mf.GetComponentInParent<UnityEditor.Experimental.EditorVR.EditorVR>())
						continue;

					Renderer renderer = mf.GetComponent<Renderer>();
					if (renderer)
						spatialHash.AddObject(renderer, renderer.bounds);
				}
			}
		}

		private IEnumerator UpdateDynamicObjects()
		{
			while (true)
			{
				m_ChangedObjects.Clear();

				// TODO AE 9/21/16: Hook updates of new objects that are created
				foreach (var obj in spatialHash.allObjects)
				{
					if (!obj)
					{
						m_ChangedObjects.Add(obj);
						continue;
					}

					if (obj.transform.hasChanged)
					{
						m_ChangedObjects.Add(obj);
						obj.transform.hasChanged = false;
					}
				}

				foreach (var changedObject in m_ChangedObjects)
				{
					spatialHash.RemoveObject(changedObject);

					if (changedObject)
						spatialHash.AddObject(changedObject, changedObject.bounds);
				}

				yield return null;
			}
		}

		public void AddObject(GameObject gameObject)
		{
			foreach (var renderer in gameObject.GetComponentsInChildren<Renderer>())
			{
				spatialHash.AddObject(renderer, renderer.bounds);
			}
		}

		public void RemoveObject(GameObject gameObject)
		{
			foreach (var renderer in gameObject.GetComponentsInChildren<Renderer>())
			{
				spatialHash.RemoveObject(renderer);
			}
		}
	}
}