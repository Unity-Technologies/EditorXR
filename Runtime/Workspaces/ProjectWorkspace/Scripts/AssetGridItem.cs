using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Labs.EditorXR.Core;
using Unity.Labs.EditorXR.Data;
using Unity.Labs.EditorXR.Extensions;
using Unity.Labs.EditorXR.Handles;
using Unity.Labs.EditorXR.Helpers;
using Unity.Labs.EditorXR.Interfaces;
using Unity.Labs.EditorXR.Proxies;
using Unity.Labs.EditorXR.Utilities;
using Unity.Labs.ModuleLoader;
using Unity.Labs.SpatialHash;
using Unity.Labs.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputNew;
using UnityEngine.UI;

namespace Unity.Labs.EditorXR.Workspaces
{
    sealed class AssetGridItem : DraggableListItem<AssetData, int>, IUsesPlaceSceneObject, IUsesSpatialHash, IUsesSetHighlight,
        IUsesViewerBody, IUsesRayVisibilitySettings, IUsesRequestFeedback, IUsesDirectSelection, IUsesRaycastResults, IUpdateInspectors
    {
        const float k_PreviewDuration = 0.1f;
        const float k_MinPreviewScale = 0.01f;
        const float k_IconPreviewScale = 0.1f;
        const float k_MaxPreviewScale = 0.2f;
        const float k_RotateSpeed = 50f;
        const float k_TransitionDuration = 0.1f;
        const float k_ScaleBump = 1.1f;
        const int k_PreviewRenderQueue = 9200;

        const int k_AutoHidePreviewVertexCount = 10000;
        const int k_HidePreviewVertexCount = 100000;

        const float k_CheckAssignDelayTime = 0.125f;

#pragma warning disable 649
        [SerializeField]
        TextMeshProUGUI m_Text;

        [SerializeField]
        BaseHandle m_Handle;

        [SerializeField]
        Image m_TextPanel;

        [SerializeField]
        Renderer m_Cube;

        [SerializeField]
        Renderer m_Sphere;

        [SerializeField]
        Material m_PositiveAssignmentHighlightMaterial;

        [SerializeField]
        Material m_NegativeAssignmentHighlightMaterial;

        [HideInInspector]
        [SerializeField] // Serialized so that this remains set after cloning
        GameObject m_Icon;

        [HideInInspector]
        [SerializeField] // Serialized so that this remains set after cloning
        Transform m_PreviewObjectTransform;

        [SerializeField]
        bool m_IncludeRaySelectForDrop;
#pragma warning restore 649

        GameObject m_IconPrefab;

        bool m_Setup;
        bool m_AutoHidePreview;
        Vector3 m_PreviewPrefabScale;
        Vector3 m_PreviewTargetScale;
        Vector3 m_PreviewPivotOffset;
        Bounds m_PreviewBounds;
        Transform m_PreviewObjectClone;

        Coroutine m_PreviewCoroutine;
        Coroutine m_VisibilityCoroutine;

        Material m_SphereMaterial;

        // in priority order, the types of Components that you can assign this asset to
        List<Type> m_AssignmentDependencyTypes = new List<Type>();

        GameObject m_CachedDropSelection;
        float m_LastDragSelectionChange;

        // negative value means object has been checked and can't be assigned to
        // positive means it can be assigned, 0 means it hasn't yet been checked
        readonly Dictionary<int, float> m_ObjectAssignmentChecks = new Dictionary<int, float>();

        readonly List<Renderer> m_SelectionRenderers = new List<Renderer>();
        readonly Dictionary<Renderer, Material> m_SelectionOriginalMaterials = new Dictionary<Renderer, Material>();

        public GameObject icon
        {
            private get { return m_Icon ? m_Icon : m_Cube.gameObject; }
            set
            {
                m_Cube.gameObject.SetActive(false);
                m_Sphere.gameObject.SetActive(false);

                if (m_IconPrefab == value) // If this GridItem already has this icon loaded, just refresh it's active state
                {
                    m_Icon.SetActive(!m_PreviewObjectTransform || m_AutoHidePreview);
                    return;
                }

                if (m_Icon)
                    UnityObjectUtils.Destroy(m_Icon);

                m_IconPrefab = value;
                m_Icon = EditorXRUtils.Instantiate(m_IconPrefab, transform, false);
                m_Icon.transform.localPosition = Vector3.up * 0.5f;
                m_Icon.transform.localRotation = Quaternion.AngleAxis(90, Vector3.down);
                m_Icon.transform.localScale = Vector3.one;

                if (m_PreviewObjectTransform && !m_AutoHidePreview)
                    m_Icon.SetActive(false);
            }
        }

        public Material material
        {
            set
            {
                if (m_SphereMaterial)
                    UnityObjectUtils.Destroy(m_SphereMaterial);

                m_SphereMaterial = Instantiate(value);
                m_SphereMaterial.renderQueue = k_PreviewRenderQueue;
                m_Sphere.sharedMaterial = m_SphereMaterial;
                m_Sphere.gameObject.SetActive(true);

                m_Cube.gameObject.SetActive(false);

                if (m_Icon)
                    m_Icon.gameObject.SetActive(false);
            }
        }

        public Texture texture
        {
            set
            {
                m_Sphere.gameObject.SetActive(true);
                m_Cube.gameObject.SetActive(false);

                if (m_Icon)
                    m_Icon.gameObject.SetActive(false);

                if (!value)
                {
                    m_Sphere.sharedMaterial.mainTexture = null;
                    return;
                }

                if (m_SphereMaterial)
                    UnityObjectUtils.Destroy(m_SphereMaterial);

                m_SphereMaterial = new Material(Shader.Find("Standard")) { mainTexture = value };
                m_SphereMaterial.renderQueue = k_PreviewRenderQueue;
                m_Sphere.sharedMaterial = m_SphereMaterial;
            }
        }

        public Texture fallbackTexture
        {
            set
            {
                if (value)
                    value.wrapMode = TextureWrapMode.Clamp;

                m_Cube.sharedMaterial.mainTexture = value;
                m_Cube.gameObject.SetActive(true);
                m_Sphere.gameObject.SetActive(false);

                if (m_Icon)
                    m_Icon.gameObject.SetActive(false);
            }
        }

        public float scaleFactor { private get; set; }

#if !FI_AUTOFILL
        IProvidesRaycastResults IFunctionalitySubscriber<IProvidesRaycastResults>.provider { get; set; }
        IProvidesSpatialHash IFunctionalitySubscriber<IProvidesSpatialHash>.provider { get; set; }
        IProvidesPlaceSceneObject IFunctionalitySubscriber<IProvidesPlaceSceneObject>.provider { get; set; }
        IProvidesViewerBody IFunctionalitySubscriber<IProvidesViewerBody>.provider { get; set; }
        IProvidesDirectSelection IFunctionalitySubscriber<IProvidesDirectSelection>.provider { get; set; }
        IProvidesSetHighlight IFunctionalitySubscriber<IProvidesSetHighlight>.provider { get; set; }
        IProvidesRequestFeedback IFunctionalitySubscriber<IProvidesRequestFeedback>.provider { get; set; }
        IProvidesRayVisibilitySettings IFunctionalitySubscriber<IProvidesRayVisibilitySettings>.provider { get; set; }
#endif

        public override void Setup(AssetData listData, bool firstTime = false)
        {
            base.Setup(listData, firstTime);

            m_PreviewCoroutine = null;
            m_VisibilityCoroutine = null;
            m_AutoHidePreview = false;
            icon.transform.localScale = Vector3.one;

            // First time setup
            if (!m_Setup)
            {
                // Cube material might change, so we always instance it
                MaterialUtils.GetMaterialClone(m_Cube);

                m_Handle.dragStarted += OnDragStarted;
                m_Handle.dragging += OnDragging;
                m_Handle.dragging += OnDraggingFeedForward;
                m_Handle.dragEnded += OnDragEnded;

                m_Handle.hoverStarted += OnHoverStarted;
                m_Handle.hoverEnded += OnHoverEnded;

                m_Handle.getDropObject = GetDropObject;

                AssetDropUtils.AssignmentDependencies.TryGetValue(data.type, out m_AssignmentDependencyTypes);

                m_Setup = true;
            }

            InstantiatePreview();

            m_Text.text = listData.name;
        }

        public void UpdateTransforms(float scale)
        {
            scaleFactor = scale;

            // Don't scale the item while changing visibility because this would conflict with AnimateVisibility
            if (m_VisibilityCoroutine != null)
                return;

            transform.localScale = Vector3.one * scale;

            m_TextPanel.transform.localRotation = CameraUtils.LocalRotateTowardCamera(transform.parent);

            if (m_Sphere.gameObject.activeInHierarchy)
                m_Sphere.transform.Rotate(Vector3.up, k_RotateSpeed * Time.deltaTime, Space.Self);

            if (data.type == "Scene")
            {
                icon.transform.rotation =
                    Quaternion.LookRotation(icon.transform.position - CameraUtils.GetMainCamera().transform.position, Vector3.up);
            }
        }

        void InstantiatePreview()
        {
            if (m_PreviewObjectTransform)
                UnityObjectUtils.Destroy(m_PreviewObjectTransform.gameObject);

            var preview = data.preview;
            if (!preview)
                return;

            m_PreviewObjectTransform = Instantiate(preview).transform;

            m_PreviewObjectTransform.position = Vector3.zero;
            m_PreviewObjectTransform.rotation = Quaternion.identity;

            m_PreviewPrefabScale = m_PreviewObjectTransform.localScale;

            // Normalize total scale to 1
            m_PreviewBounds = BoundsUtils.GetBounds(m_PreviewObjectTransform);

            // Don't show a preview if there are no renderers
            if (m_PreviewBounds.size == Vector3.zero)
            {
                UnityObjectUtils.Destroy(m_PreviewObjectTransform.gameObject);
                return;
            }

            // Turn off expensive render settings
            foreach (var renderer in m_PreviewObjectTransform.GetComponentsInChildren<Renderer>())
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            }

            // Turn off lights
            foreach (var light in m_PreviewObjectTransform.GetComponentsInChildren<Light>())
            {
                light.enabled = false;
            }

            m_PreviewPivotOffset = m_PreviewObjectTransform.position - m_PreviewBounds.center;
            m_PreviewObjectTransform.SetParent(transform, false);

            var maxComponent = m_PreviewBounds.size.MaxComponent();
            var scaleFactor = 1 / maxComponent;
            m_PreviewTargetScale = m_PreviewPrefabScale * scaleFactor;
            m_PreviewObjectTransform.localPosition = m_PreviewPivotOffset * scaleFactor + Vector3.up * 0.5f;

            var vertCount = 0;
            foreach (var meshFilter in m_PreviewObjectTransform.GetComponentsInChildren<MeshFilter>())
            {
                if (meshFilter.sharedMesh)
                    vertCount += meshFilter.sharedMesh.vertexCount;
            }

            foreach (var skinnedMeshRenderer in m_PreviewObjectTransform.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (skinnedMeshRenderer.sharedMesh)
                    vertCount += skinnedMeshRenderer.sharedMesh.vertexCount;
            }

            // Do not show previews over a max vert count
            if (vertCount > k_HidePreviewVertexCount)
            {
                UnityObjectUtils.Destroy(m_PreviewObjectTransform.gameObject);
                return;
            }

            // Auto hide previews over a smaller vert count
            if (vertCount > k_AutoHidePreviewVertexCount)
            {
                m_AutoHidePreview = true;
                m_PreviewObjectTransform.localScale = Vector3.zero;
            }
            else
            {
                m_PreviewObjectTransform.localScale = m_PreviewTargetScale;
                icon.SetActive(false);
            }
        }

