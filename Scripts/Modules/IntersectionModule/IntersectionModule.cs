#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Data;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
	sealed class IntersectionModule : MonoBehaviour, IUsesGameObjectLocking
	{
		const int k_MaxTestsPerTester = 250;

		class RayIntersection
		{
			public GameObject go;
			public float distance;
		}

		class DirectIntersection
		{
			public Renderer renderer;
			public Vector3 contactPoint;
		}

		readonly Dictionary<IntersectionTester, DirectIntersection> m_IntersectedObjects = new Dictionary<IntersectionTester, DirectIntersection>();
		readonly List<IntersectionTester> m_Testers = new List<IntersectionTester>();
		readonly Dictionary<Transform, RayIntersection> m_RaycastGameObjects = new Dictionary<Transform, RayIntersection>(); // Stores which gameobject the proxies' ray origins are pointing at

		SpatialHash<Renderer> m_SpatialHash; 
		MeshCollider m_CollisionTester;

		public bool ready { get { return m_SpatialHash != null; } }
		public List<IntersectionTester> testers { get { return m_Testers; } }
		public List<Renderer> allObjects { get { return m_SpatialHash == null ? null : m_SpatialHash.allObjects; } }
		public int intersectedObjectCount { get { return m_IntersectedObjects.Count; } }

		// Local method use only -- created here to reduce garbage collection
		readonly List<Renderer> m_Intersections = new List<Renderer>();
		readonly List<SortableRenderer> m_SortedIntersections = new List<SortableRenderer>();
		
		struct SortableRenderer
		{
			public Renderer renderer;
			public float distance;
		}

		void Awake()
		{
			IntersectionUtils.BakedMesh = new Mesh(); // Create a new Mesh in each Awake because it is destroyed on scene load
		}

		internal void Setup(SpatialHash<Renderer> hash)
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

			for (int i = 0; i < m_Testers.Count; i++)
			{
				var tester = m_Testers[i];

				if (!tester.active)
				{
					//Intersection Exit
					m_IntersectedObjects[tester].renderer = null;
					continue;
				}

				var testerTransform = tester.transform;
				if (testerTransform.hasChanged)
				{
					var intersectionFound = false;
					m_Intersections.Clear();
					if (m_SpatialHash.GetIntersections(m_Intersections, tester.renderer.bounds))
					{
						var testerBounds = tester.renderer.bounds;
						var testerBoundsCenter = testerBounds.center;

						m_SortedIntersections.Clear();
						for (int j = 0; j < m_Intersections.Count; j++)
						{
							var obj = m_Intersections[j];
							// Ignore destroyed objects
							if (!obj)
								continue;

							// Ignore inactive objects
							if (!obj.gameObject.activeInHierarchy)
								continue;

							// Ignore locked objects
							if (this.IsLocked(obj.gameObject))
								continue;

							// Bounds check
							if (!obj.bounds.Intersects(testerBounds))
								continue;

							m_SortedIntersections.Add(new SortableRenderer
							{
								renderer = obj,
								distance = (obj.bounds.center - testerBoundsCenter).magnitude
							});
						}

						//Sort list to try and hit closer object first
						m_SortedIntersections.Sort((a, b) => a.distance.CompareTo(b.distance));

						if (m_SortedIntersections.Count > k_MaxTestsPerTester)
							continue;

						for (int j = 0; j < m_SortedIntersections.Count; j++)
						{
							Vector3 contactPoint;
							var renderer = m_SortedIntersections[j].renderer;
							if (IntersectionUtils.TestObject(m_CollisionTester, renderer, tester, out contactPoint))
							{
								intersectionFound = true;
								var intersection = m_IntersectedObjects[tester];
								if (intersection.renderer == renderer)
								{
									// Intersection Stay
									intersection.contactPoint = contactPoint;
								}
								else
								{
									// Intersection Exit / Enter
									intersection.renderer = renderer;
									intersection.contactPoint = contactPoint;
								}
							}

							if (intersectionFound)
								break;
						}
					}

					// Intersection Exit
					if (!intersectionFound)
						m_IntersectedObjects[tester].renderer = null;

					testerTransform.hasChanged = false;
				}
			}
		}

		internal void AddTester(IntersectionTester tester)
		{
			m_Testers.Add(tester);
			m_IntersectedObjects[tester] = new DirectIntersection();
		}

		internal Renderer GetIntersectedObjectForTester(IntersectionTester tester, out Vector3 contactPoint)
		{
			var intersection = m_IntersectedObjects[tester];
			contactPoint = intersection.contactPoint;
			return intersection.renderer;
		}

		internal GameObject GetFirstGameObject(Transform rayOrigin, out float distance)
		{
			RayIntersection intersection;
			if (m_RaycastGameObjects.TryGetValue(rayOrigin, out intersection))
			{
				distance = intersection.distance;
				return intersection.go;
			}

			distance = 0;
			return null;
		}

		internal void UpdateRaycast(Transform rayOrigin, float distance)
		{
			GameObject go;
			RaycastHit hit;
			Raycast(new Ray(rayOrigin.position, rayOrigin.forward), out hit, out go, distance);
			m_RaycastGameObjects[rayOrigin] = new RayIntersection { go = go, distance = hit.distance };
		}

		internal bool Raycast(Ray ray, out RaycastHit hit, out GameObject obj, float maxDistance = Mathf.Infinity, List<GameObject> ignoreList = null)
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

					IntersectionUtils.SetupCollisionTester(m_CollisionTester, transform);

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
