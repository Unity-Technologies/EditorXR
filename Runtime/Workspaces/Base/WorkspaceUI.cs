using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Labs.EditorXR.Extensions;
using Unity.Labs.EditorXR.Handles;
using Unity.Labs.EditorXR.Helpers;
using Unity.Labs.EditorXR.Interfaces;
using Unity.Labs.EditorXR.Proxies;
using Unity.Labs.EditorXR.Utilities;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEngine;
using UnityEngine.InputNew;
using UnityEngine.UI;

namespace Unity.Labs.EditorXR.Workspaces
{
    sealed class WorkspaceUI : MonoBehaviour, IUsesStencilRef, IUsesViewerScale, IUsesPointer, IUsesRequestFeedback
    {
        [Flags]
        enum ResizeDirection
        {
            Front = 1,
            Back = 2,
            Left = 4,
            Right = 8
        }

        class DragState
        {
            public Transform rayOrigin { get; private set; }
            bool m_Resizing;
            Vector3 m_PositionOffset;
            Quaternion m_RotationOffset;
            WorkspaceUI m_WorkspaceUI;
            Vector3 m_DragStart;
            Vector3 m_PositionStart;
            Vector3 m_BoundsSizeStart;
            ResizeDirection m_Direction;

            public DragState(WorkspaceUI workspaceUI, Transform rayOrigin, bool resizing)
            {
                m_WorkspaceUI = workspaceUI;
                m_Resizing = resizing;
                this.rayOrigin = rayOrigin;

                if (resizing)
                {
                    var pointerPosition = m_WorkspaceUI.GetPointerPosition(rayOrigin);
                    m_DragStart = pointerPosition;
                    m_PositionStart = workspaceUI.transform.parent.position;
                    m_BoundsSizeStart = workspaceUI.bounds.size;
                    var localPosition = m_WorkspaceUI.transform.InverseTransformPoint(pointerPosition);
                    m_Direction = m_WorkspaceUI.GetResizeDirectionForLocalPosition(localPosition);
                }
                else
                {
                    MathUtilsExt.GetTransformOffset(rayOrigin, m_WorkspaceUI.transform.parent, out m_PositionOffset, out m_RotationOffset);
                }
            }

            public void OnDragging()
            {
                if (m_Resizing)
                {
                    var viewerScale = m_WorkspaceUI.GetViewerScale();
                    var pointerPosition = m_WorkspaceUI.GetPointerPosition(rayOrigin);
                    var dragVector = (pointerPosition - m_DragStart) / viewerScale;
                    var bounds = m_WorkspaceUI.bounds;
                    var transform = m_WorkspaceUI.transform;

                    var positionOffsetForward = Vector3.Dot(dragVector, transform.forward) * 0.5f;
                    var positionOffsetRight = Vector3.Dot(dragVector, transform.right) * 0.5f;

                    switch (m_Direction)
                    {
                        default:
                            bounds.size = m_BoundsSizeStart + Vector3.back * Vector3.Dot(dragVector, transform.forward);
                            positionOffsetRight = 0;
                            break;
                        case ResizeDirection.Back:
                            bounds.size = m_BoundsSizeStart + Vector3.forward * Vector3.Dot(dragVector, transform.forward);
                            positionOffsetRight = 0;
                            break;
                        case ResizeDirection.Left:
                            bounds.size = m_BoundsSizeStart + Vector3.left * Vector3.Dot(dragVector, transform.right);
                            positionOffsetForward = 0;
                            break;
                        case ResizeDirection.Right:
                            bounds.size = m_BoundsSizeStart + Vector3.right * Vector3.Dot(dragVector, transform.right);
                            positionOffsetForward = 0;
                            break;
                        case ResizeDirection.Front | ResizeDirection.Left:
                            bounds.size = m_BoundsSizeStart + Vector3.left * Vector3.Dot(dragVector, transform.right)
                                + Vector3.back * Vector3.Dot(dragVector, transform.forward);
                            break;
                        case ResizeDirection.Front | ResizeDirection.Right:
                            bounds.size = m_BoundsSizeStart + Vector3.right * Vector3.Dot(dragVector, transform.right)
                                + Vector3.back * Vector3.Dot(dragVector, transform.forward);
                            break;
                        case ResizeDirection.Back | ResizeDirection.Left:
                            bounds.size = m_BoundsSizeStart + Vector3.left * Vector3.Dot(dragVector, transform.right)
                                + Vector3.forward * Vector3.Dot(dragVector, transform.forward);
                            break;
                        case ResizeDirection.Back | ResizeDirection.Right:
                            bounds.size = m_BoundsSizeStart + Vector3.right * Vector3.Dot(dragVector, transform.right)
                                + Vector3.forward * Vector3.Dot(dragVector, transform.forward);
                            break;
                    }

                    if (m_WorkspaceUI.resize != null)
                        m_WorkspaceUI.resize(bounds);

                    var currentExtents = m_WorkspaceUI.bounds.extents;
                    var extents = bounds.extents;
                    var absRight = Mathf.Abs(positionOffsetRight);
                    var absForward = Mathf.Abs(positionOffsetForward);
                    var positionOffset = (absRight - (currentExtents.x - extents.x)) * Mathf.Sign(positionOffsetRight) * transform.right
                        + (absForward - (currentExtents.z - extents.z)) * Mathf.Sign(positionOffsetForward) * transform.forward;

                    m_WorkspaceUI.transform.parent.position = m_PositionStart + positionOffset * viewerScale;
                    m_WorkspaceUI.OnResizing(rayOrigin);
                }
                else
                {
                    MathUtilsExt.SetTransformOffset(rayOrigin, m_WorkspaceUI.transform.parent, m_PositionOffset, m_RotationOffset);
                    m_WorkspaceUI.OnMoving(rayOrigin);
                }
            }
        }