        protected override void OnDragStarted(BaseHandle handle, HandleEventData eventData)
        {
            base.OnDragStarted(handle, eventData);

            var rayOrigin = eventData.rayOrigin;
            this.AddRayVisibilitySettings(rayOrigin, this, m_IncludeRaySelectForDrop, true);

            var clone = Instantiate(gameObject, transform.position, transform.rotation, transform.parent);
            var cloneItem = clone.GetComponent<AssetGridItem>();

            var type = data.type;
            if (cloneItem.m_PreviewObjectTransform)
            {
                m_PreviewObjectClone = cloneItem.m_PreviewObjectTransform;

#if UNITY_EDITOR
                if (type == AssetData.PrefabTypeString || type == AssetData.ModelTypeString)
                {
                    var originalPosition = m_PreviewObjectClone.position;
                    var originalRotation = m_PreviewObjectClone.rotation;
                    var originalScale = m_PreviewObjectClone.localScale;
                    var restoreParent = m_PreviewObjectClone.parent;
                    UnityObjectUtils.Destroy(m_PreviewObjectClone.gameObject);
                    m_PreviewObjectClone = ((GameObject)PrefabUtility.InstantiatePrefab(data.asset)).transform;
                    m_PreviewObjectClone.SetParent(restoreParent, false);
                    m_PreviewObjectClone.position = originalPosition;
                    m_PreviewObjectClone.rotation = originalRotation;
                    m_PreviewObjectClone.localScale = originalScale;
                    cloneItem.m_PreviewObjectTransform = m_PreviewObjectClone;
                }
#endif

                cloneItem.m_Cube.gameObject.SetActive(false);

                if (cloneItem.m_Icon)
                    cloneItem.m_Icon.gameObject.SetActive(false);

                m_PreviewObjectClone.gameObject.SetActive(true);
                m_PreviewObjectClone.localScale = m_PreviewTargetScale;

                // Destroy label
                UnityObjectUtils.Destroy(cloneItem.m_TextPanel.gameObject);
            }

            m_DragObject = clone.transform;

            // Disable any SmoothMotion that may be applied to a cloned Asset Grid Item now referencing input device p/r/s
            var smoothMotion = clone.GetComponent<SmoothMotion>();
            if (smoothMotion != null)
                smoothMotion.enabled = false;

            // setup our assignment dependency list with any known types
            AssetDropUtils.AssignmentDependencies.TryGetValue(type, out m_AssignmentDependencyTypes);

            StartCoroutine(ShowGrabbedObject());
        }

