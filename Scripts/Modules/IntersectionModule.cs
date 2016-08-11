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

		private readonly Dictionary<IntersectionTester, SpatialObject> m_IntersectedObjects = new Dictionary<IntersectionTester, SpatialObject>();
		private IntersectionTester[] m_Testers = new IntersectionTester[0];

		private SpatialHash m_SpatialHash;

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

		public List<SpatialObject> allObjects
		{
			get
			{
				if (m_SpatialHash == null)
					return null;
				return m_SpatialHash.allObjects;
			}
		}

		public int spatialCellCount
		{
			get { return m_SpatialHash.spatialCellCount; }
		}

		public int intersectedObjectCount
		{
			get { return m_IntersectedObjects.Count; }
		}

		public void ClearHash()
		{
			m_SpatialHash.Clear();
		}
#endif

		void Awake()
		{
			m_ConeMesh = IntersectionTester.GenerateConeMesh(m_ConeSegments, m_ConeRadius, m_ConeHeight, out m_ConeRays);
		}

		internal void Setup(SpatialHash hash)
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
				Color color = i%2 == 0 ? Color.red : Color.green;
				i++;
				if (!tester.active)
					continue;
				if (m_SpatialHash.changes || tester.renderer.transform.hasChanged)
				{
					bool detected = false;
					var globalBucket = tester.GetCell(m_SpatialHash);
					List<SpatialObject> intersections = null;
					if (m_SpatialHash.GetIntersections(globalBucket, out intersections))
					{
						//Sort list to try and hit closer object first
						intersections.Sort((a, b) => (a.sceneObject.bounds.center - tester.renderer.bounds.center).magnitude.CompareTo((b.sceneObject.bounds.center - tester.renderer.bounds.center).magnitude));
						foreach (var obj in intersections)
						{
							//Early-outs:
							// No mesh data
							// Not updated yet
							if (obj.sceneObject.transform.hasChanged)
								continue;
							//Bounds check                                                                     
							if (!obj.sceneObject.bounds.Intersects(tester.renderer.bounds))
								continue;
							if (U.Intersection.TestObject(obj, color, tester))
							{
								detected = true;
								SpatialObject oldObject;
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
						SpatialObject intersectedObject;
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
		void OnIntersectionEnter(IntersectionTester tester, SpatialObject obj)
		{
			m_IntersectedObjects[tester] = obj;
			Debug.Log("Entered " + obj);
		}

		void OnIntersectionStay(IntersectionTester tester, SpatialObject obj)
		{
			m_IntersectedObjects[tester] = obj;
			//Debug.Log("Stayed " + obj);
		}

		void OnIntersectionExit(IntersectionTester tester, SpatialObject obj)
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

		public SpatialObject GetIntersectedObjectForTester(IntersectionTester IntersectionTester)
		{
			SpatialObject obj;
			m_IntersectedObjects.TryGetValue(IntersectionTester, out obj);
			return obj;
		}

		public SpatialObject GrabObjectAndRemove(IntersectionTester tester)
		{
			SpatialObject obj = GetIntersectedObjectForTester(tester);
			tester.grabbed = obj;
			m_SpatialHash.RemoveObject(obj);
			return obj;
		}

		public SpatialObject GrabObjectAndDisableTester(IntersectionTester IntersectionTester)
		{
			SpatialObject obj = GetIntersectedObjectForTester(IntersectionTester);
			IntersectionTester.grabbed = obj;
			IntersectionTester.active = false;
			return obj;
		}

		public SpatialObject GrabObjectAndRemoveAndDisableTester(IntersectionTester IntersectionTester)
		{
			SpatialObject obj = GetIntersectedObjectForTester(IntersectionTester);
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
			m_SpatialHash.AddObject(tester.grabbed);
			tester.grabbed = null;
			tester.active = true;
		}
	}
}