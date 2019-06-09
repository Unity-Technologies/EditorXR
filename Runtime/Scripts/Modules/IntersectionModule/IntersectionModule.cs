using System.Collections.Generic;
using Unity.Labs.EditorXR.Interfaces;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Data;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
    sealed class IntersectionModule : ScriptableSettings<IntersectionModule>, IInitializableModule,
        IModuleBehaviorCallbacks, IModuleDependency<SpatialHashModule>, IUsesGameObjectLocking,
        IGetVRPlayerObjects, IInterfaceConnector, IProvidesSceneRaycast
    {
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

        const int k_MaxTestsPerTester = 250;

        [SerializeField]
        Vector3 m_PlayerBoundsMargin = new Vector3(0.25f, 0.25f, 0.25f);

        readonly Dictionary<IntersectionTester, DirectIntersection> m_IntersectedObjects = new Dictionary<IntersectionTester, DirectIntersection>();
        readonly List<IntersectionTester> m_Testers = new List<IntersectionTester>();
        readonly Dictionary<Transform, RayIntersection> m_RaycastGameObjects = new Dictionary<Transform, RayIntersection>(); // Stores which gameobject the proxies' ray origins are pointing at
        readonly Dictionary<Transform, bool> m_RayoriginEnabled = new Dictionary<Transform, bool>();
        readonly List<GameObject> m_StandardIgnoreList = new List<GameObject>();

        SpatialHash<Renderer> m_SpatialHash;
        MeshCollider m_CollisionTester;

        public bool ready { get { return m_SpatialHash != null; } }

        public List<IntersectionTester> testers { get { return m_Testers; } }

        public List<Renderer> allObjects { get { return m_SpatialHash == null ? null : m_SpatialHash.allObjects; } }

        public int intersectedObjectCount { get { return m_IntersectedObjects.Count; } }
        public List<GameObject> standardIgnoreList { get { return m_StandardIgnoreList; } }

        public int initializationOrder { get { return 0; } }
        public int shutdownOrder { get { return 0; } }

        SpatialHashModule m_SpatialHashModule;

        // Local method use only -- created here to reduce garbage collection
        readonly List<Renderer> m_Intersections = new List<Renderer>();
        readonly List<SortableRenderer> m_SortedIntersections = new List<SortableRenderer>();

        struct SortableRenderer
        {
            public Renderer renderer;
            public float distance;
        }

        public void ConnectDependency(SpatialHashModule dependency)
        {
            m_SpatialHashModule = dependency;
        }

        public void LoadModule()
        {
            IntersectionUtils.BakedMesh = new Mesh(); // Create a new Mesh in each Awake because it is destroyed on scene load
            IControlInputIntersectionMethods.setRayOriginEnabled = SetRayOriginEnabled;

            ICheckBoundsMethods.checkBounds = CheckBounds;
            ICheckSphereMethods.checkSphere = CheckSphere;
            IContainsVRPlayerCompletelyMethods.containsVRPlayerCompletely = ContainsVRPlayerCompletely;
        }

        public void UnloadModule() { }

        public void Initialize()
        {
            var moduleParent = ModuleLoaderCore.instance.GetModuleParent();
            m_CollisionTester = EditorXRUtils.CreateGameObjectWithComponent<MeshCollider>(moduleParent.transform);

            m_SpatialHash = m_SpatialHashModule.spatialHash;
            m_IntersectedObjects.Clear();
            m_Testers.Clear();
            m_RaycastGameObjects.Clear();
            m_RayoriginEnabled.Clear();
            m_StandardIgnoreList.Clear();
        }

        public void Shutdown()
        {
            if (m_CollisionTester)
                UnityObjectUtils.Destroy(m_CollisionTester.gameObject);
        }

        public void OnBehaviorAwake() { }

        public void OnBehaviorEnable() { }

        public void OnBehaviorStart() { }

        public void OnBehaviorUpdate()
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

        public void OnBehaviorDisable() { }

        public void OnBehaviorDestroy() { }

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

        internal void SetRayOriginEnabled(Transform rayOrigin, bool enabled)
        {
            m_RayoriginEnabled[rayOrigin] = enabled;
        }

        internal void UpdateRaycast(Transform rayOrigin, float distance)
        {
            if (!m_RayoriginEnabled.ContainsKey(rayOrigin))
                m_RayoriginEnabled[rayOrigin] = true;

            GameObject go;
            RaycastHit hit;
            Raycast(new Ray(rayOrigin.position, rayOrigin.forward), out hit, out go, distance);

            if (!m_RayoriginEnabled[rayOrigin])
            {
                go = null;
                hit.distance = 0;
            }

            m_RaycastGameObjects[rayOrigin] = new RayIntersection { go = go, distance = hit.distance };
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
            var objectBounds = BoundsUtils.GetBounds(obj.transform);
            var playerBounds = BoundsUtils.GetBounds(this.GetVRPlayerObjects());
            playerBounds.extents += m_PlayerBoundsMargin;
            return objectBounds.ContainsCompletely(playerBounds);
        }

        public void ConnectInterface(object target, object userData = null)
        {
            var standardIgnoreList = target as IStandardIgnoreList;
            if (standardIgnoreList != null)
            {
                standardIgnoreList.ignoreList = this.standardIgnoreList;
            }
        }

        public void DisconnectInterface(object target, object userData = null) { }

        public void LoadProvider() { }

        public void ConnectSubscriber(object obj)
        {
#if !FI_AUTOFILL
            var raycastSubscriber = obj as IFunctionalitySubscriber<IProvidesSceneRaycast>;
            if (raycastSubscriber != null)
                raycastSubscriber.provider = this;
#endif
        }

        public void UnloadProvider() { }
    }
}
