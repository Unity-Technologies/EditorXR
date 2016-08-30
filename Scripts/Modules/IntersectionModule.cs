using System;
using System.Collections.Generic;
using UnityEngine.VR.Utilities;
using UnityEngine.VR.Data;

namespace UnityEngine.VR.Modules
{
	public class IntersectionModule : MonoBehaviour
	{
		[SerializeField]
		private Color m_TesterColor = Color.yellow;

		[SerializeField]
		private int m_ConeSegments = 4;

		[SerializeField]
		private float m_ConeRadius = 0.03f;

		[SerializeField]
		private float m_ConeHeight = 0.05f;

		private Mesh m_ConeMesh;
		private Ray[] m_ConeRays;

		private readonly Dictionary<IntersectionTester, Renderer> m_IntersectedObjects = new Dictionary<IntersectionTester, Renderer>();
		private IntersectionTester[] m_Testers = new IntersectionTester[0];

		private SpatialHash<Renderer> m_SpatialHash;

		public bool hasObjects
		{
			get { return m_IntersectedObjects.Count > 0; }
		}

#if UNITY_EDITOR
		public IntersectionTester[] testers
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

		void Awake()
		{
			m_ConeMesh = IntersectionTester.GenerateConeMesh(m_ConeSegments, m_ConeRadius, m_ConeHeight, out m_ConeRays);
		}

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
				if (tester.renderer.transform.hasChanged)
				{
					bool detected = false;
					Renderer[] intersections;
					if (m_SpatialHash.GetIntersections(tester.renderer.bounds, out intersections))
					{
						//Sort list to try and hit closer object first
						Array.Sort(intersections, (a, b) => (a.bounds.center - tester.renderer.bounds.center).magnitude.CompareTo((b.bounds.center - tester.renderer.bounds.center).magnitude));
						//intersections.Sort((a, b) => (a.bounds.center - tester.renderer.bounds.center).magnitude.CompareTo((b.bounds.center - tester.renderer.bounds.center).magnitude));
						foreach (var obj in intersections)
						{
							//Early-outs:
							// No mesh data
							// Not updated yet
							if (obj.transform.hasChanged)
								continue;
							//Bounds check                                                                     
							if (!obj.bounds.Intersects(tester.renderer.bounds))
								continue;
							if (U.Intersection.TestObject(obj, color, tester))
							{
								detected = true;
								Renderer oldObject;
								if (m_IntersectedObjects.TryGetValue(tester, out oldObject))
								{
									if (oldObject == obj)
									{
										OnIntersectionStay(tester, obj);
									} else
									{
										OnIntersectionExit(tester, oldObject);
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

		//TODO: don't use procedural meshes for the m_Testers. As it stands, they are created/destroyed every time you run --MTS
		public void AddTester(Transform trans)
		{
			m_IntersectedObjects.Clear();
			GameObject g = new GameObject("pointer");
			MeshRenderer renderer = g.AddComponent<MeshRenderer>();
			renderer.sharedMaterial = new Material(Shader.Find("Standard"));
			renderer.sharedMaterial.color = m_TesterColor;
			MeshFilter filter = g.AddComponent<MeshFilter>();
			g.transform.SetParent(trans, false);
			filter.sharedMesh = m_ConeMesh;
			m_Testers = new List<IntersectionTester>(m_Testers) {new IntersectionTester(renderer, m_ConeRays)}.ToArray();
		}

		//TODO: add Enter,Stay,Exit events for other systems to subscribe to
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

		public IntersectionTester GetLeftTester()
		{
			if (m_Testers.Length > 0)
				return m_Testers[0];
			return null;
		}

		public IntersectionTester GetRightTester()
		{
			if (m_Testers.Length > 1)
				return m_Testers[1];
			return null;
		}

		public Renderer GetIntersectedObjectForTester(IntersectionTester IntersectionTester)
		{
			Renderer obj;
			m_IntersectedObjects.TryGetValue(IntersectionTester, out obj);
			return obj;
		}

		public Renderer GrabObjectAndRemove(IntersectionTester tester)
		{
			Renderer obj = GetIntersectedObjectForTester(tester);
			tester.grabbed = obj;
			m_SpatialHash.RemoveObject(obj);
			return obj;
		}

		public Renderer GrabObjectAndDisableTester(IntersectionTester IntersectionTester)
		{
			Renderer obj = GetIntersectedObjectForTester(IntersectionTester);
			IntersectionTester.grabbed = obj;
			IntersectionTester.active = false;
			return obj;
		}

		public Renderer GrabObjectAndRemoveAndDisableTester(IntersectionTester IntersectionTester)
		{
			Renderer obj = GetIntersectedObjectForTester(IntersectionTester);
			if (obj != null)
			{
				IntersectionTester.grabbed = obj;
				m_SpatialHash.RemoveObject(obj);
				IntersectionTester.active = false;
			}
			return obj;
		}

		public void UnGrabObject(IntersectionTester tester)
		{
			m_SpatialHash.AddObject(tester.grabbed, tester.grabbed.bounds);
			tester.grabbed = null;
			tester.active = true;
		}
	}
}