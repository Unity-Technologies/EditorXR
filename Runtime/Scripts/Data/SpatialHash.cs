using System.Collections.Generic;
using UnityEngine;

namespace Unity.Labs.EditorXR.Data
{
    class SpatialHash<T>
    {
        readonly List<T> m_AllObjects = new List<T>();
        readonly BoundsOctree<T> m_Octree = new BoundsOctree<T>(100f, Vector3.zero, 0.5f, 1.2f);

        public List<T> allObjects
        {
            get { return m_AllObjects; }
        }

        public bool GetIntersections(List<T> intersections, Bounds bounds)
        {
            m_Octree.GetColliding(intersections, bounds);
            return intersections.Count > 0;
        }

        public bool GetIntersections(List<T> intersections, Ray ray, float maxDistance = Mathf.Infinity)
        {
            m_Octree.GetColliding(intersections, ray, maxDistance);
            return intersections.Count > 0;
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

        public Bounds GetMaxBounds()
        {
            return m_Octree.GetMaxBounds();
        }
    }
}