        float PreviouslyFoundResult(GameObject go)
        {
            float previous;
            m_ObjectAssignmentChecks.TryGetValue(go.GetInstanceID(), out previous);
            return previous;
        }

        void SetAssignableHighlight(GameObject selection, Transform rayOrigin, bool assignable)
        {
            if (assignable)
            {
                // blinking green highlight = YES, object can have this asset assigned
                var mat = m_PositiveAssignmentHighlightMaterial;
                this.SetBlinkingHighlight(selection, true, rayOrigin, mat);
            }
            else
            {
                // solid red highlight = NO, object can't have this asset assigned
                var mat = m_NegativeAssignmentHighlightMaterial;
                this.SetHighlight(selection, true, rayOrigin, mat);
            }
        }

        void StopHighlight(GameObject go, Transform rayOrigin = null)
        {
            this.SetBlinkingHighlight(go, false);
            this.SetHighlight(go, false, rayOrigin, null, true);
        }

        void OnDraggingFeedForward(BaseHandle handle, HandleEventData eventData)
        {
            var rayOrigin = eventData.rayOrigin;
            var selection = TryGetSelection(rayOrigin);

            // we've just stopped hovering something, stop any highlights
            if (selection == null && m_CachedDropSelection != null)
            {
                StopHighlight(m_CachedDropSelection, rayOrigin);
                m_CachedDropSelection = null;
                m_LastDragSelectionChange = Time.time;
                RestoreOriginalSelectionMaterials();
            }
            else if (selection != null)
            {
                var time = Time.time;
                var previous = PreviouslyFoundResult(selection);

                if (selection != m_CachedDropSelection)
                {
                    StopHighlight(m_CachedDropSelection);
                    // if we've previously checked this object, indicate the result again
                    if (previous > 0f)
                    {
                        SetAssignableHighlight(selection, rayOrigin, true);
                        PreviewMaterialOnSelection(selection);
                    }
                    else if (previous < 0f)
                    {
                        SetAssignableHighlight(selection, rayOrigin, false);
                        RestoreOriginalSelectionMaterials();
                    }

                    m_CachedDropSelection = selection;
                    m_LastDragSelectionChange = time;
                    return;
                }

                if (previous == 0f)
                {
                    // avoid checking every object the selector passes over with a short delay
                    if (time - m_LastDragSelectionChange > k_CheckAssignDelayTime)
                    {
                        var assignable = CheckAssignable(selection);
                        SetAssignableHighlight(selection, rayOrigin, assignable);

                        if (assignable)
                            PreviewMaterialOnSelection(selection);
                    }
                }
            }
        }