        const int k_AngledFaceBlendShapeIndex = 2;
        const int k_ThinFrameBlendShapeIndex = 3;
        const string k_MaterialStencilRef = "_StencilRef";

        const float k_ResizeIconCrossfadeDuration = 0.1f;
        const float k_ResizeIconSmoothFollow = 10f;

        const float k_FrontFrameZOffset = 0.088f;

        static readonly Vector3 k_BaseFrontPanelRotation = Vector3.zero;
        static readonly Vector3 k_MaxFrontPanelRotation = new Vector3(90f, 0f, 0f);

#pragma warning disable 649
        [SerializeField]
        Transform m_SceneContainer;

        [SerializeField]
        RectTransform m_FrontPanel;

        [SerializeField]
        BaseHandle[] m_Handles;

        [SerializeField]
        Image[] m_ResizeIcons;

        [SerializeField]
        Transform m_FrontLeftHandle;

        [SerializeField]
        Transform m_FrontLeftCornerHandle;

        [SerializeField]
        Transform m_FrontRightHandle;

        [SerializeField]
        Transform m_FrontRightCornerHandle;

        [SerializeField]
        Transform m_BottomFrontHandle;

        [SerializeField]
        Transform m_TopFaceContainer;

        [SerializeField]
        WorkspaceHighlight m_TopHighlight;

        [SerializeField]
        SkinnedMeshRenderer m_Frame;

        [SerializeField]
        Transform m_FrameFrontFaceTransform;

        [SerializeField]
        Transform m_FrameFrontFaceHighlightTransform;

        [SerializeField]
        Transform m_TopPanelDividerTransform;

        [SerializeField]
        RectTransform m_UIContentContainer;

        [SerializeField]
        Image m_FrontResizeIcon;

        [SerializeField]
        Image m_RightResizeIcon;

        [SerializeField]
        Image m_LeftResizeIcon;

        [SerializeField]
        Image m_BackResizeIcon;

        [SerializeField]
        Image m_FrontLeftResizeIcon;

        [SerializeField]
        Image m_FrontRightResizeIcon;

        [SerializeField]
        Image m_BackLeftResizeIcon;

        [SerializeField]
        Image m_BackRightResizeIcon;

        [SerializeField]
        Transform m_TopHighlightContainer;

        [SerializeField]
        RectTransform m_HandleRectTransform;

        [SerializeField]
        RectTransform m_ResizeIconRectTransform;

        [SerializeField]
        WorkspaceHighlight m_FrontHighlight;

        [SerializeField]
        float m_FrameHandleSize = 0.03f;

        [SerializeField]
        float m_FrontFrameHandleSize = 0.01f;

        [SerializeField]
        float m_FrameHeight = 0.09275f;

        [SerializeField]
        float m_ResizeHandleMargin = 0.01f;

        [SerializeField]
        float m_ResizeCornerSize = 0.05f;

        [SerializeField]
        bool m_DynamicFaceAdjustment = true;

        [SerializeField]
        WorkspaceButton m_CloseButton;

        [SerializeField]
        WorkspaceButton m_ResizeButton;
#pragma warning restore 649

        BoxCollider m_FrameCollider;
        Bounds m_Bounds;
        float? m_TopPanelDividerOffset;

        readonly BindingDictionary m_Controls = new BindingDictionary();
        readonly List<ProxyFeedbackRequest> m_LeftMoveFeedback = new List<ProxyFeedbackRequest>();
        readonly List<ProxyFeedbackRequest> m_RightMoveFeedback = new List<ProxyFeedbackRequest>();
        readonly List<ProxyFeedbackRequest> m_LeftResizeFeedback = new List<ProxyFeedbackRequest>();
        readonly List<ProxyFeedbackRequest> m_RightResizeFeedback = new List<ProxyFeedbackRequest>();

        // Cached for optimization
        float m_PreviousXRotation;
        Coroutine m_FrameThicknessCoroutine;
        Coroutine m_TopFaceVisibleCoroutine;
        Material m_TopFaceMaterial;
        Material m_FrontFaceMaterial;

        float m_LerpAmount;
        float m_FrontZOffset;

        DragState m_DragState;

        readonly List<Transform> m_HovereringRayOrigins = new List<Transform>();
        readonly Dictionary<Transform, Image> m_LastResizeIcons = new Dictionary<Transform, Image>();

