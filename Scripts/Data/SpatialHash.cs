using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.Experimental.EditorVR.Data
{
	public class SpatialHash<T>
	{
		private readonly List<T> m_AllObjects = new List<T>();
		private readonly BoundsOctree<T> m_Octree = new BoundsOctree<T>(100f, Vector3.zero, 0.5f, 1.2f);

		public List<T> allObjects
		{
			get { return m_AllObjects; }
		}

		public bool GetIntersections(Bounds bounds, out T[] intersections)
		{
			intersections = m_Octree.GetColliding(bounds);
			return intersections.Length > 0;
		}

		public void AddObject(T obj, Bounds bounds)
		{
			m_AllObjects.Add(obj);
			m_Octree.Add(obj, bounds);
		}

		public void RemoveObject(T obj)
		{
			m_AllObjects.Remove(obj);
			m_Octree.Remove(obj);
		}
	}
}