        void PreviewMaterialOnSelection(GameObject selection)
        {
            if (data.type != "Material" || selection == null)
                return;

            m_SelectionRenderers.Clear();
            m_SelectionOriginalMaterials.Clear();

            selection.GetComponentsInChildren(m_SelectionRenderers);

            var material = (Material)data.asset;
            foreach (var renderer in m_SelectionRenderers)
            {
                m_SelectionOriginalMaterials.Add(renderer, renderer.sharedMaterial);
                renderer.sharedMaterial = material;
            }
        }

        void RestoreOriginalSelectionMaterials()
        {
            if (m_SelectionRenderers.Count < 1)
                return;

            foreach (var renderer in m_SelectionRenderers)
            {
                Material originalMaterial;
                if (m_SelectionOriginalMaterials.TryGetValue(renderer, out originalMaterial))
                    renderer.sharedMaterial = originalMaterial;
            }

            m_SelectionRenderers.Clear();
            m_SelectionOriginalMaterials.Clear();
        }

        bool CheckAssignable(GameObject go, bool checkChildren = false)
        {
            // if our asset type has a component dependency, we might want to add that
            // component for the user sometimes - filling in the blank on their intention
            // ex: AudioClips & AudioSources, VideoClips & VideoPlayers
            if (m_AssignmentDependencyTypes == null)
            {
                m_ObjectAssignmentChecks[go.GetInstanceID()] = Time.time;
                return true;
            }

            foreach (var t in m_AssignmentDependencyTypes)
            {
                if (AssetDropUtils.AutoFillTypes.Contains(t))
                {
                    m_ObjectAssignmentChecks[go.GetInstanceID()] = Time.time;
                    return true;
                }
            }

            if (!checkChildren)
            {
                foreach (Type t in m_AssignmentDependencyTypes)
                {
                    if (go.GetComponent(t) != null)
                    {
                        m_ObjectAssignmentChecks[go.GetInstanceID()] = Time.time;
                        return true;
                    }
                }
            }
            else
            {
                foreach (Type t in m_AssignmentDependencyTypes)
                {
                    if (go.GetComponentInChildren(t) != null)
                    {
                        m_ObjectAssignmentChecks[go.GetInstanceID()] = Time.time;
                        return true;
                    }

                }
            }

            m_ObjectAssignmentChecks[go.GetInstanceID()] = -Time.time;
            return false;
        }

