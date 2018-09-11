#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Data;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
    sealed partial class IntersectionModule : MonoBehaviour, IUsesGameObjectLocking, IGetVRPlayerObjects
    {
        const int k_MaxTestsPerTester = 250;

        [SerializeField]
        Vector3 m_PlayerBoundsMargin = new Vector3(0.25f, 0.25f, 0.25f);

        readonly Dictionary<IntersectionTester, Renderer> m_IntersectedObjects = new Dictionary<IntersectionTester, Renderer>();
        readonly List<IntersectionTester> m_Testers = new List<IntersectionTester>();
        readonly Dictionary<Transform, RayIntersection> m_RaycastGameObjects = new Dictionary<Transform, RayIntersection>(); // Stores which gameobject the proxies' ray origins are pointing at
        readonly List<GameObject> m_StandardIgnoreList = new List<GameObject>();

        SpatialHash<Renderer> m_SpatialHash;
        MeshCollider m_CollisionTester;

        bool m_ComputeSupported;

        struct RayIntersection
        {
            public GameObject go;
            public float distance;
        }

        public bool ready { get { return m_SpatialHash != null; } }

        public List<IntersectionTester> testers { get { return m_Testers; } }

        public List<Renderer> allObjects { get { return m_SpatialHash == null ? null : m_SpatialHash.allObjects; } }

        public int intersectedObjectCount { get { return m_IntersectedObjects.Count; } }
        public List<GameObject> standardIgnoreList { get { return m_StandardIgnoreList; } }

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
            m_ComputeSupported = SystemInfo.supportsComputeShaders;
            if (m_ComputeSupported)
                SetupGPUIntersection();
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

            for (var i = 0; i < m_Testers.Count; i++)
            {
                var tester = m_Testers[i];
                if (!tester.active)
                {
                    Renderer intersectedObject;
                    if (m_IntersectedObjects.TryGetValue(tester, out intersectedObject))
                        OnIntersectionExit(tester);

                    continue;
                }

                var testerTransform = tester.transform;
                if (testerTransform.hasChanged)
                {
                    var intersectionFound = false;
                    m_Intersections.Clear();
                    var testerCollider = tester.collider;
                    if (m_SpatialHash.GetIntersections(m_Intersections, testerCollider.bounds))
                    {
                        var testerBounds = testerCollider.bounds;
                        var testerBoundsCenter = testerBounds.center;

                        m_SortedIntersections.Clear();
                        for (int j = 0; j < m_Intersections.Count; j++)
                        {
                            var obj = m_Intersections[j];

                            // Ignore destroyed objects
                            if (!obj)
                                continue;

                            // Ignore inactive objects
                            var go = obj.gameObject;
                            if (!go.activeInHierarchy)
                                continue;

                            // Ignore locked objects
                            if (this.IsLocked(go))
                                continue;

                            // Bounds check
                            if (!obj.bounds.Intersects(testerBounds))
                                continue;

                            // Check if the object is larger than the player, and the player is inside it
                            if (ContainsVRPlayerCompletely(go))
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
                            var obj = m_SortedIntersections[j].renderer;
                            var test = m_ComputeSupported
                                ? TestObjectGPU(obj, tester)
                                : IntersectionUtils.TestObject(m_CollisionTester, obj, tester);
                            if (test)
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
                                        OnIntersectionExit(tester);
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
                            OnIntersectionExit(tester);
                    }

                    testerTransform.hasChanged = false;
                }
            }
        }

        internal void AddTester(IntersectionTester tester)
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

        void OnIntersectionExit(IntersectionTester tester)
        {
            m_IntersectedObjects.Remove(tester);
        }

        internal Renderer GetIntersectedObjectForTester(IntersectionTester tester)
        {
            Renderer obj = null;
            if (tester)
                m_IntersectedObjects.TryGetValue(tester, out obj);

            return obj;
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
                    if (ignoreList != null && ignoreList.Contains(renderer.gameObject))
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
                            obj = renderer.gameObject;
                        }
                    }
                }
            }

            return result;
        }

        internal bool CheckBounds(Bounds bounds, List<GameObject> objects, List<GameObject> ignoreList = null)
        {
            var result = false;
            m_Intersections.Clear();
            if (m_SpatialHash.GetIntersections(m_Intersections, bounds))
            {
                for (var i = 0; i < m_Intersections.Count; i++)
                {
                    var renderer = m_Intersections[i];
                    if (ignoreList != null && ignoreList.Contains(renderer.gameObject))
                        continue;

                    var transform = renderer.transform;

                    IntersectionUtils.SetupCollisionTester(m_CollisionTester, transform);

                    if (IntersectionUtils.TestBox(m_CollisionTester, transform, bounds.center, bounds.extents, Quaternion.identity))
                    {
                        objects.Add(renderer.gameObject);
                        result = true;
                    }
                }
            }

            return result;
        }

        internal bool CheckSphere(Vector3 center, float radius, List<GameObject> objects, List<GameObject> ignoreList = null)
        {
            var result = false;
            m_Intersections.Clear();
            var bounds = new Bounds(center, Vector3.one * radius * 2);
            if (m_SpatialHash.GetIntersections(m_Intersections, bounds))
            {
                for (var i = 0; i < m_Intersections.Count; i++)
                {
                    var renderer = m_Intersections[i];
                    if (ignoreList != null && ignoreList.Contains(renderer.gameObject))
                        continue;

                    var transform = renderer.transform;

                    IntersectionUtils.SetupCollisionTester(m_CollisionTester, transform);

                    if (IntersectionUtils.TestSphere(m_CollisionTester, transform, center, radius))
                    {
                        objects.Add(renderer.gameObject);
                        result = true;
                    }
                }
            }

            return result;
        }

        internal bool ContainsVRPlayerCompletely(GameObject obj)
        {
            var objectBounds = ObjectUtils.GetBounds(obj.transform);
            var playerBounds = ObjectUtils.GetBounds(this.GetVRPlayerObjects());
            playerBounds.extents += m_PlayerBoundsMargin;
            return objectBounds.ContainsCompletely(playerBounds);
        }

        void OnDestroy()
        {
            TearDownGPUIntersection();
        }
    }
}
#endif
