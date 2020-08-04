using System.Collections;
using Unity.EditorXR.Core;
using Unity.EditorXR.Extensions;
using Unity.EditorXR.Interfaces;
using Unity.EditorXR.Utilities;
using Unity.XRTools.ModuleLoader;
using UnityEngine;

namespace Unity.EditorXR.Menus
{
    class SpatialHintUI : MonoBehaviour, IUsesViewerScale, IUsesControlHaptics, IRayToNode
    {
        readonly Color k_PrimaryArrowColor = Color.white;

#pragma warning disable 649
        [Header("Scroll Visuals")]
        [SerializeField]
        CanvasGroup m_ScrollVisualsCanvasGroup;

        [SerializeField]
        HintIcon m_ScrollVisualsDragSourceArrow;

        [SerializeField]
        HintIcon m_ScrollVisualsDragTargetArrow;

        [SerializeField]
        HintLine m_ScrollHintLine;

        [SerializeField]
        HapticPulse m_ScrollBarDefineHapticPulse; // Haptic pulse performed when dragging out the spatial scroll bar

        [Header("Primary Directional Visuals")]
        [SerializeField]
        HintIcon[] m_PrimaryDirectionalHintArrows;

        [SerializeField]
        HintIcon[] m_SecondaryDirectionalHintArrows;
#pragma warning restore 649

        bool m_Visible;
        bool m_PreScrollArrowsVisible;
        Vector3 m_ScrollVisualsRotation;
        Transform m_ScrollVisualsTransform;
        Coroutine m_ScrollVisualsVisibilityCoroutine;
        Transform m_ScrollVisualsDragTargetArrowTransform;
        Node m_ControllingNode;

        /// <summary>
        /// Bool denoting the visibility of the Spatial Hint UI elements
        /// </summary>
        public bool visible
        {
            get { return m_Visible; }
            set
            {
                m_Visible = value;

                if (m_Visible)
                {
                    foreach (var arrow in m_PrimaryDirectionalHintArrows)
                    {
                        arrow.visibleColor = k_PrimaryArrowColor;
                    }

                    foreach (var arrow in m_SecondaryDirectionalHintArrows)
                    {
                        arrow.visible = false;
                    }
                }
                else
                {
                    foreach (var arrow in m_PrimaryDirectionalHintArrows)
                    {
                        arrow.visible = false;
                    }

                    foreach (var arrow in m_SecondaryDirectionalHintArrows)
                    {
                        arrow.visible = false;
                    }

                    scrollVisualsRotation = Vector3.zero;
                }
            }
        }

        /// <summary>
        /// Bool denoting the visibility of the Spatial Scroll visual elements
        /// </summary>
        public bool scrollVisualsVisible
        {
            set
            {
                if (value)
                    this.RestartCoroutine(ref m_ScrollVisualsVisibilityCoroutine, ShowScrollVisuals());
            }
        }

        /// <summary>
        /// Bool denoting the visibility of the UI elements shown before a spatial scroll has been initiated
        /// </summary>
        public bool preScrollArrowsVisible
        {
            set
            {
                m_PreScrollArrowsVisible = value;
                if (m_PreScrollArrowsVisible)
                {
                    transform.localScale = Vector3.one * this.GetViewerScale();
                    foreach (var arrow in m_PrimaryDirectionalHintArrows)
                    {
                        arrow.visibleColor = k_PrimaryArrowColor;
                    }
                }
                else
                {
                    foreach (var arrow in m_PrimaryDirectionalHintArrows)
                    {
                        arrow.visible = false;
                    }
                }
            }
        }

        /// <summary>
        /// Bool denoting the visibility of the secondary arrow visuals
        /// </summary>
        public bool secondaryArrowsVisible
        {
            set
            {
                foreach (var arrow in m_SecondaryDirectionalHintArrows)
                {
                    arrow.visible = value;
                }
            }
        }

        bool scrollArrowsVisible
        {
            set
            {
                m_ScrollVisualsDragSourceArrow.visible = value;
                m_ScrollVisualsDragTargetArrow.visible = value;
            }
        }

        /// <summary>
        /// If non-null, enable and set the world rotation of the scroll visuals
        /// </summary>
        public Vector3 scrollVisualsRotation { set { m_ScrollVisualsRotation = value; } }

        /// <summary>
        /// The node currently controlling the spatial hint visuals
        /// </summary>
        public Node controllingNode
        {
            get { return m_ControllingNode; }
            set
            {
                m_ControllingNode = value;

                if (m_ControllingNode == Node.None)
                {
                    scrollVisualsRotation = Vector3.zero;
                    this.RestartCoroutine(ref m_ScrollVisualsVisibilityCoroutine, HideScrollVisuals());
                    scrollVisualsVisible = false;
                }
            }
        }

        /// <summary>
        /// The position, whose magnitude from the scroll origin is used to trigger a spatial scroll
        /// </summary>
        public Vector3 scrollVisualsDragThresholdTriggerPosition { get; set; }

        /// <summary>
        /// The content container housing the spatial scroll visuals
        /// </summary>
        public Transform contentContainer { get { return transform; } }

        /// <summary>
        /// If TRUE, expand scroll hint arrows from center of initial scroll trigger.
        /// If FALSE, draw scroll hint line visuals along the line the user is defining
        /// </summary>
        public bool centeredScrolling { get; set; }

#if !FI_AUTOFILL
        IProvidesViewerScale IFunctionalitySubscriber<IProvidesViewerScale>.provider { get; set; }
        IProvidesControlHaptics IFunctionalitySubscriber<IProvidesControlHaptics>.provider { get; set; }
#endif