        protected override void OnDragEnded(BaseHandle handle, HandleEventData eventData)
        {
            m_ObjectAssignmentChecks.Clear();
            StopHighlight(m_CachedDropSelection, eventData.rayOrigin);

            var gridItem = m_DragObject.GetComponent<AssetGridItem>();

            var rayOrigin = eventData.rayOrigin;
            this.RemoveRayVisibilitySettings(rayOrigin, this);

            if (!this.IsOverShoulder(eventData.rayOrigin))
            {
                var previewObjectTransform = gridItem.m_PreviewObjectTransform;
                if (previewObjectTransform)
                {
#if UNITY_EDITOR
                    UnityEditor.Undo.RegisterCreatedObjectUndo(previewObjectTransform.gameObject, "Place Scene Object");
#endif
                    this.PlaceSceneObject(previewObjectTransform, m_PreviewPrefabScale);
                }
                else
                {
                    HandleAssetDropByType(rayOrigin, gridItem);
                }
            }

            StartCoroutine(HideGrabbedObject(m_DragObject.gameObject, gridItem.m_Cube));
            base.OnDragEnded(handle, eventData);
        }

        void HandleAssetDropByType(Transform rayOrigin, AssetGridItem gridItem)
        {
            switch (data.type)
            {
                case AssetData.PrefabTypeString:
                case AssetData.ModelTypeString:
                    PlaceModelOrPrefab(gridItem.transform, data);
                    break;
                case "AnimationClip":
                    SelectAndPlace(rayOrigin, data, AssetDropUtils.AssignAnimationClipAction);
                    break;
                case "AudioClip":
                    SelectAndPlace(rayOrigin, data, AssetDropUtils.AudioClipAction);
                    break;
                case "VideoClip":
                    SelectAndPlace(rayOrigin, data, AssetDropUtils.VideoClipAction);
                    break;
                case "Font":
                    SelectAndPlace(rayOrigin, data, AssetDropUtils.AssignFontAction);
                    break;
                case "PhysicMaterial":
                    SelectAndPlace(rayOrigin, data, AssetDropUtils.AssignPhysicMaterialAction);
                    break;
                case "Material":
                    SelectAndPlace(rayOrigin, data, AssetDropUtils.AssignMaterialAction);
                    break;
                case "Script":
                    SelectAndPlace(rayOrigin, data, AssetDropUtils.AttachScriptAction);
                    break;
                case "Shader":
                    SelectAndPlace(rayOrigin, data, AssetDropUtils.AssignShaderAction);
                    break;
            }
        }

