using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Labs.EditorXR.Interfaces;
using Unity.Labs.EditorXR.Utilities;
using Unity.Labs.ModuleLoader;
using Unity.Labs.SpatialHash;
using Unity.Labs.Utils;
using UnityEngine;

namespace Unity.Labs.EditorXR.Modules
{
    sealed class IntersectionModule : ScriptableSettings<IntersectionModule>, IDelayedInitializationModule, IModuleBehaviorCallbacks,
        IUsesGameObjectLocking, IUsesGetVRPlayerObjects, IProvidesSceneRaycast, IProvidesControlInputIntersection,
        IProvidesContainsVRPlayerCompletely, IProvidesCheckSphere, IProvidesCheckBounds
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
        readonly Dictionary<Transform, bool> m_RayOriginEnabled = new Dictionary<Transform, bool>();
        Coroutine m_UpdateCoroutine;

        ISpatialHashContainer<Renderer> m_SpatialHashContainer;
        MeshCollider m_CollisionTester;

        public Func<GameObject, bool> shouldExcludeObject { private get; set; }

        public int initializationOrder { get { return -3; } }
        public int shutdownOrder { get { return 0; } }

        SpatialHashModule m_SpatialHashModule;

#if !FI_AUTOFILL
        IProvidesGameObjectLocking IFunctionalitySubscriber<IProvidesGameObjectLocking>.provider { get; set; }
        IProvidesGetVRPlayerObjects IFunctionalitySubscriber<IProvidesGetVRPlayerObjects>.provider { get; set; }
#endif

        // Local method use only -- created here to reduce garbage collection. Collections must be cleared before use
        static readonly List<Renderer> k_Renderers = new List<Renderer>();
        static readonly List<SortableRenderer> k_SortedIntersections = new List<SortableRenderer>();
        static readonly List<Renderer> k_ChangedObjects = new List<Renderer>();

        struct SortableRenderer
        {
            public Renderer renderer;
            public float distance;
        }

        public void LoadModule()
        {
            IntersectionUtils.BakedMesh = new Mesh(); // Create a new Mesh in LoadModule because it is destroyed on scene load
            m_SpatialHashModule = ModuleLoaderCore.instance.GetModule<SpatialHashModule>();
        }

        public void UnloadModule() { }

        public void Initialize()
        {
            var moduleParent = ModuleLoaderCore.instance.GetModuleParent();
            var collisionTesterObject = new GameObject();
            collisionTesterObject.transform.SetParent(moduleParent.transform, false);
            m_CollisionTester = collisionTesterObject.AddComponent<MeshCollider>();

            if (m_SpatialHashModule != null)
            {
                m_SpatialHashModule.Clear();
                m_SpatialHashContainer = m_SpatialHashModule.GetOrCreateContainer<Renderer>();
            }

            m_IntersectedObjects.Clear();
            m_Testers.Clear();
            m_RaycastGameObjects.Clear();
            m_RayOriginEnabled.Clear();

            shouldExcludeObject = go => go.transform.IsChildOf(moduleParent.transform);

            SetupObjects();
            m_UpdateCoroutine = EditorMonoBehaviour.instance.StartCoroutine(UpdateDynamicObjects());
        }

        void SetupObjects()
        {
            k_Renderers.Clear();
            var meshFilters = FindObjectsOfType<MeshFilter>();
            foreach (var meshFilter in meshFilters)
            {
                if (meshFilter.sharedMesh)
                {
                    if (shouldExcludeObject != null && shouldExcludeObject(meshFilter.gameObject))
                        continue;

                    var renderer = meshFilter.GetComponent<Renderer>();
                    if (renderer)
                        k_Renderers.Add(renderer);
                }
            }

            var skinnedMeshRenderers = FindObjectsOfType<SkinnedMeshRenderer>();
            foreach (var skinnedMeshRenderer in skinnedMeshRenderers)
            {
                if (skinnedMeshRenderer.sharedMesh)
                {
                    if (shouldExcludeObject != null && shouldExcludeObject(skinnedMeshRenderer.gameObject))
                        continue;

                    k_Renderers.Add(skinnedMeshRenderer);
                }
            }

            m_SpatialHashModule.AddRenderers(k_Renderers);
        }

        IEnumerator UpdateDynamicObjects()
        {
            while (true)
            {
                k_ChangedObjects.Clear();

                // TODO AE 9/21/16: Hook updates of new objects that are created
                var allObjects = m_SpatialHashContainer.GetObjects();
                foreach (var obj in allObjects)
                {
                    if (!obj)
                    {
                        k_ChangedObjects.Add(obj);
                        continue;
                    }

                    if (obj.transform.hasChanged)
                    {
                        k_ChangedObjects.Add(obj);
                        obj.transform.hasChanged = false;
                    }
                }

                m_SpatialHashModule.RemoveObjects(k_ChangedObjects);
                m_SpatialHashModule.AddRenderers(k_ChangedObjects);
                m_SpatialHashContainer.Trim();

                yield return null;
            }
        }