        public bool highlightsVisible
        {
            set
            {
                if (m_TopHighlight.visible == value && m_FrontHighlight.visible == value)
                    return;

                m_TopHighlight.visible = value;
                m_FrontHighlight.visible = value;

                if (value)
                    IncreaseFrameThickness();
                else
                    ResetFrameThickness();
            }
        }

        public bool frontHighlightVisible
        {
            set
            {
                if (m_FrontHighlight.visible == value)
                    return;

                m_FrontHighlight.visible = value;

                if (value)
                    IncreaseFrameThickness();
                else
                    ResetFrameThickness();
            }
        }

        public bool amplifyTopHighlight
        {
            set
            {
                this.StopCoroutine(ref m_TopFaceVisibleCoroutine);
                m_TopFaceVisibleCoroutine = value ? StartCoroutine(HideTopFace()) : StartCoroutine(ShowTopFace());
            }
        }

        /// <summary>
        /// (-1 to 1) ranged value that controls the separator mask's X-offset placement
        /// A value of zero will leave the mask in the center of the workspace
        /// </summary>
        public float topPanelDividerOffset
        {
            set
            {
                m_TopPanelDividerOffset = value;
                m_TopPanelDividerTransform.gameObject.SetActive(true);
            }
        }

        public Transform topFaceContainer
        {
            get { return m_TopFaceContainer; }
            set { m_TopFaceContainer = value; }
        }

        public bool dynamicFaceAdjustment
        {
            get { return m_DynamicFaceAdjustment; }
            set { m_DynamicFaceAdjustment = value; }
        }

        public bool preventResize
        {
            set
            {
                foreach (var handle in m_Handles)
                {
                    handle.gameObject.SetActive(!value);
                }

                m_ResizeButton.gameObject.SetActive(!value);
            }
        }

        public Bounds bounds
        {
            get { return m_Bounds; }
            set
            {
                m_Bounds = value;

                m_Bounds.center = Vector3.down * m_FrameHeight * 0.5f;

                var extents = m_Bounds.extents;
                var size = m_Bounds.size;
                size.y = m_FrameHeight + m_FrameHandleSize;
                m_Bounds.size = size;

                const float kWidthMultiplier = 0.96154f;
                const float kDepthMultiplier = 0.99383f;
                const float kWidthOffset = -0.156f;
                const float kDepthOffset = -0.0318f;
                const float kDepthCompensation = -0.008f;

                var width = size.x;
                var depth = size.z;
                var faceWidth = width - Workspace.FaceMargin;
                var faceDepth = depth - Workspace.FaceMargin;

                m_Frame.SetBlendShapeWeight(0, width * kWidthMultiplier + kWidthOffset);
                m_Frame.SetBlendShapeWeight(1, depth * kDepthMultiplier + kDepthOffset + kDepthCompensation * m_LerpAmount);

                // Resize content container
                m_UIContentContainer.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, faceWidth);
                var localPosition = m_UIContentContainer.localPosition;
                localPosition.z = -extents.z;
                m_UIContentContainer.localPosition = localPosition;

                // Resize front panel
                m_FrameFrontFaceTransform.localScale = new Vector3(faceWidth, 1f, 1f);
                const float kFrontFaceHighlightMargin = 0.0008f;
                m_FrameFrontFaceHighlightTransform.localScale = new Vector3(faceWidth + kFrontFaceHighlightMargin, 1f, 1f);

                // Position the separator mask if enabled
                if (m_TopPanelDividerOffset != null)
                {
                    m_TopPanelDividerTransform.localPosition = new Vector3(faceWidth * (m_TopPanelDividerOffset.Value - 0.5f), 0f, 0f);
                    m_TopPanelDividerTransform.localScale = new Vector3(1f, 1f, faceDepth + Workspace.HighlightMargin);
                }

                // Scale the Top Face and the Top Face Highlight
                const float kHighlightMargin = 0.0005f;
                m_TopHighlightContainer.localScale = new Vector3(faceWidth + kHighlightMargin, 1f, faceDepth + kHighlightMargin);
                m_TopFaceContainer.localScale = new Vector3(faceWidth, 1f, faceDepth);

                var frameBounds = adjustedBounds;
                m_FrameCollider.size = frameBounds.size;
                m_FrameCollider.center = frameBounds.center;

                AdjustHandlesAndIcons();
            }
        }

        public Bounds adjustedBounds
        {
            get
            {
                var adjustedBounds = bounds;
                adjustedBounds.size += Vector3.forward * m_FrontZOffset;
                adjustedBounds.center += Vector3.back * m_FrontZOffset * 0.5f;
                return adjustedBounds;
            }
        }

        public event Action<Transform> buttonHovered;
        public event Action<Transform> closeClicked;
        public event Action<Transform> resetSizeClicked;
        public event Action<Transform> resizing;
        public event Action<Transform> moving;
        public event Action<Transform> hoveringFrame;

        public Transform sceneContainer { get { return m_SceneContainer; } }
        public RectTransform frontPanel { get { return m_FrontPanel; } }
        public WorkspaceHighlight topHighlight { get { return m_TopHighlight; } }

        public byte stencilRef { get; set; }