        void SelectAndPlace(Transform rayOrigin, AssetData data, Action<GameObject, AssetData> placeFunc)
        {
            var selection = TryGetSelection(rayOrigin);
            if (selection != null)
            {
                placeFunc.Invoke(selection, data);
                this.UpdateInspectors(selection, true);
            }
        }

        void PlaceModelOrPrefab(Transform itemTransform, AssetData data)
        {
#if UNITY_EDITOR
            var go = (GameObject)PrefabUtility.InstantiatePrefab(data.asset);
#else
            var go = (GameObject)Instantiate(data.asset);
#endif

            var transform = go.transform;
            transform.position = itemTransform.position;
            transform.rotation = itemTransform.rotation.ConstrainYaw();

            this.AddToSpatialHash(go);

#if UNITY_EDITOR
            UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Project Workspace");
#endif
        }

        GameObject TryGetSelection(Transform rayOrigin, bool includeRays)
        {
            var directSelections = this.GetDirectSelection();
            if (directSelections == null)
                return null;

            DirectSelectionData selectionData;
            if (!directSelections.TryGetValue(rayOrigin, out selectionData))
                return null;

            var selection = selectionData.gameObject;
            if (selection == null && includeRays)
                selection = this.GetFirstGameObject(rayOrigin);

            return selection;
        }

        GameObject TryGetSelection(Transform rayOrigin)
        {
            return TryGetSelection(rayOrigin, m_IncludeRaySelectForDrop);
        }