        void Awake()
        {
            m_ScrollVisualsTransform = m_ScrollVisualsCanvasGroup.transform;
            m_ScrollVisualsCanvasGroup.alpha = 0f;

            m_ScrollVisualsDragTargetArrowTransform = m_ScrollVisualsDragTargetArrow.transform;
        }

        IEnumerator ShowScrollVisuals()
        {
            // Display two arrows denoting the positive and negative directions allow for spatial scrolling, as defined by the drag vector
            scrollArrowsVisible = true;
            preScrollArrowsVisible = false;
            secondaryArrowsVisible = false;
            transform.localScale = Vector3.one * this.GetViewerScale();
            m_ScrollVisualsTransform.LookAt(m_ScrollVisualsRotation, Vector3.up); // Scroll arrows should face/billboard the user.
            m_ScrollVisualsCanvasGroup.alpha = 1f; // remove
            m_ScrollVisualsDragTargetArrowTransform.localPosition = Vector3.zero;

            const float kTargetDuration = 1f;
            var currentDuration = 0f;
            var currentLocalScale = m_ScrollVisualsTransform.localScale;
            var targetLocalScale = Vector3.one;
            var currentAlpha = m_ScrollVisualsCanvasGroup.alpha;
            var secondArrowCurrentPosition = m_ScrollVisualsDragTargetArrowTransform.position;
            while (currentDuration < kTargetDuration)
            {
                var shapedDuration = MathUtilsExt.SmoothInOutLerpFloat(currentDuration / kTargetDuration);
                m_ScrollVisualsCanvasGroup.alpha = Mathf.Lerp(currentAlpha, 1f, shapedDuration);

                // Only validate movement in the initial direction with which the user began the drag
                m_ScrollVisualsDragTargetArrowTransform.position = Vector3.Lerp(secondArrowCurrentPosition, scrollVisualsDragThresholdTriggerPosition, shapedDuration);

                currentDuration += Time.unscaledDeltaTime * 2f;

                m_ScrollVisualsDragTargetArrowTransform.LookAt(m_ScrollVisualsDragTargetArrowTransform.position - m_ScrollVisualsTransform.position);
                m_ScrollVisualsDragTargetArrowTransform.LookAt(m_ScrollVisualsTransform.position - m_ScrollVisualsDragTargetArrowTransform.position);
                m_ScrollVisualsTransform.localScale = Vector3.Lerp(currentLocalScale, targetLocalScale, shapedDuration);

                var scrollVisualsDragTargetArrowTransformOrigin = m_ScrollVisualsTransform.position;
                var scrollVisualsDragTargetArrowTransformDestination = m_ScrollVisualsDragTargetArrowTransform.position;
                if (centeredScrolling)
                {
                    Vector3 offset = (scrollVisualsDragTargetArrowTransformOrigin - scrollVisualsDragTargetArrowTransformDestination) * -1;
                    var distance = (scrollVisualsDragTargetArrowTransformOrigin - scrollVisualsDragTargetArrowTransformDestination).magnitude;

                    // Increase the initial line position separation for scrolls of a smaller magnitude
                    // This mandates a sully visible scroll line, regardless of scroll start/end magnitude
                    var distanceShaped = Mathf.Clamp(2 - distance * 0.175f, 0.75f, 2f);
                    offset *= distanceShaped;
                    scrollVisualsDragTargetArrowTransformOrigin -= offset;
                    scrollVisualsDragTargetArrowTransformDestination += offset;
                }

                var lineRendererPositions = new[] { scrollVisualsDragTargetArrowTransformOrigin, scrollVisualsDragTargetArrowTransformDestination };
                m_ScrollHintLine.Positions = lineRendererPositions;
                m_ScrollHintLine.LineWidth = shapedDuration * this.GetViewerScale();

                this.Pulse(controllingNode, m_ScrollBarDefineHapticPulse, 1f, 1f + 8 * currentDuration);

                yield return null;
            }

            m_ScrollVisualsCanvasGroup.alpha = 1f;
        }

        IEnumerator HideScrollVisuals()
        {
            scrollArrowsVisible = false;

            const float kTargetDuration = 1f;
            var hiddenLocalScale = Vector3.zero;
            var currentDuration = 0f;
            var currentLocalScale = m_ScrollVisualsTransform.localScale;
            var currentAlpha = m_ScrollVisualsCanvasGroup.alpha;
            while (currentDuration < kTargetDuration)
            {
                var shapedDuration = MathUtilsExt.SmoothInOutLerpFloat(currentDuration / kTargetDuration);
                m_ScrollVisualsTransform.localScale = Vector3.Lerp(currentLocalScale, hiddenLocalScale, shapedDuration);
                m_ScrollVisualsCanvasGroup.alpha = Mathf.Lerp(currentAlpha, 0f, shapedDuration);
                currentDuration += Time.unscaledDeltaTime * 3.5f;
                m_ScrollHintLine.LineWidth = (1 - shapedDuration) * this.GetViewerScale();
                yield return null;
            }

            m_ScrollVisualsCanvasGroup.alpha = 0;
            m_ScrollVisualsTransform.localScale = hiddenLocalScale;
        }

        /// <summary>
        /// Pulse the scroll arrows
        /// </summary>
        public void PulseScrollArrows()
        {
            m_ScrollVisualsDragSourceArrow.PulseColor();
            m_ScrollVisualsDragTargetArrow.PulseColor();
            m_ScrollHintLine.PulseColor();
        }
    }
}