        public void Shutdown()
        {
            EditorMonoBehaviour.instance.StopCoroutine(m_UpdateCoroutine);
            if (m_SpatialHashModule != null)
                m_SpatialHashModule.Clear();

            if (m_CollisionTester != null)
                UnityObjectUtils.Destroy(m_CollisionTester.gameObject);
        }

        public void OnBehaviorAwake() { }

        public void OnBehaviorEnable() { }

        public void OnBehaviorStart() { }

        public void OnBehaviorUpdate()
        {
            if (m_SpatialHashContainer == null)
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
                    k_Renderers.Clear();
                    var testerCollider = tester.collider;
                    if (m_SpatialHashContainer.GetIntersections(k_Renderers, testerCollider.bounds))
                    {
                        var testerBounds = testerCollider.bounds;
                        var testerBoundsCenter = testerBounds.center;

                        k_SortedIntersections.Clear();
                        for (int j = 0; j < k_Renderers.Count; j++)
                        {
                            var obj = k_Renderers[j];

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

                            k_SortedIntersections.Add(new SortableRenderer
                            {
                                renderer = obj,
                                distance = (obj.bounds.center - testerBoundsCenter).magnitude
                            });
                        }

                        //Sort list to try and hit closer object first
                        k_SortedIntersections.Sort((a, b) => a.distance.CompareTo(b.distance));

                        if (k_SortedIntersections.Count > k_MaxTestsPerTester)
                            continue;

                        for (int j = 0; j < k_SortedIntersections.Count; j++)
                        {
                            Vector3 contactPoint;
                            var renderer = k_SortedIntersections[j].renderer;
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

        public void SetRayOriginEnabled(Transform rayOrigin, bool enabled)
        {
            m_RayOriginEnabled[rayOrigin] = enabled;
        }

        internal void UpdateRaycast(Transform rayOrigin, float distance)
        {
            if (!m_RayOriginEnabled.ContainsKey(rayOrigin))
                m_RayOriginEnabled[rayOrigin] = true;

            GameObject go;
            RaycastHit hit;
            Raycast(new Ray(rayOrigin.position, rayOrigin.forward), out hit, out go, distance);

            // TODO: check enabled before doing raycast
            if (!m_RayOriginEnabled[rayOrigin])
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
            k_Renderers.Clear();
            if (m_SpatialHashContainer.GetIntersections(k_Renderers, ray, maxDistance))
            {
                for (int i = 0; i < k_Renderers.Count; i++)
                {
                    var renderer = k_Renderers[i];
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

        public bool CheckBounds(Bounds bounds, List<GameObject> objects, List<GameObject> ignoreList = null)
        {
            var result = false;
            k_Renderers.Clear();
            if (m_SpatialHashContainer.GetIntersections(k_Renderers, bounds))
            {
                for (var i = 0; i < k_Renderers.Count; i++)
                {
                    var renderer = k_Renderers[i];
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

        public bool CheckSphere(Vector3 center, float radius, List<GameObject> objects, List<GameObject> ignoreList = null)
        {
            var result = false;
            k_Renderers.Clear();
            var bounds = new Bounds(center, radius * 2 * Vector3.one);
            if (m_SpatialHashContainer.GetIntersections(k_Renderers, bounds))
            {
                for (var i = 0; i < k_Renderers.Count; i++)
                {
                    var renderer = k_Renderers[i];
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

        public bool ContainsVRPlayerCompletely(GameObject obj)
        {
            var vrPlayerObjects = this.GetVRPlayerObjects();
            if (vrPlayerObjects.Count == 0)
                return false;

            var objectBounds = BoundsUtils.GetBounds(obj.transform);
            var playerBounds = BoundsUtils.GetBounds(vrPlayerObjects);
            playerBounds.extents += m_PlayerBoundsMargin;
            return objectBounds.ContainsCompletely(playerBounds);
        }

        public void LoadProvider() { }

        public void ConnectSubscriber(object obj)
        {
#if !FI_AUTOFILL
            var raycastSubscriber = obj as IFunctionalitySubscriber<IProvidesSceneRaycast>;
            if (raycastSubscriber != null)
                raycastSubscriber.provider = this;

            var controlInputIntersectionSubscriber = obj as IFunctionalitySubscriber<IProvidesControlInputIntersection>;
            if (controlInputIntersectionSubscriber != null)
                controlInputIntersectionSubscriber.provider = this;

            var containsVRPlayerCompletelySubscriber = obj as IFunctionalitySubscriber<IProvidesContainsVRPlayerCompletely>;
            if (containsVRPlayerCompletelySubscriber != null)
                containsVRPlayerCompletelySubscriber.provider = this;

            var checkSphereSubscriber = obj as IFunctionalitySubscriber<IProvidesCheckSphere>;
            if (checkSphereSubscriber != null)
                checkSphereSubscriber.provider = this;

            var checkBoundsSubscriber = obj as IFunctionalitySubscriber<IProvidesCheckBounds>;
            if (checkBoundsSubscriber != null)
                checkBoundsSubscriber.provider = this;
#endif
        }

        public void UnloadProvider() { }
    }
}