        public Transform leftRayOrigin { private get; set; }
        public Transform rightRayOrigin { private get; set; }

        public event Action<Bounds> resize;

#if !FI_AUTOFILL
        IProvidesViewerScale IFunctionalitySubscriber<IProvidesViewerScale>.provider { get; set; }
        IProvidesRequestFeedback IFunctionalitySubscriber<IProvidesRequestFeedback>.provider { get; set; }
#endif

        void Awake()
        {
            foreach (var icon in m_ResizeIcons)
            {
                icon.CrossFadeAlpha(0f, 0f, true);
            }

            m_Frame.SetBlendShapeWeight(k_ThinFrameBlendShapeIndex, 50f); // Set default frame thickness to be in middle for a thinner initial frame

            if (m_TopPanelDividerOffset == null)
                m_TopPanelDividerTransform.gameObject.SetActive(false);

            foreach (var handle in m_Handles)
            {
                handle.hoverStarted += OnHandleHoverStarted;
                handle.hoverEnded += OnHandleHoverEnded;
            }

            m_CloseButton.clicked += OnCloseClicked;
            m_CloseButton.hovered += OnButtonHovered;
            m_ResizeButton.clicked += OnResetSizeClicked;
            m_ResizeButton.hovered += OnButtonHovered;

            m_FrameCollider = transform.parent.gameObject.AddComponent<BoxCollider>();
        }

        IEnumerator Start()
        {
            const string kShaderBlur = "_Blur";
            const string kShaderAlpha = "_Alpha";
            const string kShaderVerticalOffset = "_VerticalOffset";
            const float kTargetDuration = 1.25f;

            m_TopFaceMaterial = MaterialUtils.GetMaterialClone(m_TopFaceContainer.GetComponentInChildren<MeshRenderer>());
            m_TopFaceMaterial.SetFloat("_Alpha", 1f);
            m_TopFaceMaterial.SetInt(k_MaterialStencilRef, stencilRef);

            m_FrontFaceMaterial = MaterialUtils.GetMaterialClone(m_FrameFrontFaceTransform.GetComponentInChildren<MeshRenderer>());
            m_FrontFaceMaterial.SetInt(k_MaterialStencilRef, stencilRef);

            var originalBlurAmount = m_TopFaceMaterial.GetFloat("_Blur");
            var currentBlurAmount = 10f; // also the maximum blur amount
            var currentDuration = 0f;
            var currentVelocity = 0f;

            m_TopFaceMaterial.SetFloat(kShaderBlur, currentBlurAmount);
            m_TopFaceMaterial.SetFloat(kShaderVerticalOffset, 1f); // increase the blur sample offset to amplify the effect
            m_TopFaceMaterial.SetFloat(kShaderAlpha, 0.5f); // set partially transparent

            while (currentDuration < kTargetDuration)
            {
                currentDuration += Time.deltaTime;
                currentBlurAmount = MathUtilsExt.SmoothDamp(currentBlurAmount, originalBlurAmount, ref currentVelocity, kTargetDuration, Mathf.Infinity, Time.deltaTime);
                m_TopFaceMaterial.SetFloat(kShaderBlur, currentBlurAmount);

                var percentageComplete = currentDuration / kTargetDuration;
                m_TopFaceMaterial.SetFloat(kShaderVerticalOffset, 1 - percentageComplete); // lerp back towards an offset of zero
                m_TopFaceMaterial.SetFloat(kShaderAlpha, percentageComplete * 0.5f + 0.5f); // lerp towards fully opaque from 50% transparent

                yield return null;
            }

            m_TopFaceMaterial.SetFloat(kShaderBlur, originalBlurAmount);
            m_TopFaceMaterial.SetFloat(kShaderVerticalOffset, 0f);
            m_TopFaceMaterial.SetFloat(kShaderAlpha, 1f);

            yield return null;
        }

        void AdjustHandlesAndIcons()
        {
            var size = m_Bounds.size;
            m_HandleRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
            m_HandleRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.z);