        void OnHoverStarted(BaseHandle handle, HandleEventData eventData)
        {
            if (m_PreviewObjectTransform && gameObject.activeInHierarchy)
            {
                if (m_AutoHidePreview)
                {
                    this.StopCoroutine(ref m_PreviewCoroutine);
                    m_PreviewCoroutine = StartCoroutine(AnimatePreview(false));
                }
                else
                {
                    m_PreviewObjectTransform.localScale = m_PreviewTargetScale * k_ScaleBump;
                }
            }

            base.OnHoverStart(handle, eventData);
            ShowGrabFeedback(this.RequestNodeFromRayOrigin(eventData.rayOrigin));
        }

        void OnHoverEnded(BaseHandle handle, HandleEventData eventData)
        {
            if (m_PreviewObjectTransform && gameObject.activeInHierarchy)
            {
                if (m_AutoHidePreview)
                {
                    this.StopCoroutine(ref m_PreviewCoroutine);
                    m_PreviewCoroutine = StartCoroutine(AnimatePreview(true));
                }
                else
                {
                    m_PreviewObjectTransform.localScale = m_PreviewTargetScale;
                }
            }

            HideGrabFeedback();
        }

        IEnumerator AnimatePreview(bool @out)
        {
            icon.SetActive(true);
            m_PreviewObjectTransform.gameObject.SetActive(true);

            var iconTransform = icon.transform;
            var currentIconScale = iconTransform.localScale;
            var targetIconScale = @out ? Vector3.one : Vector3.zero;

            var currentPreviewScale = m_PreviewObjectTransform.localScale;
            var targetPreviewScale = @out ? Vector3.zero : m_PreviewTargetScale;

            var startTime = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - startTime < k_PreviewDuration)
            {
                var t = (Time.realtimeSinceStartup - startTime) / k_PreviewDuration;

                icon.transform.localScale = Vector3.Lerp(currentIconScale, targetIconScale, t);
                m_PreviewObjectTransform.transform.localScale = Vector3.Lerp(currentPreviewScale, targetPreviewScale, t);
                yield return null;
            }

            m_PreviewObjectTransform.transform.localScale = targetPreviewScale;
            icon.transform.localScale = targetIconScale;

            m_PreviewObjectTransform.gameObject.SetActive(!@out);
            icon.SetActive(@out);

