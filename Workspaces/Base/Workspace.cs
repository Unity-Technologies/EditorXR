using System;
using System.Collections;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
    abstract class Workspace : MonoBehaviour, IWorkspace, IInstantiateUI, IUsesStencilRef, IConnectInterfaces,
        IUsesViewerScale, IControlHaptics, IRayToNode
    {
        const float k_MaxFrameSize = 100f; // Because BlendShapes cap at 100, our workspace maxes out at 100m wide
        protected const float k_DoubleFaceMargin = FaceMargin * 2;

        public static readonly Vector3 DefaultBounds = new Vector3(0.7f, 0f, 0.4f);
        public static readonly Vector3 MinBounds = new Vector3(0.677f, 0f, 0.1f);

        public const float FaceMargin = 0.025f;
        public const float HighlightMargin = 0.002f;

#pragma warning disable 649
        [SerializeField]
        Vector3 m_MinBounds = MinBounds;

        [SerializeField]
        GameObject m_BasePrefab;

        [SerializeField]
        ActionMap m_ActionMap;

        [SerializeField]
        HapticPulse m_ButtonClickPulse;

        [SerializeField]
        HapticPulse m_ButtonHoverPulse;

        [SerializeField]
        HapticPulse m_ResizePulse;

        [SerializeField]
        protected HapticPulse m_MovePulse;
#pragma warning restore 649

        Bounds m_ContentBounds;
        BoxCollider m_OuterCollider;

        Coroutine m_VisibilityCoroutine;
        Coroutine m_ResetSizeCoroutine;

        protected WorkspaceUI m_WorkspaceUI;

        protected Vector3? m_CustomStartingBounds;

        protected Transform m_LeftRayOrigin;
        protected Transform m_RightRayOrigin;

        public Vector3 minBounds
        {
            get { return m_MinBounds; }
            set { m_MinBounds = value; }
        }

        public Bounds contentBounds
        {
            get { return m_ContentBounds; }
            set
            {
                if (!value.Equals(contentBounds))
                {
                    m_ContentBounds = value;
                    var size = value.size;
                    size.x = Mathf.Clamp(Mathf.Max(size.x, minBounds.x), 0, k_MaxFrameSize);
                    size.y = Mathf.Max(size.y, minBounds.y);
                    size.z = Mathf.Clamp(Mathf.Max(size.z, minBounds.z), 0, k_MaxFrameSize);
                    m_ContentBounds.size = size;

                    // Offset by half height
                    var center = m_ContentBounds.center;
                    center.y = size.y * 0.5f;
                    m_ContentBounds.center = center;

                    UpdateBounds();
                    OnBoundsChanged();
                }
            }
        }

        public Bounds outerBounds
        {
            get
            {
                const float outerBoundsCenterOffset = 0.09275f; //Amount to extend the bounds to include frame
                return new Bounds(contentBounds.center + Vector3.down * outerBoundsCenterOffset * 0.5f,
                    new Vector3(
                        contentBounds.size.x,
                        contentBounds.size.y + outerBoundsCenterOffset,
                        contentBounds.size.z
                    ));
            }
        }

        public Bounds vacuumBounds { get { return outerBounds; } }

        public byte stencilRef { get; set; }

        /// <summary>
        /// If true, allow the front face of the workspace to dynamically adjust its angle when rotated
        /// </summary>
        public bool dynamicFaceAdjustment { set { m_WorkspaceUI.dynamicFaceAdjustment = value; } }

        /// <summary>
        /// If true, prevent the resizing of a workspace
        /// </summary>
        public bool preventResize { set { m_WorkspaceUI.preventResize = value; } }

        /// <summary>
        /// (-1 to 1) ranged value that controls the separator mask's X-offset placement
        /// A value of zero will leave the mask in the center of the workspace
        /// </summary>
        public float topPanelDividerOffset
        {
            set
            {
                m_WorkspaceUI.topPanelDividerOffset = value;
                m_WorkspaceUI.bounds = contentBounds;
            }
        }

        public event Action<IWorkspace> destroyed;

        public Transform topPanel { get { return m_WorkspaceUI.topFaceContainer; } }

        public Transform frontPanel { get { return m_WorkspaceUI.frontPanel; } }

        public ActionMap actionMap { get { return m_ActionMap; } }
        public bool ignoreActionMapInputLocking { get { return false; } }

        public Transform leftRayOrigin
        {
            protected get { return m_LeftRayOrigin; }
            set
            {
                m_LeftRayOrigin = value;
                leftNode = this.RequestNodeFromRayOrigin(m_LeftRayOrigin);
            }
        }

        public Transform rightRayOrigin
        {
            protected get { return m_RightRayOrigin; }
            set
            {
                m_RightRayOrigin = value;
                rightNode = this.RequestNodeFromRayOrigin(m_RightRayOrigin);
            }
        }

        protected Node leftNode { get; set; }
        protected Node rightNode { get; set; }

        public virtual void Setup()
        {
            var baseObject = this.InstantiateUI(m_BasePrefab, transform, false);

            m_WorkspaceUI = baseObject.GetComponent<WorkspaceUI>();
            this.ConnectInterfaces(m_WorkspaceUI);
            m_WorkspaceUI.closeClicked += OnCloseClicked;
            m_WorkspaceUI.resetSizeClicked += OnResetClicked;
            m_WorkspaceUI.buttonHovered += OnButtonHovered;
            m_WorkspaceUI.hoveringFrame += OnHoveringFrame;
            m_WorkspaceUI.moving += OnMoving;
            m_WorkspaceUI.resizing += OnResizing;

            m_WorkspaceUI.leftRayOrigin = leftRayOrigin;
            m_WorkspaceUI.rightRayOrigin = rightRayOrigin;

            m_WorkspaceUI.resize += bounds =>
            {
                var size = contentBounds.size;
                var boundsSize = bounds.size;
                size.x = boundsSize.x;
                size.z = boundsSize.z;
                var content = contentBounds;
                content.size = size;
                contentBounds = content;
            };

            m_WorkspaceUI.sceneContainer.transform.localPosition = Vector3.zero;

            m_OuterCollider = gameObject.AddComponent<BoxCollider>();
            m_OuterCollider.isTrigger = true;

            var startingBounds = m_CustomStartingBounds ?? DefaultBounds;

            //Do not set bounds directly, in case OnBoundsChanged requires Setup override to complete
            m_ContentBounds = new Bounds(Vector3.up * startingBounds.y * 0.5f, startingBounds); // If custom bounds have been set, use them as the initial bounds
            UpdateBounds();

            this.RestartCoroutine(ref m_VisibilityCoroutine, AnimateShow());
        }

        public void Close()
        {
            this.RestartCoroutine(ref m_VisibilityCoroutine, AnimateHide());
        }

        protected virtual void OnCloseClicked(Transform rayOrigin)
        {
            this.Pulse(this.RequestNodeFromRayOrigin(rayOrigin), m_ButtonClickPulse);
            Close();
        }

        protected virtual void OnResetClicked(Transform rayOrigin)
        {
            this.Pulse(this.RequestNodeFromRayOrigin(rayOrigin), m_ButtonClickPulse);
            this.RestartCoroutine(ref m_ResetSizeCoroutine, AnimateResetSize());
        }

        protected void OnButtonHovered(Transform rayOrigin)
        {
            this.Pulse(this.RequestNodeFromRayOrigin(rayOrigin), m_ButtonHoverPulse);
        }

        public void SetUIHighlightsVisible(bool value)
        {
            m_WorkspaceUI.highlightsVisible = value;
        }

        void UpdateBounds()
        {
            m_WorkspaceUI.bounds = contentBounds;

            var outerBounds = this.outerBounds;
            m_OuterCollider.size = outerBounds.size;
            m_OuterCollider.center = outerBounds.center;
        }

        protected virtual void OnDestroy()
        {
            destroyed(this);
        }

        protected virtual void OnBoundsChanged() { }

        IEnumerator AnimateShow()
        {
            m_WorkspaceUI.highlightsVisible = true;

            var targetScale = Vector3.one;
            var scale = Vector3.zero;
            var smoothVelocity = Vector3.zero;
            var currentDuration = 0f;
            const float kTargetDuration = 0.75f;
            while (currentDuration < kTargetDuration)
            {
                currentDuration += Time.unscaledDeltaTime;
                transform.localScale = scale;
                scale = MathUtilsExt.SmoothDamp(scale, targetScale, ref smoothVelocity, kTargetDuration, Mathf.Infinity, Time.unscaledDeltaTime);
                yield return null;
            }

            transform.localScale = targetScale;
            m_WorkspaceUI.highlightsVisible = false;
            m_VisibilityCoroutine = null;
        }

        IEnumerator AnimateHide()
        {
            var targetScale = Vector3.zero;
            var scale = transform.localScale;
            var smoothVelocity = Vector3.zero;
            var currentDuration = 0f;
            const float kTargetDuration = 0.185f;
            while (currentDuration < kTargetDuration)
            {
                currentDuration += Time.unscaledDeltaTime;
                transform.localScale = scale;
                scale = MathUtilsExt.SmoothDamp(scale, targetScale, ref smoothVelocity, kTargetDuration, Mathf.Infinity, Time.unscaledDeltaTime);
                yield return null;
            }

            transform.localScale = targetScale;
            m_WorkspaceUI.highlightsVisible = false;
            m_VisibilityCoroutine = null;
            ObjectUtils.Destroy(gameObject);
        }

        IEnumerator AnimateResetSize()
        {
            var currentBoundsSize = contentBounds.size;
            var currentBoundsCenter = contentBounds.center;
            var targetBoundsSize = m_CustomStartingBounds ?? minBounds;
            var targetBoundsCenter = Vector3.zero;
            var smoothVelocitySize = Vector3.zero;
            var smoothVelocityCenter = Vector3.zero;
            var currentDuration = 0f;
            const float kTargetDuration = 0.75f;
            while (currentDuration < kTargetDuration)
            {
                currentDuration += Time.unscaledDeltaTime;
                currentBoundsCenter = MathUtilsExt.SmoothDamp(currentBoundsCenter, targetBoundsCenter, ref smoothVelocityCenter, kTargetDuration, Mathf.Infinity, Time.unscaledDeltaTime);
                currentBoundsSize = MathUtilsExt.SmoothDamp(currentBoundsSize, targetBoundsSize, ref smoothVelocitySize, kTargetDuration, Mathf.Infinity, Time.unscaledDeltaTime);
                contentBounds = new Bounds(currentBoundsCenter, currentBoundsSize);
                OnBoundsChanged();
                yield return null;
            }

            contentBounds = new Bounds(targetBoundsCenter, targetBoundsSize);
            OnBoundsChanged();
            m_ResetSizeCoroutine = null;
        }

        public virtual void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl)
        {
            m_WorkspaceUI.ProcessInput((WorkspaceInput)input, consumeControl);
        }

        protected void OnButtonClicked(Transform rayOrigin)
        {
            this.Pulse(this.RequestNodeFromRayOrigin(rayOrigin), m_ButtonClickPulse);
        }

        void OnMoving(Transform rayOrigin)
        {
            this.Pulse(this.RequestNodeFromRayOrigin(rayOrigin), m_MovePulse);
        }

        void OnResizing(Transform rayOrigin)
        {
            this.Pulse(this.RequestNodeFromRayOrigin(rayOrigin), m_ResizePulse);
        }

        void OnHoveringFrame(Transform rayOrigin)
        {
            this.Pulse(this.RequestNodeFromRayOrigin(rayOrigin), m_ResizePulse);
        }
    }
}
