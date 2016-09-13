using System;
using System.Collections.Generic;
using UnityEngine.VR.Utilities;
using UnityEngine.VR.Data;

namespace UnityEngine.VR.Modules
{
	public class IntersectionModule : MonoBehaviour
	{
		private readonly Dictionary<IntersectionTester, Renderer> m_IntersectedObjects = new Dictionary<IntersectionTester, Renderer>();
		private readonly List<IntersectionTester> m_Testers = new List<IntersectionTester>();

		private SpatialHash<Renderer> m_SpatialHash;

		public bool hasObjects
		{
			get { return m_IntersectedObjects.Count > 0; }
		}

#if UNITY_EDITOR
		public List<IntersectionTester> testers
		{
			get { return m_Testers; }
		}

		public bool ready
		{
			get
			{
				if (m_SpatialHash == null)
					return false;
				return true;
			}
		}

		public List<Renderer> allObjects
		{
			get
			{
				return m_SpatialHash == null ? null : m_SpatialHash.allObjects;
			}
		}

		public int intersectedObjectCount
		{
			get { return m_IntersectedObjects.Count; }
		}
#endif

		internal void Setup(SpatialHash<Renderer> hash)
		{
			m_SpatialHash = hash;
		}

		void Update()
		{
			if (m_SpatialHash == null)
				return;

			if (m_Testers == null)
				return;

			int i = 0;
			foreach (var tester in m_Testers)
			{
				Color color = i % 2 == 0 ? Color.red : Color.green;
				i++;

				if (!tester.active)
					continue;

				if (tester.transform.hasChanged)
				{
					bool detected = false;
					Renderer[] intersections;
					if (m_SpatialHash.GetIntersections(tester.renderer.bounds, out intersections))
					{
						//Sort list to try and hit closer object first
						Array.Sort(intersections, (a, b) => (a.bounds.center - tester.renderer.bounds.center).magnitude.CompareTo((b.bounds.center - tester.renderer.bounds.center).magnitude));
						foreach (var obj in intersections)
						{
							//Early-outs:
							// Not updated yet
							if (obj.transform.hasChanged)
								continue;

							//Bounds check
							if (!obj.bounds.Intersects(tester.renderer.bounds))
								continue;

							if (U.Intersection.TestObject(obj, color, tester))
							{
								detected = true;
								Renderer currentObject;
								if (m_IntersectedObjects.TryGetValue(tester, out currentObject))
								{
									if (currentObject == obj)
									{
										OnIntersectionStay(tester, obj);
									} else
									{
										OnIntersectionExit(tester, currentObject);
										OnIntersectionEnter(tester, obj);
									}
								} else
								{
									OnIntersectionEnter(tester, obj);
								}
							}

							if (detected)
								break;
						}
					}

					if (!detected)
					{
						Renderer intersectedObject;
						if (m_IntersectedObjects.TryGetValue(tester, out intersectedObject))
						{
							OnIntersectionExit(tester, intersectedObject);
						}
					}
				}

				tester.renderer.transform.hasChanged = false;
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
			Debug.Log("Entered " + obj);
		}

		void OnIntersectionStay(IntersectionTester tester, Renderer obj)
		{
			m_IntersectedObjects[tester] = obj;
			//Debug.Log("Stayed " + obj);
		}

		void OnIntersectionExit(IntersectionTester tester, Renderer obj)
		{
			m_IntersectedObjects.Remove(tester);
			Debug.Log("Exited " + obj);
		}

		public Renderer GetIntersectedObjectForTester(IntersectionTester tester)
		{
			Renderer obj;
			m_IntersectedObjects.TryGetValue(tester, out obj);
			return obj;
		}

		public Renderer GrabObjectAndRemove(IntersectionTester tester)
		{
			Renderer obj = GetIntersectedObjectForTester(tester);
			tester.grabbedObject = obj;
			m_SpatialHash.RemoveObject(obj);
			return obj;
		}

		public Renderer GrabObjectAndDisableTester(IntersectionTester tester)
		{
			Renderer obj = GetIntersectedObjectForTester(tester);
			tester.grabbedObject = obj;
			tester.active = false;
			return obj;
		}

		public Renderer GrabObjectAndRemoveAndDisableTester(IntersectionTester tester)
		{
			Renderer obj = GetIntersectedObjectForTester(tester);
			if (obj != null)
			{
				tester.grabbedObject = obj;
				m_SpatialHash.RemoveObject(obj);
				tester.active = false;
			}
			return obj;
		}

		public void UnGrabObject(IntersectionTester tester)
		{
			m_SpatialHash.AddObject(tester.grabbedObject, tester.grabbedObject.bounds);
			tester.grabbedObject = null;
			tester.active = true;
		}
	}
}