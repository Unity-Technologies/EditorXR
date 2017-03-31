#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Data;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
	sealed class IntersectionModule : MonoBehaviour, IUsesGameObjectLocking
	{
		const int k_MaxTestsPerTester = 100;

		private readonly Dictionary<IntersectionTester, Renderer> m_IntersectedObjects = new Dictionary<IntersectionTester, Renderer>();
		private readonly List<IntersectionTester> m_Testers = new List<IntersectionTester>();

		private SpatialHash<Renderer> m_SpatialHash;
		private MeshCollider m_CollisionTester;

#if UNITY_EDITOR
		public bool ready { get { return m_SpatialHash != null; } }
		public List<IntersectionTester> testers { get { return m_Testers; } }
		public List<Renderer> allObjects { get { return m_SpatialHash == null ? null : m_SpatialHash.allObjects; } }
		public int intersectedObjectCount { get { return m_IntersectedObjects.Count; } }
#endif

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

			foreach (var tester in m_Testers)
			{
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
					Renderer[] intersections;
					if (m_SpatialHash.GetIntersections(tester.renderer.bounds, out intersections))
					{
						//Sort list to try and hit closer object first
						var testerBounds = tester.renderer.bounds;
						var testerBoundsCenter = testerBounds.center;
						Array.Sort(intersections, (a, b) => (a.bounds.center - testerBoundsCenter).magnitude.CompareTo((b.bounds.center - testerBoundsCenter).magnitude));
						var intersectionList = intersections.ToList();
						intersectionList.RemoveAll(obj =>
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

						if (intersectionList.Count > k_MaxTestsPerTester)
							continue;

						foreach (var obj in intersectionList)
						{
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
	}
}
#endif