            m_PreviewCoroutine = null;
        }

        public void SetVisibility(bool visible, Action<AssetGridItem> callback = null)
        {
            this.StopCoroutine(ref m_VisibilityCoroutine);
            m_VisibilityCoroutine = StartCoroutine(AnimateVisibility(visible, callback));
        }

        IEnumerator AnimateVisibility(bool visible, Action<AssetGridItem> callback)
        {
            var currentTime = 0f;

            // Item should always be at a scale of zero before becoming visible
            if (visible)
                transform.localScale = Vector3.zero;

            var currentScale = transform.localScale;
            var targetScale = visible ? Vector3.one * scaleFactor : Vector3.zero;

            while (currentTime < k_TransitionDuration)
            {
                currentTime += Time.deltaTime;
                transform.localScale = Vector3.Lerp(currentScale, targetScale, currentTime / k_TransitionDuration);
                yield return null;
            }

            transform.localScale = targetScale;

            if (callback != null)
                callback(this);

            m_VisibilityCoroutine = null;
        }

        object GetDropObject(BaseHandle handle)
        {
            return data.asset;
        }

        void OnDestroy()
        {
            if (m_SphereMaterial)
                UnityObjectUtils.Destroy(m_SphereMaterial);

            UnityObjectUtils.Destroy(m_Cube.sharedMaterial);
        }

        // Animate the LocalScale of the asset towards a common/unified scale
        // used when the asset is magnetized/attached to the proxy, after grabbing it from the asset grid
        IEnumerator ShowGrabbedObject()
        {
            var currentLocalScale = m_DragObject.localScale;
            var currentPreviewOffset = Vector3.zero;
            var currentPreviewRotationOffset = Quaternion.identity;

            if (m_PreviewObjectClone)
                currentPreviewOffset = m_PreviewObjectClone.localPosition;

            var currentTime = 0f;
            var currentVelocity = 0f;
            const float kDuration = 1f;

            var targetScale = Vector3.one * k_IconPreviewScale;
            var pivotOffset = Vector3.zero;
            var rotationOffset = Quaternion.AngleAxis(30, Vector3.right);
            if (m_PreviewObjectClone)
            {
                var viewerScale = this.GetViewerScale();
                var maxComponent = m_PreviewBounds.size.MaxComponent() / viewerScale;
                targetScale = Vector3.one * maxComponent;

                // Object will preview at the same size when grabbed
                var previewExtents = m_PreviewBounds.extents / viewerScale;
                pivotOffset = m_PreviewPivotOffset / viewerScale;

                // If bounds are greater than offset, set to bounds
                if (previewExtents.y > pivotOffset.y)
                    pivotOffset.y = previewExtents.y;

                if (previewExtents.z > pivotOffset.z)
                    pivotOffset.z = previewExtents.z;

                if (maxComponent < k_MinPreviewScale)
                {
                    // Object will be preview at the minimum scale
                    targetScale = Vector3.one * k_MinPreviewScale;
                    pivotOffset = pivotOffset * scaleFactor + (Vector3.up + Vector3.forward) * 0.5f * k_MinPreviewScale;
                }

                if (maxComponent > k_MaxPreviewScale)
                {
                    // Object will be preview at the maximum scale
                    targetScale = Vector3.one * k_MaxPreviewScale;
                    pivotOffset = pivotOffset * scaleFactor + (Vector3.up + Vector3.forward) * 0.5f * k_MaxPreviewScale;
                }
            }

            while (currentTime < kDuration - 0.05f)
            {
                if (m_DragObject == null)
                    yield break; // Exit coroutine if m_GrabbedObject is destroyed before the loop is finished

                currentTime = MathUtilsExt.SmoothDamp(currentTime, kDuration, ref currentVelocity, 0.5f, Mathf.Infinity, Time.deltaTime);
                m_DragObject.localScale = Vector3.Lerp(currentLocalScale, targetScale, currentTime);

                if (m_PreviewObjectClone)
                {
                    m_PreviewObjectClone.localPosition = Vector3.Lerp(currentPreviewOffset, pivotOffset, currentTime);
                    m_PreviewObjectClone.localRotation = Quaternion.Lerp(currentPreviewRotationOffset, rotationOffset, currentTime); // Compensate for preview origin rotation
                }

                yield return null;
            }

            m_DragObject.localScale = targetScale;
        }

        static IEnumerator HideGrabbedObject(GameObject itemToHide, Renderer cubeRenderer)
        {
            var itemTransform = itemToHide.transform;
            var currentScale = itemTransform.localScale;
            var targetScale = Vector3.zero;
            var transitionAmount = Time.deltaTime;
            var transitionAddMultiplier = 6;
            while (transitionAmount < 1)
            {
                itemTransform.localScale = Vector3.Lerp(currentScale, targetScale, transitionAmount);
                transitionAmount += Time.deltaTime * transitionAddMultiplier;
                yield return null;
            }

            cubeRenderer.sharedMaterial = null; // Drop material so it won't be destroyed (shared with cube in list)
            UnityObjectUtils.Destroy(itemToHide);
        }

        void ShowGrabFeedback(Node node)
        {
            var request = this.GetFeedbackRequestObject<ProxyFeedbackRequest>(this);
            request.control = VRInputDevice.VRControl.Trigger1;
            request.node = node;
            request.tooltipText = "Grab";
            this.AddFeedbackRequest(request);
        }

        void HideGrabFeedback()
        {
            this.ClearFeedbackRequests(this);
        }
    }
}