            var halfFrontZOffset = m_FrontZOffset * 0.5f;
            var localPosition = m_ResizeIconRectTransform.localPosition;
            localPosition.z = -halfFrontZOffset;
            m_ResizeIconRectTransform.localPosition = localPosition;
            m_ResizeIconRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
            m_ResizeIconRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.z + m_FrontZOffset);

            var extents = m_Bounds.extents;
            var halfWidth = extents.x;
            var halfDepth = extents.z;
            var yOffset = m_FrameHeight * (0.5f - m_LerpAmount);
            localPosition = m_BottomFrontHandle.localPosition;
            localPosition.z = yOffset;
            localPosition.y = -halfDepth - m_FrontZOffset;
            m_BottomFrontHandle.localPosition = localPosition;

            yOffset = (1 - m_LerpAmount) * m_FrameHeight;
            var angle = Mathf.Atan(m_FrontZOffset / yOffset) * Mathf.Rad2Deg;

            m_FrontLeftHandle.localPosition = new Vector3(-halfWidth, -yOffset * 0.5f, -halfDepth - halfFrontZOffset);
            m_FrontLeftHandle.localRotation = Quaternion.AngleAxis(angle, Vector3.right);
            m_FrontLeftHandle.localScale = new Vector3(m_FrontFrameHandleSize, m_FrameHeight, m_FrontFrameHandleSize);
            localPosition = m_FrontLeftHandle.localPosition;
            localPosition.x = halfWidth;

            m_FrontRightHandle.localPosition = localPosition;
            m_FrontRightHandle.localRotation = m_FrontLeftHandle.localRotation;
            m_FrontRightHandle.localScale = m_FrontLeftHandle.localScale;

            var zOffset = m_FrontZOffset - (k_FrontFrameZOffset + m_FrontFrameHandleSize) * 0.5f;
            var zScale = m_FrameHeight * m_LerpAmount;
            var yPosition = -m_FrontFrameHandleSize * 0.5f - m_FrameHeight + zScale * 0.5f;
            m_FrontLeftCornerHandle.localPosition = new Vector3(-halfWidth, yPosition, -halfDepth - zOffset - m_FrontFrameHandleSize);
            m_FrontLeftCornerHandle.localScale = new Vector3(m_FrontFrameHandleSize, k_FrontFrameZOffset, zScale);

            m_FrontRightCornerHandle = m_FrontRightCornerHandle.transform;
            localPosition = m_FrontLeftCornerHandle.localPosition;
            localPosition.x = halfWidth;
            m_FrontRightCornerHandle.localPosition = localPosition;
            m_FrontRightCornerHandle.localRotation = m_FrontLeftCornerHandle.localRotation;
            m_FrontRightCornerHandle.localScale = m_FrontLeftCornerHandle.localScale;
        }

        void OnHandleHoverStarted(BaseHandle handle, HandleEventData eventData)
        {
            var rayOrigin = eventData.rayOrigin;
            if (m_HovereringRayOrigins.Count == 0 && m_DragState == null)
                IncreaseFrameThickness(rayOrigin);

            m_HovereringRayOrigins.Add(rayOrigin);
        }

        ResizeDirection GetResizeDirectionForLocalPosition(Vector3 localPosition)
        {
            var direction = localPosition.z > 0 ? ResizeDirection.Back : ResizeDirection.Front;
            var xDirection = localPosition.x > 0 ? ResizeDirection.Right : ResizeDirection.Left;

            var zDistance = bounds.extents.z - Mathf.Abs(localPosition.z);
            if (localPosition.z < 0)
                zDistance += m_FrontZOffset;

            var cornerZ = zDistance < m_ResizeCornerSize;
            var cornerX = bounds.extents.x - Mathf.Abs(localPosition.x) < m_ResizeCornerSize;

            if (cornerZ && cornerX)
                direction |= xDirection;
            else if (cornerX)
                direction = xDirection;

            return direction;
        }

        Image GetResizeIconForDirection(ResizeDirection direction)
        {
            switch (direction)
            {
                default:
                    return m_FrontResizeIcon;
                case ResizeDirection.Back:
                    return m_BackResizeIcon;
                case ResizeDirection.Left:
                    return m_LeftResizeIcon;
                case ResizeDirection.Right:
                    return m_RightResizeIcon;
                case ResizeDirection.Front | ResizeDirection.Left:
                    return m_FrontLeftResizeIcon;
                case ResizeDirection.Front | ResizeDirection.Right:
                    return m_FrontRightResizeIcon;
                case ResizeDirection.Back | ResizeDirection.Left:
                    return m_BackLeftResizeIcon;
                case ResizeDirection.Back | ResizeDirection.Right:
                    return m_BackRightResizeIcon;
            }
        }

        void OnHandleHoverEnded(BaseHandle handle, HandleEventData eventData)
        {
            var rayOrigin = eventData.rayOrigin;
            if (m_HovereringRayOrigins.Remove(rayOrigin))
            {
                Image lastResizeIcon;
                if (m_LastResizeIcons.TryGetValue(rayOrigin, out lastResizeIcon))
                {
                    lastResizeIcon.CrossFadeAlpha(0f, k_ResizeIconCrossfadeDuration, true);
                    m_LastResizeIcons.Remove(rayOrigin);
                }
            }

            if (m_HovereringRayOrigins.Count == 0)
                ResetFrameThickness();
        }

        void Update()
        {
            if (!m_DynamicFaceAdjustment)
                return;

            var currentXRotation = transform.rotation.eulerAngles.x;
            if (Mathf.Approximately(currentXRotation, m_PreviousXRotation))
                return; // Exit if no x rotation change occurred for this frame

            m_PreviousXRotation = currentXRotation;

            // a second additional value added to the y offset of the front panel when it is in mid-reveal,
            // lerped in at the middle of the rotation/reveal, and lerped out at the beginning & end of the rotation/reveal
            const int kRevealCompensationBlendShapeIndex = 5;
            const float kLerpPadding = 1.2f; // pad lerp values increasingly as it increases, displaying the "front face reveal" sooner
            const float kCorrectiveRevealShapeMultiplier = 1.85f;
            var angledAmount = Mathf.Clamp(Mathf.DeltaAngle(currentXRotation, 0f), 0f, 90f);
            var midRevealCorrectiveShapeAmount = Mathf.PingPong(angledAmount * kCorrectiveRevealShapeMultiplier, 90);

            // add lerp padding to reach and maintain the target value sooner
            m_LerpAmount = angledAmount / 90f;
            var paddedLerp = m_LerpAmount * kLerpPadding;

            // offset front panel according to workspace rotation angle
            const float kAdditionalFrontPanelLerpPadding = 1.1f;
            const float kFrontPanelYOffset = 0.03f;
            const float kFrontPanelZStartOffset = 0.0084f;
            const float kFrontPanelZEndOffset = -0.05f;
            m_FrontPanel.localRotation = Quaternion.Euler(Vector3.Lerp(k_BaseFrontPanelRotation, k_MaxFrontPanelRotation, paddedLerp * kAdditionalFrontPanelLerpPadding));
            m_FrontPanel.localPosition = Vector3.Lerp(Vector3.forward * kFrontPanelZStartOffset, new Vector3(0, kFrontPanelYOffset, kFrontPanelZEndOffset), paddedLerp);

            m_FrontZOffset = (k_FrontFrameZOffset + m_FrontFrameHandleSize) * Mathf.Clamp01(paddedLerp * kAdditionalFrontPanelLerpPadding);
            var frameBounds = adjustedBounds;
            m_FrameCollider.size = frameBounds.size;
            m_FrameCollider.center = frameBounds.center;

            AdjustHandlesAndIcons();

            // change blendshapes according to workspace rotation angle
            m_Frame.SetBlendShapeWeight(k_AngledFaceBlendShapeIndex, angledAmount * kLerpPadding);
            m_Frame.SetBlendShapeWeight(kRevealCompensationBlendShapeIndex, midRevealCorrectiveShapeAmount);
        }

        public void ProcessInput(WorkspaceInput input, ConsumeControlDelegate consumeControl)
        {
            if (m_Controls.Count == 0)
                InputUtils.GetBindingDictionaryFromActionMap(input.actionMap, m_Controls);

            var moveResizeLeft = input.moveResizeLeft;
            var moveResizeRight = input.moveResizeRight;

            if (m_DragState != null)
            {
                var rayOrigin = m_DragState.rayOrigin;

                if ((rayOrigin == leftRayOrigin && moveResizeLeft.wasJustReleased)
                    || (rayOrigin == rightRayOrigin && moveResizeRight.wasJustReleased))
                {
                    m_DragState = null;
                    m_LastResizeIcons.Clear();

                    foreach (var smoothMotion in GetComponentsInChildren<SmoothMotion>())
                    {
                        smoothMotion.enabled = true;
                    }

                    highlightsVisible = false;
                }
                else
                {
                    m_DragState.OnDragging();
                }

                return;
            }

            Transform dragRayOrigin = null;
            Image dragResizeIcon = null;
            var resizing = false;

            var hasLeft = false;
            var hasRight = false;
            for (int i = 0; i < m_HovereringRayOrigins.Count; i++)
            {
                var rayOrigin = m_HovereringRayOrigins[i];
                Image lastResizeIcon;
                m_LastResizeIcons.TryGetValue(rayOrigin, out lastResizeIcon);
                if (rayOrigin == leftRayOrigin)
                {
                    if (m_LeftResizeFeedback.Count == 0)
                        ShowLeftResizeFeedback();

                    if (moveResizeLeft.wasJustPressed)
                    {
                        consumeControl(moveResizeLeft);
                        dragRayOrigin = rayOrigin;
                        dragResizeIcon = lastResizeIcon;
                        resizing = true;
                    }

                    hasLeft = true;
                }

                if (rayOrigin == rightRayOrigin)
                {
                    if (m_RightResizeFeedback.Count == 0)
                        ShowRightResizeFeedback();

                    if (moveResizeRight.wasJustPressed)
                    {
                        consumeControl(moveResizeRight);
                        dragRayOrigin = rayOrigin;
                        dragResizeIcon = lastResizeIcon;
                        resizing = true;
                    }

                    hasRight = true;
                }

                const float kVisibleOpacity = 0.75f;
                var localPosition = transform.InverseTransformPoint(this.GetPointerPosition(rayOrigin));
                var direction = GetResizeDirectionForLocalPosition(localPosition);
                var resizeIcon = GetResizeIconForDirection(direction);

                if (lastResizeIcon != null)
                {
                    if (resizeIcon != lastResizeIcon)
                    {
                        resizeIcon.CrossFadeAlpha(kVisibleOpacity, k_ResizeIconCrossfadeDuration, true);
                        lastResizeIcon.CrossFadeAlpha(0f, k_ResizeIconCrossfadeDuration, true);
                    }
                }
                else
                {
                    resizeIcon.CrossFadeAlpha(kVisibleOpacity, k_ResizeIconCrossfadeDuration, true);
                }

                m_LastResizeIcons[rayOrigin] = resizeIcon;

                var iconTransform = resizeIcon.transform;
                var iconPosition = iconTransform.localPosition;
                var smoothFollow = lastResizeIcon == null ? 1 : k_ResizeIconSmoothFollow * Time.deltaTime;
                var localDirection = localPosition - transform.InverseTransformPoint(rayOrigin.position);
                switch (direction)
                {
                    case ResizeDirection.Front:
                    case ResizeDirection.Back:
                        var iconPositionX = iconPosition.x;
                        var positionOffsetX = Mathf.Sign(localDirection.x) * m_ResizeHandleMargin;
                        var tergetPositionX = localPosition.x + positionOffsetX;
                        if (Mathf.Abs(tergetPositionX) > bounds.extents.x - m_ResizeCornerSize)
                            tergetPositionX = localPosition.x - positionOffsetX;

                        iconPosition.x = Mathf.Lerp(iconPositionX, tergetPositionX, smoothFollow);
                        break;
                    case ResizeDirection.Left:
                    case ResizeDirection.Right:
                        var iconPositionY = iconPosition.y;
                        var positionOffsetY = Mathf.Sign(localDirection.z) * m_ResizeHandleMargin;
                        var tergetPositionY = localPosition.z + positionOffsetY;
                        if (Mathf.Abs(tergetPositionY) > bounds.extents.z - m_ResizeCornerSize)
                            tergetPositionY = localPosition.z - positionOffsetY;

                        iconPosition.y = Mathf.Lerp(iconPositionY, tergetPositionY, smoothFollow);
                        break;
                }

                iconTransform.localPosition = iconPosition;
            }

            if (!hasRight)
                HideRightResizeFeedback();

            if (!hasLeft)
                HideLeftResizeFeedback();

            var adjustedBounds = this.adjustedBounds;
            if (!dragRayOrigin)
            {
                var leftPosition = transform.InverseTransformPoint(leftRayOrigin.position);
                var leftPointerPosition = transform.InverseTransformPoint(this.GetPointerPosition(leftRayOrigin));
                if (adjustedBounds.Contains(leftPosition) || adjustedBounds.Contains(leftPointerPosition))
                {
                    if (m_LeftMoveFeedback.Count == 0)
                        ShowLeftMoveFeedback();

                    if (moveResizeLeft.wasJustPressed)
                    {
                        dragRayOrigin = leftRayOrigin;
                        m_LastResizeIcons.TryGetValue(dragRayOrigin, out dragResizeIcon);
                        consumeControl(moveResizeLeft);
                    }
                }
                else
                {
                    HideLeftMoveFeedback();
                }

                var rightPosition = transform.InverseTransformPoint(rightRayOrigin.position);
                var rightPointerPosition = transform.InverseTransformPoint(this.GetPointerPosition(rightRayOrigin));
                if (adjustedBounds.Contains(rightPosition) || adjustedBounds.Contains(rightPointerPosition))
                {
                    if (m_RightMoveFeedback.Count == 0)
                        ShowRightMoveFeedback();

                    if (moveResizeRight.wasJustPressed)
                    {
                        dragRayOrigin = rightRayOrigin;
                        m_LastResizeIcons.TryGetValue(dragRayOrigin, out dragResizeIcon);
                        consumeControl(moveResizeRight);
                    }
                }
                else
                {
                    HideRightMoveFeedback();
                }
            }

            if (dragRayOrigin)
            {
                m_DragState = new DragState(this, dragRayOrigin, resizing);
                if (dragResizeIcon != null)
                    dragResizeIcon.CrossFadeAlpha(0f, k_ResizeIconCrossfadeDuration, true);

                ResetFrameThickness();

                foreach (var smoothMotion in GetComponentsInChildren<SmoothMotion>())
                {
                    smoothMotion.enabled = false;
                }

                highlightsVisible = true;
            }
        }
        void OnDestroy()
        {
            UnityObjectUtils.Destroy(m_TopFaceMaterial);
            UnityObjectUtils.Destroy(m_FrontFaceMaterial);

            m_CloseButton.clicked -= OnCloseClicked;
            m_CloseButton.hovered -= OnButtonHovered;
            m_ResizeButton.clicked -= OnResetSizeClicked;
            m_ResizeButton.hovered -= OnButtonHovered;
        }

        void OnCloseClicked(Transform rayOrigin)
        {
            if (closeClicked != null)
                closeClicked(rayOrigin);
        }

        void OnResetSizeClicked(Transform rayOrigin)
        {
            if (resetSizeClicked != null)
                resetSizeClicked(rayOrigin);
        }

        void OnButtonHovered(Transform rayOrigin)
        {
            if (buttonHovered != null)
                buttonHovered(rayOrigin);
        }

        void IncreaseFrameThickness(Transform rayOrigin = null)
        {
            this.StopCoroutine(ref m_FrameThicknessCoroutine);
            const float kTargetBlendAmount = 0f;
            m_FrameThicknessCoroutine = StartCoroutine(ChangeFrameThickness(kTargetBlendAmount, rayOrigin));
        }

        void ResetFrameThickness()
        {
            this.StopCoroutine(ref m_FrameThicknessCoroutine);
            const float kTargetBlendAmount = 50f;
            m_FrameThicknessCoroutine = StartCoroutine(ChangeFrameThickness(kTargetBlendAmount, null));
        }

        IEnumerator ChangeFrameThickness(float targetBlendAmount, Transform rayOrigin)
        {
            const float kTargetDuration = 0.25f;
            var currentDuration = 0f;
            var currentBlendAmount = m_Frame.GetBlendShapeWeight(k_ThinFrameBlendShapeIndex);
            var currentVelocity = 0f;
            while (currentDuration < kTargetDuration)
            {
                currentDuration += Time.deltaTime;
                currentBlendAmount = MathUtilsExt.SmoothDamp(currentBlendAmount, targetBlendAmount, ref currentVelocity, kTargetDuration, Mathf.Infinity, Time.deltaTime);
                m_Frame.SetBlendShapeWeight(k_ThinFrameBlendShapeIndex, currentBlendAmount);
                yield return null;
            }

            // If hovering the frame, and not moving, perform haptic feedback
            if (hoveringFrame != null && m_HovereringRayOrigins.Count > 0 && m_DragState == null && Mathf.Approximately(targetBlendAmount, 0f))
                hoveringFrame(rayOrigin);

            m_FrameThicknessCoroutine = null;
        }

        IEnumerator ShowTopFace()
        {
            const string kMaterialHighlightAlphaProperty = "_Alpha";
            const float kTargetAlpha = 1f;
            const float kTargetDuration = 0.35f;
            var currentDuration = 0f;
            var currentAlpha = m_TopFaceMaterial.GetFloat(kMaterialHighlightAlphaProperty);
            var currentVelocity = 0f;
            while (currentDuration < kTargetDuration)
            {
                currentDuration += Time.deltaTime;
                currentAlpha = MathUtilsExt.SmoothDamp(currentAlpha, kTargetAlpha, ref currentVelocity, kTargetDuration, Mathf.Infinity, Time.deltaTime);
                m_TopFaceMaterial.SetFloat(kMaterialHighlightAlphaProperty, currentAlpha);
                yield return null;
            }

            m_TopFaceVisibleCoroutine = null;
        }

        IEnumerator HideTopFace()
        {
            const string kMaterialHighlightAlphaProperty = "_Alpha";
            const float kTargetAlpha = 0f;
            const float kTargetDuration = 0.2f;
            var currentDuration = 0f;
            var currentAlpha = m_TopFaceMaterial.GetFloat(kMaterialHighlightAlphaProperty);
            var currentVelocity = 0f;
            while (currentDuration < kTargetDuration)
            {
                currentDuration += Time.deltaTime;
                currentAlpha = MathUtilsExt.SmoothDamp(currentAlpha, kTargetAlpha, ref currentVelocity, kTargetDuration, Mathf.Infinity, Time.deltaTime);
                m_TopFaceMaterial.SetFloat(kMaterialHighlightAlphaProperty, currentAlpha);
                yield return null;
            }

            m_TopFaceVisibleCoroutine = null;
        }

        void OnMoving(Transform rayOrigin)
        {
            if (moving != null)
                moving(rayOrigin);
        }

        void OnResizing(Transform rayOrigin)
        {
            if (resizing != null)
                resizing(rayOrigin);
        }

        void ShowFeedback(List<ProxyFeedbackRequest> requests, Node node, string controlName, string tooltipText, int priority = 1)
        {
            if (tooltipText == null)
                tooltipText = controlName;

            List<VRInputDevice.VRControl> ids;
            if (m_Controls.TryGetValue(controlName, out ids))
            {
                foreach (var id in ids)
                {
                    var request = this.GetFeedbackRequestObject<ProxyFeedbackRequest>(this);
                    request.node = node;
                    request.control = id;
                    request.priority = priority;
                    request.tooltipText = tooltipText;
                    requests.Add(request);
                    this.AddFeedbackRequest(request);
                }
            }
        }

        void ShowLeftMoveFeedback()
        {
            ShowFeedback(m_LeftMoveFeedback, Node.LeftHand, "Move Resize Left", "Move Workspace");
        }

        void ShowLeftResizeFeedback()
        {
            ShowFeedback(m_LeftResizeFeedback, Node.LeftHand, "Move Resize Left", "Resize Workspace", 2);
        }

        void ShowRightMoveFeedback()
        {
            ShowFeedback(m_RightMoveFeedback, Node.RightHand, "Move Resize Right", "Move Workspace");
        }

        void ShowRightResizeFeedback()
        {
            ShowFeedback(m_RightResizeFeedback, Node.RightHand, "Move Resize Right", "Resize Workspace", 2);
        }

        void HideFeedback(List<ProxyFeedbackRequest> requests)
        {
            foreach (var request in requests)
            {
                this.RemoveFeedbackRequest(request);
            }
            requests.Clear();
        }

        void HideLeftMoveFeedback()
        {
            HideFeedback(m_LeftMoveFeedback);
        }

        void HideRightMoveFeedback()
        {
            HideFeedback(m_RightMoveFeedback);
        }

        void HideLeftResizeFeedback()
        {
            HideFeedback(m_LeftResizeFeedback);
        }

        void HideRightResizeFeedback()
        {
            HideFeedback(m_RightResizeFeedback);
        }
    }
}
