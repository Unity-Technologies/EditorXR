#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Data;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
	delegate bool RaycastDelegate(Ray ray, out RaycastHit hit, out GameObject go, float maxDistance = Mathf.Infinity, List<GameObject> ignoreList = null);

	sealed class IntersectionModule : MonoBehaviour, IUsesGameObjectLocking
	{
		const int k_MaxTestsPerTester = 100;

		readonly Dictionary<IntersectionTester, Renderer> m_IntersectedObjects = new Dictionary<IntersectionTester, Renderer>();
		readonly List<IntersectionTester> m_Testers = new List<IntersectionTester>();

		SpatialHash<Renderer> m_SpatialHash;
		MeshCollider m_CollisionTester;

#if UNITY_EDITOR
		public bool ready { get { return m_SpatialHash != null; } }
		public List<IntersectionTester> testers { get { return m_Testers; } }
		public List<Renderer> allObjects { get { return m_SpatialHash == null ? null : m_SpatialHash.allObjects; } }
		public int intersectedObjectCount { get { return m_IntersectedObjects.Count; } }
#endif

		readonly List<Renderer> m_Intersections = new List<Renderer>();

		public void Setup(SpatialHash<Renderer> hash)
		{
			m_SpatialHash = hash;
			m_CollisionTester = ObjectUtils.CreateGameObjectWithComponent<MeshCollider>(transform);
		}

		void Update()
		{
			if (m_SpatialHash == null)
				return;

			if (m_Testers == null)
				return;

			for(int i = 0; i < m_Testers.Count; i++)
			{
				var tester = m_Testers[i];
				if (!tester.active)
				{
					Renderer intersectedObject;
					if (m_IntersectedObjects.TryGetValue(tester, out intersectedObject))
						OnIntersectionExit(tester, intersectedObject);

					continue;
				}

				var testerTransform = tester.transform;
				if (testerTransform.hasChanged)
				{
					var intersectionFound = false;
					m_Intersections.Clear();
					if (m_SpatialHash.GetIntersections(m_Intersections, tester.renderer.bounds))
					{
						//Sort list to try and hit closer object first
						var testerBounds = tester.renderer.bounds;
						var testerBoundsCenter = testerBounds.center;
						m_Intersections.Sort((a, b) => (a.bounds.center - testerBoundsCenter).magnitude.CompareTo((b.bounds.center - testerBoundsCenter).magnitude));
						m_Intersections.RemoveAll(obj =>
						{
							// Ignore destroyed objects
							if (!obj)
								return true;

							// Ignore inactive objects
							if (!obj.gameObject.activeInHierarchy)
								return true;

							// Ignore locked objects
							if (this.IsLocked(obj.gameObject))
								return true;

							// Bounds check
							if (!obj.bounds.Intersects(testerBounds))
								return true;

							return false;
						});

						if (m_Intersections.Count > k_MaxTestsPerTester)
							continue;

						for (int j = 0; j < m_Intersections.Count; j++)
						{
							var obj = m_Intersections[j];
							if (IntersectionUtils.TestObject(m_CollisionTester, obj, tester))
							{
								intersectionFound = true;
								Renderer currentObject;
								if (m_IntersectedObjects.TryGetValue(tester, out currentObject))
								{
									if (currentObject == obj)
									{
										OnIntersectionStay(tester, obj);
									}
									else
									{
										OnIntersectionExit(tester, currentObject);
										OnIntersectionEnter(tester, obj);
									}
								}
								else
								{
									OnIntersectionEnter(tester, obj);
								}
							}

							if (intersectionFound)
								break;
						}
					}

					if (!intersectionFound)
					{
						Renderer intersectedObject;
						if (m_IntersectedObjects.TryGetValue(tester, out intersectedObject))
							OnIntersectionExit(tester, intersectedObject);
					}

					testerTransform.hasChanged = false;
				}
			}
		}

		public void AddTester(IntersectionTester tester)
		{
			m_IntersectedObjects.Clear();
			m_Testers.Add(tester);
		}

		void OnIntersectionEnter(IntersectionTester tester, Renderer obj)
		{
			m_IntersectedObjects[tester] = obj;
		}

		void OnIntersectionStay(IntersectionTester tester, Renderer obj)
		{
			m_IntersectedObjects[tester] = obj;
		}

		void OnIntersectionExit(IntersectionTester tester, Renderer obj)
		{
			m_IntersectedObjects.Remove(tester);
		}

		public Renderer GetIntersectedObjectForTester(IntersectionTester tester)
		{
			Renderer obj;
			m_IntersectedObjects.TryGetValue(tester, out obj);
			return obj;
		}

		public bool Raycast(Ray ray, out RaycastHit hit, out GameObject obj, float maxDistance = Mathf.Infinity, List<GameObject> ignoreList = null)
		{
			obj = null;
			hit = new RaycastHit();
			var result = false;
			var distance = Mathf.Infinity;
			m_Intersections.Clear();
			if (m_SpatialHash.GetIntersections(m_Intersections, ray, maxDistance))
			{
				for (int i = 0; i < m_Intersections.Count; i++)
				{
					var renderer = m_Intersections[i];
					var gameObject = renderer.gameObject;
					if (ignoreList != null && ignoreList.Contains(gameObject))
						continue;

					var transform = renderer.transform;

					RaycastHit tmp;
					if (IntersectionUtils.TestRay(m_CollisionTester, transform, ray, out tmp, maxDistance))
					{
						var point = transform.TransformPoint(tmp.point);
						var dist = Vector3.Distance(point, ray.origin);
						if (dist < distance)
						{
							result = true;
							distance = dist;
							hit.distance = dist;
							hit.point = point;
							hit.normal = transform.TransformDirection(tmp.normal);
							obj = gameObject;
						}
					}
				}
			}

			return result;
		}
	}
}
#endif
