#if UNITY_2018_3_OR_NEWER
using System.Collections.Generic;
using System.Linq;
using Unity.Labs.EditorXR.Interfaces;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEditor.Experimental.EditorVR.Proxies;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEditor.Experimental.EditorVR.Workspaces;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Core
{
    class EditorXRMiniWorldModule : IModuleDependency<EditorVR>, IModuleDependency<EditorXRDirectSelectionModule>,
        IModuleDependency<EditorXRRayModule>, IModuleDependency<SpatialHashModule>, IModuleDependency<HighlightModule>,
        IModuleDependency<IntersectionModule>, IModuleDependency<WorkspaceModule>, IUsesPlaceSceneObjects, IUsesViewerScale, IUsesSpatialHash
    {
        internal class MiniWorldRay
        {
            readonly List<GrabData> m_GrabData = new List<GrabData>();

            public Transform originalRayOrigin { get; private set; }
            public IMiniWorld miniWorld { get; private set; }
            public IProxy proxy { get; private set; }
            public Node node { get; private set; }
            public IntersectionTester tester { get; private set; }

            public bool hasPreview { get; private set; }

            public bool hasObjects
            {
                get { return m_GrabData.Count > 0; }
            }

            public bool dragStartedOutside { get; set; }
            public bool isContained { get; set; }

            class GrabData
            {
                Vector3 m_OriginalLocalPositionOffset;
                Vector3 m_LocalPositionOffset;
                Quaternion m_RotationOffset;

                public Vector3 centerPositionOffset { get; private set; }
                public Quaternion originalRotation { get; private set; }
                public Vector3 originalScale { get; private set; }

                public Transform transform { get; private set; }

                public GrabData(Transform transform, Transform parent, Vector3 center)
                {
                    this.transform = transform;
                    centerPositionOffset = transform.position - center;
                    originalRotation = transform.rotation;
                    originalScale = transform.localScale;
                    GetCurrentOffsets(parent);
                }

                public void GetCurrentOffsets(Transform parent)
                {
                    MathUtilsExt.GetTransformOffset(parent, transform, out m_LocalPositionOffset, out m_RotationOffset);
                    m_OriginalLocalPositionOffset = m_LocalPositionOffset;
                }

                public void SetScale(float scaleFactor)
                {
                    transform.localScale *= scaleFactor;
                    m_LocalPositionOffset = m_OriginalLocalPositionOffset * scaleFactor;
                }

                public void ResetScale()
                {
                    transform.localScale = originalScale;
                    m_LocalPositionOffset = m_OriginalLocalPositionOffset;
                }

                public void Update(Transform parent)
                {
                    MathUtilsExt.SetTransformOffset(parent, transform, m_LocalPositionOffset, m_RotationOffset);
                }
            }

            public MiniWorldRay(Transform originalRayOrigin, IMiniWorld miniWorld, IProxy proxy, Node node, IntersectionTester tester)
            {
                this.originalRayOrigin = originalRayOrigin;
                this.miniWorld = miniWorld;
                this.proxy = proxy;
                this.node = node;
                this.tester = tester;
            }

            public void OnObjectsGrabbed(HashSet<Transform> heldObjects, Transform rayOrigin)
            {
                var center = BoundsUtils.GetBounds(heldObjects.ToArray()).center;

                m_GrabData.Clear();
                foreach (var heldObject in heldObjects)
                {
                    m_GrabData.Add(new GrabData(heldObject, rayOrigin, center));
                }
            }

            public void TransferObjects(MiniWorldRay destinationRay, Transform rayOrigin = null)
            {
                var destinationGrabData = destinationRay.m_GrabData;
                destinationGrabData.AddRange(m_GrabData);
                m_GrabData.Clear();
                destinationRay.dragStartedOutside = dragStartedOutside;

                if (rayOrigin)
                {
                    foreach (var grabData in destinationGrabData)
                    {
                        grabData.GetCurrentOffsets(rayOrigin);
                    }
                }
            }

            public void EnterPreviewMode(IUsesSpatialHash hash, float scaleFactor)
            {
                hasPreview = true;
                foreach (var grabData in m_GrabData)
                {
                    hash.RemoveFromSpatialHash(grabData.transform.gameObject);
                    grabData.SetScale(scaleFactor);
                    grabData.Update(originalRayOrigin);
                }
            }

            public void ExitPreviewMode(IUsesSpatialHash hash)
            {
                foreach (var grabData in m_GrabData)
                {
                    hash.AddToSpatialHash(grabData.transform.gameObject);
                    grabData.ResetScale();
                }

                hasPreview = false;
            }

            public void DropPreviewObjects(IUsesPlaceSceneObjects placer)
            {
                var count = m_GrabData.Count;
                var transforms = new Transform[count];
                var targetPositionOffsets = new Vector3[count];
                var targetRotations = new Quaternion[count];
                var targetScales = new Vector3[count];

                for (var i = 0; i < count; i++)
                {
                    var grabData = m_GrabData[i];
                    transforms[i] = grabData.transform;
                    targetPositionOffsets[i] = grabData.centerPositionOffset;
                    targetRotations[i] = grabData.originalRotation;
                    targetScales[i] = grabData.originalScale;
                }

                if (hasPreview)
                    placer.PlaceSceneObjects(transforms, targetPositionOffsets, targetRotations, targetScales);

                m_GrabData.Clear();
                hasPreview = false;
            }

            public void UpdatePreview()
            {
                foreach (var grabData in m_GrabData)
                {
                    grabData.Update(originalRayOrigin);
                }
            }
        }

        EditorVR m_EditorVR;
        EditorXRDirectSelectionModule m_DirectSelectionModule;
        EditorXRRayModule m_RayModule;
        SpatialHashModule m_SpatialHashModule;
        HighlightModule m_HighlightModule;
        IntersectionModule m_IntersectionModule;

        GameObject m_ModuleParent;

        readonly Dictionary<Transform, MiniWorldRay> m_Rays = new Dictionary<Transform, MiniWorldRay>();
        readonly Dictionary<Transform, bool> m_RayWasContained = new Dictionary<Transform, bool>();

        readonly List<IMiniWorld> m_Worlds = new List<IMiniWorld>();

        bool m_MiniWorldIgnoreListDirty = true;

        // Local method use only -- created here to reduce garbage collection
        static readonly List<Renderer> k_IgnoreList = new List<Renderer>();
        static readonly List<Renderer> k_Renderers = new List<Renderer>();

        public Dictionary<Transform, MiniWorldRay> rays
        {
            get { return m_Rays; }
        }

        public List<IMiniWorld> worlds
        {
            get { return m_Worlds; }
        }

#if !FI_AUTOFILL
        IProvidesSpatialHash IFunctionalitySubscriber<IProvidesSpatialHash>.provider { get; set; }
        IProvidesPlaceSceneObjects IFunctionalitySubscriber<IProvidesPlaceSceneObjects>.provider { get; set; }
        IProvidesViewerScale IFunctionalitySubscriber<IProvidesViewerScale>.provider { get; set; }
#endif

        public void ConnectDependency(EditorXRRayModule dependency)
        {
            m_RayModule = dependency;
        }

        public void ConnectDependency(SpatialHashModule dependency)
        {
            m_SpatialHashModule = dependency;
        }

        public void ConnectDependency(HighlightModule dependency)
        {
            m_HighlightModule = dependency;
        }

        public void ConnectDependency(IntersectionModule dependency)
        {
            m_IntersectionModule = dependency;
        }

        public void ConnectDependency(WorkspaceModule dependency)
        {
            dependency.workspaceCreated += OnWorkspaceCreated;
            dependency.workspaceDestroyed += OnWorkspaceDestroyed;
        }

        public void LoadModule()
        {
#if UNITY_EDITOR
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
#endif
            IIsInMiniWorldMethods.isInMiniWorld = IsInMiniWorld;

            m_ModuleParent = ModuleLoaderCore.instance.GetModuleParent();
        }

        bool IsInMiniWorld(Transform rayOrigin)
        {
            foreach (var miniWorld in m_Worlds)
            {
                var rayOriginPosition = rayOrigin.position;
                var pointerPosition = rayOriginPosition + rayOrigin.forward * m_DirectSelectionModule.GetPointerLength(rayOrigin);
                if (miniWorld.Contains(rayOriginPosition) || miniWorld.Contains(pointerPosition))
                    return true;
            }

            return false;
        }

        public void ConnectDependency(EditorVR dependency)
        {
            m_EditorVR = dependency;
        }

        public void UnloadModule()
        {
#if UNITY_EDITOR
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
#endif
        }

        public void ConnectDependency(EditorXRDirectSelectionModule dependency)
        {
            m_DirectSelectionModule = dependency;
            dependency.objectsGrabbed += OnObjectsGrabbed;
            dependency.objectsDropped += OnObjectsDropped;
            dependency.objectsTransferred += OnObjectsTransferred;
        }

        void OnHierarchyChanged()
        {
            m_MiniWorldIgnoreListDirty = true;
        }

        /// <summary>
        /// Re-use DefaultProxyRay and strip off objects and components not needed for MiniWorldRays
        /// </summary>
        Transform InstantiateMiniWorldRay()
        {
            var miniWorldRay = EditorXRUtils.Instantiate(m_RayModule.proxyRayPrefab.gameObject).transform;
            UnityObjectUtils.Destroy(miniWorldRay.GetComponent<DefaultProxyRay>());

            var renderers = miniWorldRay.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (!renderer.GetComponentInParent<IntersectionTester>())
                    UnityObjectUtils.Destroy(renderer.gameObject);
                else
                    renderer.enabled = false;
            }

            return miniWorldRay;
        }

        void UpdateMiniWorldIgnoreList()
        {
            m_ModuleParent.GetComponentsInChildren(true, k_Renderers);
            k_IgnoreList.Clear();

            foreach (var r in k_Renderers)
            {
                if (r.CompareTag(EditorVR.VRPlayerTag))
                    continue;

                if (r.gameObject.layer != LayerMask.NameToLayer("UI") && r.CompareTag(MiniWorldRenderer.ShowInMiniWorldTag))
                    continue;

                k_IgnoreList.Add(r);
            }

            foreach (var miniWorld in m_Worlds)
            {
                miniWorld.ignoreList = k_IgnoreList;
            }
        }

        internal void UpdateMiniWorlds()
        {
            if (m_MiniWorldIgnoreListDirty)
            {
                UpdateMiniWorldIgnoreList();
                m_MiniWorldIgnoreListDirty = false;
            }

            // Update MiniWorldRays
            foreach (var ray in m_Rays)
            {
                var miniWorldRayOrigin = ray.Key;
                var miniWorldRay = ray.Value;

                if (!miniWorldRay.proxy.active)
                {
                    miniWorldRay.tester.active = false;
                    continue;
                }

                var miniWorld = miniWorldRay.miniWorld;
                var inverseScale = miniWorld.miniWorldTransform.lossyScale.Inverse();

                if (float.IsInfinity(inverseScale.x) || float.IsNaN(inverseScale.x)) // Extreme scales cause transform errors
                    continue;

                // Transform into reference space
                var originalRayOrigin = miniWorldRay.originalRayOrigin;
                var referenceTransform = miniWorld.referenceTransform;
                var miniWorldTransform = miniWorld.miniWorldTransform;
                miniWorldRayOrigin.position = referenceTransform.TransformPoint(miniWorldTransform.InverseTransformPoint(originalRayOrigin.position));
                miniWorldRayOrigin.rotation = referenceTransform.rotation * Quaternion.Inverse(miniWorldTransform.rotation) * originalRayOrigin.rotation;
                miniWorldRayOrigin.localScale = Vector3.Scale(inverseScale, referenceTransform.localScale);

                // Set miniWorldRayOrigin active state based on whether controller is inside corresponding MiniWorld
                var originalPointerPosition = originalRayOrigin.position + originalRayOrigin.forward * m_DirectSelectionModule.GetPointerLength(originalRayOrigin);
                var isContained = miniWorld.Contains(originalPointerPosition);
                miniWorldRay.tester.active = isContained;
                miniWorldRayOrigin.gameObject.SetActive(isContained);

                var miniWorldRayObjects = m_DirectSelectionModule.GetHeldObjects(miniWorldRayOrigin);
                var originalRayObjects = m_DirectSelectionModule.GetHeldObjects(originalRayOrigin);

                var hasPreview = miniWorldRay.hasPreview;
                if (miniWorldRayObjects == null && originalRayObjects == null && !hasPreview)
                {
                    miniWorldRay.isContained = isContained;
                    continue;
                }

                var wasContained = miniWorldRay.isContained;
                var dragStartedOutside = miniWorldRay.dragStartedOutside;
                if (isContained != wasContained)
                {
                    // Early out if we grabbed a real-world object that started inside a mini world
                    if (!isContained && miniWorldRayObjects == null)
                    {
                        miniWorldRay.isContained = false;
                        continue;
                    }

                    // Transfer objects to and from original ray and MiniWorld ray (e.g. outside to inside mini world)
                    var from = isContained ? originalRayOrigin : miniWorldRayOrigin;
                    var to = isContained ? miniWorldRayOrigin : originalRayOrigin;

                    KeyValuePair<Transform, MiniWorldRay>? overlapPair = null;
                    MiniWorldRay incomingPreview = null;

                    // Try to transfer objects between MiniWorlds
                    if (miniWorldRayObjects != null && !isContained)
                    {
                        foreach (var kvp in m_Rays)
                        {
                            var otherRayOrigin = kvp.Key;
                            var otherRay = kvp.Value;
                            if (originalRayOrigin == otherRay.originalRayOrigin && otherRay != miniWorldRay && otherRay.isContained)
                            {
                                overlapPair = kvp;
                                from = miniWorldRayOrigin;
                                to = otherRayOrigin;
                                break;
                            }
                        }
                    }

                    if (originalRayObjects != null && isContained && !dragStartedOutside)
                    {
                        //Check for other miniworlds' previews entering this ray's miniworld
                        foreach (var kvp in m_Rays)
                        {
                            var otherRay = kvp.Value;
                            if (originalRayOrigin == otherRay.originalRayOrigin && otherRay != miniWorldRay && otherRay.hasObjects)
                            {
                                incomingPreview = otherRay;
                                from = originalRayOrigin;
                                to = miniWorldRayOrigin;
                                break;
                            }
                        }
                    }

                    var pointerLengthDiff = m_DirectSelectionModule.GetPointerLength(to) - m_DirectSelectionModule.GetPointerLength(from);
                    m_DirectSelectionModule.TransferHeldObjects(from, to, Vector3.forward * pointerLengthDiff);

                    if (overlapPair.HasValue)
                    {
                        var kvp = overlapPair.Value;
                        miniWorldRay.TransferObjects(kvp.Value, kvp.Key);
                    }

                    if (incomingPreview != null)
                    {
                        incomingPreview.ExitPreviewMode(this);
                        incomingPreview.TransferObjects(miniWorldRay);
                        m_DirectSelectionModule.ResumeGrabbers(incomingPreview.node);
                    }

                    if (!isContained)
                        m_RayWasContained[originalRayOrigin] = false; //Prevent ray from showing
                }

                if (dragStartedOutside)
                {
                    miniWorldRay.isContained = isContained;
                    continue;
                }

                var node = miniWorldRay.node;

                if (miniWorldRayObjects != null && !isContained && wasContained)
                {
                    var containedInOtherMiniWorld = false;
                    foreach (var world in m_Worlds)
                    {
                        if (miniWorld != world && world.Contains(originalPointerPosition))
                            containedInOtherMiniWorld = true;
                    }

                    // Transfer objects from miniworld to preview state
                    // Don't switch to previewing the objects we are dragging if we are still in another mini world
                    if (!containedInOtherMiniWorld)
                    {
                        // Check for player head
                        var playerHead = false;
                        foreach (var obj in miniWorldRayObjects)
                        {
                            if (obj.CompareTag(EditorVR.VRPlayerTag))
                            {
                                playerHead = true;
                                m_DirectSelectionModule.DropHeldObjects(node);
                                break;
                            }
                        }

                        if (!playerHead)
                        {
                            var scaleFactor = this.GetViewerScale() / miniWorld.referenceTransform.localScale.x;
                            miniWorldRay.EnterPreviewMode(this, scaleFactor);
                            m_DirectSelectionModule.SuspendGrabbers(node);
                        }
                    }
                }

                if (hasPreview)
                {
                    // Check if we have just entered another miniworld
                    var enterOther = false;
                    foreach (var kvp in m_Rays)
                    {
                        var otherRay = kvp.Value;
                        var otherMiniWorld = otherRay.miniWorld;
                        if (otherMiniWorld != miniWorld && otherRay.node == node && otherMiniWorld.Contains(originalPointerPosition))
                        {
                            miniWorldRay.ExitPreviewMode(this);
                            m_DirectSelectionModule.ResumeGrabbers(node);
                            enterOther = true;
                            break;
                        }
                    }

                    if (!enterOther)
                    {
                        if (!isContained)
                        {
                            miniWorldRay.UpdatePreview();
                        }
                        else if (!wasContained)
                        {
                            miniWorldRay.ExitPreviewMode(this);
                            m_DirectSelectionModule.ResumeGrabbers(node);
                        }
                    }
                }

                miniWorldRay.isContained = isContained;
            }

            // Update ray visibilities
            foreach (var deviceData in m_EditorVR.deviceData)
            {
                var proxy = deviceData.proxy;
                if (!proxy.active)
                    continue;

                UpdateRayContaimnent(deviceData);
            }
        }

        void UpdateRayContaimnent(DeviceData data)
        {
            bool wasContained;
            var rayOrigin = data.rayOrigin;
            m_RayWasContained.TryGetValue(rayOrigin, out wasContained);

            var isContained = false;
            foreach (var miniWorld in m_Worlds)
            {
                isContained |= miniWorld.Contains(rayOrigin.position + rayOrigin.forward * m_DirectSelectionModule.GetPointerLength(rayOrigin));
            }

            if (isContained && !wasContained)
                m_RayModule.AddVisibilitySettings(rayOrigin, this, false, true);

            if (!isContained && wasContained)
                m_RayModule.RemoveVisibilitySettings(rayOrigin, this);

            m_RayWasContained[rayOrigin] = isContained;
        }

        internal void OnWorkspaceCreated(IWorkspace workspace)
        {
            var miniWorldWorkspace = workspace as MiniWorldWorkspace;
            if (!miniWorldWorkspace)
                return;

            miniWorldWorkspace.zoomSliderMax = m_SpatialHashModule.GetMaxBounds().size.MaxComponent()
                / miniWorldWorkspace.contentBounds.size.MaxComponent();

            var miniWorld = miniWorldWorkspace.miniWorld;
            var worldID = m_Worlds.Count;
            miniWorld.miniWorldTransform.name = string.Format("Miniworld {0}", worldID);
            m_Worlds.Add(miniWorld);

            m_RayModule.ForEachProxyDevice(deviceData =>
            {
                var node = deviceData.node;
                var rayOrigin = deviceData.rayOrigin;
                var proxy = deviceData.proxy;

                var miniWorldRayOrigin = InstantiateMiniWorldRay();
                miniWorldRayOrigin.name = string.Format("{0} Miniworld {1} Ray", node, worldID);
                miniWorldRayOrigin.parent = workspace.transform;

                var tester = miniWorldRayOrigin.GetComponentInChildren<IntersectionTester>();
                tester.active = false;

                m_Rays[miniWorldRayOrigin] = new MiniWorldRay(rayOrigin, miniWorld, proxy, node, tester);

                m_IntersectionModule.AddTester(tester);

                m_HighlightModule.AddRayOriginForNode(node, miniWorldRayOrigin);

                if (proxy.active)
                {
                    if (node == Node.LeftHand)
                        miniWorldWorkspace.leftRayOrigin = rayOrigin;

                    if (node == Node.RightHand)
                        miniWorldWorkspace.rightRayOrigin = rayOrigin;
                }
            }, false);
        }

        internal void OnWorkspaceDestroyed(IWorkspace workspace)
        {
            var miniWorldWorkspace = workspace as MiniWorldWorkspace;
            if (!miniWorldWorkspace)
                return;

            var miniWorld = miniWorldWorkspace.miniWorld;

            // Clean up MiniWorldRays
            m_Worlds.Remove(miniWorld);
            var miniWorldRaysCopy = new Dictionary<Transform, MiniWorldRay>(m_Rays);
            foreach (var ray in miniWorldRaysCopy)
            {
                var miniWorldRay = ray.Value;
                if (miniWorldRay.miniWorld == miniWorld)
                    m_Rays.Remove(ray.Key);
            }
        }

        void OnObjectsGrabbed(Transform rayOrigin, HashSet<Transform> grabbedObjects)
        {
            foreach (var kvp in m_Rays)
            {
                var miniWorldRayOrigin = kvp.Key;
                var ray = kvp.Value;
                var isOriginalRayOrigin = rayOrigin == ray.originalRayOrigin;
                if (isOriginalRayOrigin)
                    ray.dragStartedOutside = true;

                var isMiniWorldRayOrigin = rayOrigin == miniWorldRayOrigin;
                if (isOriginalRayOrigin || isMiniWorldRayOrigin)
                    ray.OnObjectsGrabbed(grabbedObjects, rayOrigin);
            }
        }

        void OnObjectsDropped(Transform rayOrigin, Transform[] grabbedObjects)
        {
            var node = Node.None;
            foreach (var ray in m_Rays)
            {
                var miniWorldRay = ray.Value;
                if (ray.Key == rayOrigin || miniWorldRay.originalRayOrigin == rayOrigin)
                {
                    node = miniWorldRay.node;
                    break;
                }
            }

            foreach (var ray in m_Rays)
            {
                var miniWorldRay = ray.Value;
                if (miniWorldRay.node == node)
                {
                    miniWorldRay.DropPreviewObjects(this);
                    miniWorldRay.dragStartedOutside = false;

                    if (!miniWorldRay.isContained)
                        m_RayModule.RemoveVisibilitySettings(rayOrigin, this);
                }
            }
        }

        void OnObjectsTransferred(Transform sourceRayOrigin, Transform destinationRayOrigin)
        {
            // Handle hand-to-hand transfers from two-handed scaling
            foreach (var src in m_Rays)
            {
                var srcRayOrigin = src.Key;
                var srcRay = src.Value;
                var srcRayHasObjects = srcRay.hasObjects;
                if (srcRayOrigin == sourceRayOrigin)
                {
                    if (srcRayHasObjects)
                    {
                        foreach (var dest in m_Rays)
                        {
                            if (dest.Key == destinationRayOrigin)
                            {
                                srcRay.TransferObjects(dest.Value, destinationRayOrigin);
                                break;
                            }
                        }
                    }
                }

                var srcRayOriginalRayOrigin = srcRay.originalRayOrigin;
                if (srcRayOriginalRayOrigin == sourceRayOrigin)
                {
                    if (srcRayHasObjects)
                    {
                        foreach (var dest in m_Rays)
                        {
                            var destRay = dest.Value;
                            if (destRay.originalRayOrigin == destinationRayOrigin && destRay.miniWorld == srcRay.miniWorld)
                                srcRay.TransferObjects(destRay, destinationRayOrigin);
                        }
                    }
                }
            }
        }
    }
}
#endif
