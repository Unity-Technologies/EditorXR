using System;
using System.Collections;
using TMPro;
using Unity.Labs.EditorXR.Interfaces;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Handles;
using UnityEditor.Experimental.EditorVR.Helpers;
using UnityEditor.Experimental.EditorVR.Proxies;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
    class PolyGridItem : DraggableListItem<PolyGridAsset, string>, IUsesPlaceSceneObject, IUsesSpatialHash,
        IUsesViewerBody, IUsesRayVisibilitySettings, IUsesRequestFeedback, IUsesGrouping, IUsesControlHaptics
    {
        const float k_PreviewDuration = 0.1f;
        const float k_MinPreviewScale = 0.01f;
        const float k_IconPreviewScale = 0.1f;
        const float k_MaxPreviewScale = 0.2f;
        const float k_TransitionDuration = 0.1f;
        const float k_ScaleBump = 0.1f;
        const float k_ThumbnailScaleBump = -0.1f;
        const float k_ThumbnailOffsetBump = k_ThumbnailScaleBump * -0.5f;
        const float k_ImportingScaleBump = 0.375f;

        const float k_InitializeDelay = 0.5f; // Delay initialization for fast scrolling

        const int k_AutoHidePreviewComplexity = 10000;

#pragma warning disable 649
        [SerializeField]
        TextMeshProUGUI m_Text;

        [SerializeField]
        BaseHandle m_Handle;

        [SerializeField]
        Image m_TextPanel;

        [SerializeField]
        GameObject m_Icon;

        [SerializeField]
        Color m_ImportingTargetColor;

        [HideInInspector]
        [SerializeField] // Serialized so that this remains set after cloning
        Transform m_PreviewObjectTransform;
#pragma warning restore 649

        bool m_AutoHidePreview;
        Vector3 m_PreviewPrefabScale;
        Vector3 m_PreviewTargetScale;
        Vector3 m_PreviewPivotOffset;
        Bounds m_PreviewBounds;
        Transform m_PreviewObjectClone;
        Material m_IconMaterial;
        Vector3 m_IconScale;

        bool m_Hovered;
        bool m_WasHovered;
        float m_HoverTime;
        float m_HoverLerpStart;
        float m_HoverLerp;

        bool m_Importing;
        bool m_WasImporting;
        float m_ImportingTime;
        float m_ImportingStartScale;
        float m_ImportingScale;

        Color m_ImportingDefaultColor;
        Color m_ImportingStartColor;
        Color m_ImportingColor;

        Coroutine m_VisibilityCoroutine;

        float m_SetupTime = float.MaxValue;

        public float scaleFactor { private get; set; }

#if !FI_AUTOFILL
        IProvidesSpatialHash IFunctionalitySubscriber<IProvidesSpatialHash>.provider { get; set; }
        IProvidesPlaceSceneObject IFunctionalitySubscriber<IProvidesPlaceSceneObject>.provider { get; set; }
        IProvidesViewerBody IFunctionalitySubscriber<IProvidesViewerBody>.provider { get; set; }
        IProvidesGrouping IFunctionalitySubscriber<IProvidesGrouping>.provider { get; set; }
        IProvidesRequestFeedback IFunctionalitySubscriber<IProvidesRequestFeedback>.provider { get; set; }
        IProvidesRayVisibilitySettings IFunctionalitySubscriber<IProvidesRayVisibilitySettings>.provider { get; set; }
        IProvidesControlHaptics IFunctionalitySubscriber<IProvidesControlHaptics>.provider { get; set; }
#endif

        // Local method use only -- created here to reduce garbage collection
        Action<float> m_CompleteHoverTransition;
        Action<float> m_SetThumbnailScale;
        Action<float> m_SetImportingScale;
        Action<Color> m_SetImportingColor;

        void Awake()
        {
            m_CompleteHoverTransition = CompleteHoverTransition;
            m_SetThumbnailScale = SetThumbnailScale;
            m_SetImportingScale = SetImportingScale;
            m_SetImportingColor = SetImportingColor;
        }

        public override void Setup(PolyGridAsset listData, bool firstTime)
        {
            base.Setup(listData, firstTime);

            // First time setup
            if (firstTime)
            {
                m_IconScale = m_Icon.transform.localScale;

                m_ImportingDefaultColor = m_TextPanel.color;

                m_Handle.dragStarted += OnDragStarted;
                m_Handle.dragging += OnDragging;
                m_Handle.dragEnded += OnDragEnded;

                m_Handle.hoverStarted += OnHoverStarted;
                m_Handle.hoverEnded += OnHoverEnded;

                m_IconMaterial = MaterialUtils.GetMaterialClone(m_Icon.GetComponent<Renderer>());
            }

            m_Hovered = false;
            m_WasHovered = false;
            m_HoverLerpStart = 1f;
            m_HoverLerp = 1f;

            m_Importing = false;
            m_WasImporting = false;
            m_ImportingStartScale = m_IconScale.y;
            m_ImportingScale = m_ImportingStartScale;

            m_ImportingStartColor = m_ImportingDefaultColor;
            m_ImportingColor = m_ImportingStartColor;

            m_VisibilityCoroutine = null;
            m_Icon.transform.localScale = m_IconScale;
            m_IconMaterial.mainTexture = null;

            if (m_PreviewObjectTransform)
                UnityObjectUtils.Destroy(m_PreviewObjectTransform.gameObject);

            m_SetupTime = Time.time;
            UpdateRepresentation();
        }

        void OnModelImportCompleted(PolyGridAsset gridAsset, GameObject prefab)
        {
            m_Importing = false;
            UpdateRepresentation();
        }

        void OnThumbnailImportCompleted(PolyGridAsset gridAsset, Texture2D thumbnail)
        {
            UpdateRepresentation();
        }

        void UpdateRepresentation()
        {
            if (!m_Icon) // Prevent MissingReferenceException if shutdown occurs while fetching thumbnails
                return;

            m_Text.text = data.asset.displayName;

            if (!m_PreviewObjectTransform && data.prefab)
            {
                m_Icon.SetActive(false);
                InstantiatePreview();
            }

            m_Icon.SetActive(!m_PreviewObjectTransform || m_AutoHidePreview);

            if (m_IconMaterial.mainTexture == null && data.thumbnail)
                m_IconMaterial.mainTexture = data.thumbnail;
        }

        public void UpdateTransforms(float scale)
        {
            if (Time.time - m_SetupTime > k_InitializeDelay)
            {
                m_SetupTime = float.MaxValue;

                // If this AssetData hasn't started fetching its asset yet, do so now
                if (!data.initialized)
                    data.Initialize();

#if INCLUDE_POLY_TOOLKIT
                data.modelImportCompleted += OnModelImportCompleted;
                data.thumbnailImportCompleted += OnThumbnailImportCompleted;
#endif
            }

            // Don't scale the item while changing visibility because this would conflict with AnimateVisibility
            if (m_VisibilityCoroutine != null)
                return;

            var time = Time.time;

            TransitionUtils.AnimateProperty(time, m_Hovered, ref m_WasHovered, ref m_HoverTime, ref m_HoverLerp,
                ref m_HoverLerpStart, 0f, 1f, k_PreviewDuration, Mathf.Approximately, TransitionUtils.GetPercentage,
                Mathf.Lerp, m_SetThumbnailScale, true, m_CompleteHoverTransition);

            TransitionUtils.AnimateProperty(time, m_Importing, ref m_WasImporting, ref m_ImportingTime,
                ref m_ImportingScale, ref m_ImportingStartScale, m_IconScale.y, k_ImportingScaleBump, k_PreviewDuration,
                Mathf.Approximately, TransitionUtils.GetPercentage, Mathf.Lerp, m_SetImportingScale, false);

            TransitionUtils.AnimateProperty(time, m_Importing, ref m_WasImporting, ref m_ImportingTime,
                ref m_ImportingColor, ref m_ImportingStartColor, m_ImportingDefaultColor, m_ImportingTargetColor,
                k_PreviewDuration, TransitionUtils.Approximately, TransitionUtils.GetPercentage, Color.Lerp,
                m_SetImportingColor);

            scaleFactor = scale;

            transform.localScale = Vector3.one * scale;

            m_TextPanel.transform.localRotation = CameraUtils.LocalRotateTowardCamera(transform.parent);
        }

        void SetThumbnailScale(float lerp)
        {
            if (m_PreviewObjectTransform)
            {
                if (m_AutoHidePreview)
                {
                    m_PreviewObjectTransform.localScale = lerp * Vector3.one;
                    m_Icon.transform.localScale = (1 - lerp) * m_IconScale;
                }
                else
                {
                    m_PreviewObjectTransform.localScale = Vector3.one + lerp * Vector3.one * k_ScaleBump;
                }

                return;
            }

            m_IconMaterial.mainTextureOffset = lerp * Vector3.one * k_ThumbnailOffsetBump;
            m_IconMaterial.mainTextureScale = Vector3.one + lerp * Vector3.one * k_ThumbnailScaleBump;
        }

        void CompleteHoverTransition(float lerp)
        {
            if (m_PreviewObjectTransform && m_AutoHidePreview)
            {
                m_PreviewObjectTransform.gameObject.SetActive(lerp != 0);
                m_Icon.SetActive(lerp == 0);
            }
        }

        void SetImportingScale(float scale)
        {
            var transform = m_Icon.transform;
            var localScale = transform.localScale;
            localScale.y = scale;
            transform.localScale = localScale;
        }

        void SetImportingColor(Color color)
        {
            m_TextPanel.color = color;
        }

        void InstantiatePreview()
        {
            if (!data.prefab)
                return;

            var previewObject = Instantiate(data.prefab);
            previewObject.SetActive(true);
            m_PreviewObjectTransform = previewObject.transform;

            m_PreviewObjectTransform.position = Vector3.zero;
            m_PreviewObjectTransform.rotation = Quaternion.identity;

            m_PreviewPrefabScale = m_PreviewObjectTransform.localScale;

            // Normalize total scale to 1
            m_PreviewBounds = BoundsUtils.GetBounds(m_PreviewObjectTransform);

            // Don't show a preview if there are no renderers
            if (m_PreviewBounds.size == Vector3.zero)
            {
                UnityObjectUtils.Destroy(previewObject);
                return;
            }

            m_PreviewPivotOffset = m_PreviewObjectTransform.position - m_PreviewBounds.center;
            m_PreviewObjectTransform.SetParent(transform, false);

            var maxComponent = m_PreviewBounds.size.MaxComponent();
            var scaleFactor = 1 / maxComponent;
            m_PreviewTargetScale = m_PreviewPrefabScale * scaleFactor;
            m_PreviewObjectTransform.localPosition = m_PreviewPivotOffset * scaleFactor + Vector3.up * 0.5f;

            var complexity = data.complexity;
            // Auto hide previews over a smaller vert count
            if (complexity > k_AutoHidePreviewComplexity)
            {
                m_AutoHidePreview = true;
                m_PreviewObjectTransform.localScale = Vector3.zero;
            }
            else
            {
                m_PreviewObjectTransform.localScale = Vector3.one;
                m_Icon.SetActive(false);
            }
        }

        protected override void OnDragStarted(BaseHandle handle, HandleEventData eventData)
        {
            if (data.prefab)
            {
                base.OnDragStarted(handle, eventData);

                var rayOrigin = eventData.rayOrigin;
                this.AddRayVisibilitySettings(rayOrigin, this, false, true);

                var clone = Instantiate(gameObject, transform.position, transform.rotation, transform.parent);
                var cloneItem = clone.GetComponent<PolyGridItem>();

                if (cloneItem.m_PreviewObjectTransform)
                {
                    m_PreviewObjectClone = cloneItem.m_PreviewObjectTransform;
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

                StartCoroutine(ShowGrabbedObject());
            }
        }

        protected override void OnDragEnded(BaseHandle handle, HandleEventData eventData)
        {
            if (data.prefab)
            {
                var gridItem = m_DragObject.GetComponent<PolyGridItem>();

                var rayOrigin = eventData.rayOrigin;
                this.RemoveRayVisibilitySettings(rayOrigin, this);

                if (!this.IsOverShoulder(eventData.rayOrigin))
                {
                    var previewObject = gridItem.m_PreviewObjectTransform;
                    if (previewObject)
                    {
                        this.MakeGroup(previewObject.gameObject);
                        this.PlaceSceneObject(previewObject, m_PreviewPrefabScale);
                    }
                }

                StartCoroutine(HideGrabbedObject(m_DragObject.gameObject));
            }
            else
            {
                data.ImportModel();
                m_Text.text = "Importing...";
                m_Importing = true;
            }

            base.OnDragEnded(handle, eventData);
        }

        void OnHoverStarted(BaseHandle handle, HandleEventData eventData)
        {
            base.OnHoverStart(handle, eventData);

            m_Hovered = true;

            ShowGrabFeedback(this.RequestNodeFromRayOrigin(eventData.rayOrigin));
        }

        void OnHoverEnded(BaseHandle handle, HandleEventData eventData)
        {
            base.OnHoverEnd(handle, eventData);

            m_Hovered = false;

            HideGrabFeedback();
        }

        public void SetVisibility(bool visible, Action<PolyGridItem> callback = null)
        {
            this.RestartCoroutine(ref m_VisibilityCoroutine, AnimateVisibility(visible, callback));
        }

        IEnumerator AnimateVisibility(bool visible, Action<PolyGridItem> callback)
        {
            var currentTime = 0f;

            // Item should always be at a scale of zero before becoming visible
            if (visible)
            {
                transform.localScale = Vector3.zero;
            }
#if INCLUDE_POLY_TOOLKIT
            else
            {
                data.modelImportCompleted -= OnModelImportCompleted;
                data.thumbnailImportCompleted -= OnThumbnailImportCompleted;
            }
#endif

            var currentScale = transform.localScale;
            var targetScale = visible ? m_IconScale * scaleFactor : Vector3.zero;

            while (currentTime < k_TransitionDuration)
            {
                currentTime += Time.deltaTime;
                transform.localScale = Vector3.Lerp(currentScale, targetScale,
                    MathUtilsExt.SmoothInOutLerpFloat(currentTime / k_TransitionDuration));
                yield return null;
            }

            transform.localScale = targetScale;

            if (callback != null)
                callback(this);

            m_VisibilityCoroutine = null;
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
            const float duration = 1f;

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

            while (currentTime < duration)
            {
                if (m_DragObject == null)
                    yield break; // Exit coroutine if m_GrabbedObject is destroyed before the loop is finished

                currentTime += Time.deltaTime;
                m_DragObject.localScale = Vector3.Lerp(currentLocalScale, targetScale,
                    MathUtilsExt.SmoothInOutLerpFloat(currentTime / duration));

                if (m_PreviewObjectClone)
                {
                    m_PreviewObjectClone.localPosition = Vector3.Lerp(currentPreviewOffset, pivotOffset, currentTime);
                    m_PreviewObjectClone.localRotation = Quaternion.Lerp(currentPreviewRotationOffset, rotationOffset, currentTime); // Compensate for preview origin rotation
                }

                yield return null;
            }

            m_DragObject.localScale = targetScale;
            //No need to hard-set the preview object position/rotation because they will set in OnDragging in parent class
        }

        static IEnumerator HideGrabbedObject(GameObject itemToHide)
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
