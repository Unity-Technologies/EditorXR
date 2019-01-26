using System;
using System.Collections;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Helpers;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR.Menus
{
    sealed class RadialMenuSlot : MonoBehaviour, ISetTooltipVisibility, ITooltip, ITooltipPlacement, IRayEnterHandler, IRayExitHandler
    {
        static Color s_FrameOpaqueColor;
        static readonly Vector3 k_HiddenLocalScale = new Vector3(1f, 0f, 1f);
        const float k_IconHighlightedLocalYOffset = 0.006f;
        const string k_MaterialAlphaProperty = "_Alpha";
        const string k_MaterialExpandProperty = "_Expand";
        const string k_MaterialColorTopProperty = "_ColorTop";
        const string k_MaterialColorBottomProperty = "_ColorBottom";
        const string k_MaterialColorProperty = "_Color";

        [SerializeField]
        MeshRenderer m_InsetMeshRenderer;

        [SerializeField]
        Transform m_MenuInset;

        [SerializeField]
        CanvasGroup m_CanvasGroup;

        [SerializeField]
        Image m_Icon;

        [SerializeField]
        Transform m_IconContainer;

        [SerializeField]
        Button m_Button;

        [SerializeField]
        MeshRenderer m_BorderRenderer;

        [SerializeField]
        MeshRenderer m_FrameRenderer;

        public Transform tooltipTarget { get { return m_TooltipTarget; } }

        [SerializeField]
        Transform m_TooltipTarget;

        public Transform tooltipSource { get { return m_TooltipSource; } }

        [SerializeField]
        Transform m_TooltipSource;

        public TextAlignment tooltipAlignment { get; private set; }

        public bool pressed
        {
            set
            {
                // Proceed only if value is true after previously being false
                if (m_Highlighted && value != m_Pressed && value && gameObject.activeSelf)
                {
                    m_Pressed = value;

                    this.StopCoroutine(ref m_IconHighlightCoroutine);

                    // Don't begin a new icon highlight coroutine; Allow the currently running coroutine to finish itself according to the m_Highlighted value
                    SetIconPressed();
                }
            }
        }

        bool m_Pressed;

        public bool highlighted
        {
            set
            {
                if (m_Highlighted == value || !gameObject.activeSelf)
                    return;

                this.StopCoroutine(ref m_IconHighlightCoroutine);

                m_Highlighted = value;
                if (m_Highlighted)
                {
                    // Only start the highlight coroutine if the highlight coroutine isnt already playing. Otherwise allow it to gracefully finish.
                    if (m_HighlightCoroutine == null)
                        m_HighlightCoroutine = StartCoroutine(Highlight());

                    if (hovered != null)
                        hovered();
                }
                else
                {
                    m_IconHighlightCoroutine = StartCoroutine(IconEndHighlight());
                }

                if (m_Highlighted)
                    this.ShowTooltip(this);
                else
                    this.HideTooltip(this);
            }

            get { return m_Highlighted; }
        }

        bool m_Highlighted;

        public bool semiTransparent
        {
            get { return m_SemiTransparent; }
            set
            {
                if (value == m_SemiTransparent || !gameObject.activeSelf)
                    return;

                m_SemiTransparent = value;

                this.RestartCoroutine(ref m_VisibilityCoroutine, AnimateSemiTransparent(value));
                PostReveal();
            }
        }

        bool m_SemiTransparent;

        public bool visible
        {
            set
            {
                if (value && m_Visible == value) // Allow false to fall through and perform hiding regardless of visibility
                    return;

                m_Visible = value;

                if (value)
                {
                    gameObject.SetActive(true);
                    m_MenuInset.localScale = m_HiddenInsetLocalScale;
                    m_Pressed = false;
                    m_Highlighted = false;
                    m_CanvasGroup.interactable = false;

                    this.RestartCoroutine(ref m_VisibilityCoroutine, AnimateShow());
                }
                else if (gameObject.activeSelf)
                    this.RestartCoroutine(ref m_VisibilityCoroutine, AnimateHide());
            }
        }

        bool m_Visible;

        public GradientPair gradientPair
        {
            set
            {
                s_GradientPair = value;
                m_BorderRendererMaterial.SetColor(k_MaterialColorTopProperty, value.a);
                m_BorderRendererMaterial.SetColor(k_MaterialColorBottomProperty, value.b);
            }
        }

        static GradientPair s_GradientPair;

        public Material borderRendererMaterial
        {
            get { return MaterialUtils.GetMaterialClone(m_BorderRenderer); } // return new unique color to the RadialMenuUI for settings in each RadialMenuSlot contained in a given RadialMenu
            set
            {
                m_BorderRendererMaterial = value;
                m_BorderRenderer.sharedMaterial = value;
            }
        }

        Material m_BorderRendererMaterial;

        GradientPair m_OriginalInsetGradientPair;
        Material m_InsetMaterial;
        Vector3 m_VisibleInsetLocalScale;
        Vector3 m_HiddenInsetLocalScale;
        Vector3 m_HighlightedInsetLocalScale;
        Vector3 m_OriginalIconLocalPosition;
        Vector3 m_IconHighlightedLocalPosition;
        Vector3 m_IconPressedLocalPosition;
        float m_IconLookForwardOffset = 0.5f;
        Vector3 m_IconLookDirection;
        Material m_FrameMaterial;
        Material m_IconMaterial;
        Color m_SemiTransparentFrameColor;

        Coroutine m_VisibilityCoroutine;
        Coroutine m_HighlightCoroutine;
        Coroutine m_IconHighlightCoroutine;
        Coroutine m_InsetRevealCoroutine;
        Coroutine m_RayExitDelayCoroutine;

        public string tooltipText
        {
            get { return tooltip != null ? tooltip.tooltipText : m_TooltipText; }
            set { m_TooltipText = value; }
        }

        string m_TooltipText;

        public Sprite icon
        {
            set { m_Icon.sprite = value; }
            get { return m_Icon.sprite; }
        }

        public Button button { get { return m_Button; } }

        public int orderIndex { get; set; }

        public static Quaternion hiddenLocalRotation { get; set; } // All menu slots share the same hidden location

        public Quaternion visibleLocalRotation { get; set; }

        // For overriding text (i.e. TransformActions)
        public ITooltip tooltip { private get; set; }

        public event Action hovered;

        void Awake()
        {
            m_InsetMaterial = MaterialUtils.GetMaterialClone(m_InsetMeshRenderer);
            m_IconMaterial = MaterialUtils.GetMaterialClone(m_Icon);
            m_OriginalInsetGradientPair = new GradientPair(m_InsetMaterial.GetColor(k_MaterialColorTopProperty), m_InsetMaterial.GetColor(k_MaterialColorBottomProperty));
            hiddenLocalRotation = transform.localRotation;
            m_VisibleInsetLocalScale = m_MenuInset.localScale;
            m_HighlightedInsetLocalScale = new Vector3(m_VisibleInsetLocalScale.x, m_VisibleInsetLocalScale.y * 1.2f, m_VisibleInsetLocalScale.z);
            m_VisibleInsetLocalScale = new Vector3(m_VisibleInsetLocalScale.x, m_MenuInset.localScale.y * 0.35f, m_VisibleInsetLocalScale.z);
            m_HiddenInsetLocalScale = new Vector3(m_VisibleInsetLocalScale.x * 0.5f, 0f, m_VisibleInsetLocalScale.z * 0.5f);

            m_OriginalIconLocalPosition = m_IconContainer.localPosition;
            m_IconHighlightedLocalPosition = m_OriginalIconLocalPosition + Vector3.up * k_IconHighlightedLocalYOffset;
            m_IconPressedLocalPosition = m_OriginalIconLocalPosition + Vector3.up * -k_IconHighlightedLocalYOffset;

            semiTransparent = false;
            m_FrameMaterial = MaterialUtils.GetMaterialClone(m_FrameRenderer);
            var frameMaterialColor = m_FrameMaterial.color;
            s_FrameOpaqueColor = new Color(frameMaterialColor.r, frameMaterialColor.g, frameMaterialColor.b, 1f);
            m_SemiTransparentFrameColor = new Color(s_FrameOpaqueColor.r, s_FrameOpaqueColor.g, s_FrameOpaqueColor.b, 0.5f);
        }

        void OnDisable()
        {
            this.StopCoroutine(ref m_VisibilityCoroutine);
            this.StopCoroutine(ref m_HighlightCoroutine);
            this.StopCoroutine(ref m_IconHighlightCoroutine);
        }

        void OnDestroy()
        {
            ObjectUtils.Destroy(m_InsetMaterial);
            ObjectUtils.Destroy(m_IconMaterial);
            ObjectUtils.Destroy(m_FrameMaterial);
        }

        public void CorrectIconRotation()
        {
            m_IconLookDirection = m_Icon.transform.position + transform.parent.forward * m_IconLookForwardOffset; // set a position offset above the icon, regardless of the icon's rotation
            m_IconContainer.LookAt(m_IconLookDirection);
            m_IconContainer.localEulerAngles = new Vector3(0f, m_IconContainer.localEulerAngles.y, 0f);
            var angle = m_IconContainer.localEulerAngles.y;
            m_IconContainer.localEulerAngles = new Vector3(0f, angle, 0f);
            m_TooltipTarget.localEulerAngles = new Vector3(90f, angle, 0f);

            var yaw = transform.localRotation.eulerAngles.y;
            tooltipAlignment = yaw > 90 && yaw <= 270 ? TextAlignment.Right : TextAlignment.Left;
        }

        IEnumerator AnimateShow()
        {
            m_InsetMaterial.SetFloat(k_MaterialAlphaProperty, 0);
            m_InsetMaterial.SetColor(k_MaterialColorTopProperty, m_OriginalInsetGradientPair.a);
            m_InsetMaterial.SetColor(k_MaterialColorBottomProperty, m_OriginalInsetGradientPair.b);
            m_FrameMaterial.SetColor(k_MaterialColorProperty, s_FrameOpaqueColor);
            m_BorderRendererMaterial.SetFloat(k_MaterialExpandProperty, 0);
            m_MenuInset.localScale = m_HiddenInsetLocalScale;
            transform.localScale = k_HiddenLocalScale;
            m_IconContainer.localPosition = m_OriginalIconLocalPosition;

            this.RestartCoroutine(ref m_InsetRevealCoroutine, ShowInset());

            var opacity = 0f;
            var positionWait = orderIndex * 0.05f;
            while (opacity < 1)
            {
                opacity += Time.deltaTime / positionWait * 2;
                var opacityShaped = Mathf.Pow(opacity, opacity);

                transform.localScale = Vector3.Lerp(k_HiddenLocalScale, Vector3.one, opacity);
                m_BorderRendererMaterial.SetFloat(k_MaterialExpandProperty, 1 - opacityShaped);
                CorrectIconRotation();
                yield return null;
            }

            transform.localScale = Vector3.one;
            m_BorderRendererMaterial.SetFloat(k_MaterialExpandProperty, 0);
            PostReveal();

            m_VisibilityCoroutine = null;
        }

        void PostReveal()
        {
            m_CanvasGroup.interactable = true;
            CorrectIconRotation();
        }

        IEnumerator ShowInset()
        {
            m_CanvasGroup.alpha = 0.0001f;

            var duration = 0f;
            var positionWait = (orderIndex + 1) * 0.075f;
            while (duration < 2)
            {
                duration += Time.deltaTime / positionWait * 2;
                var opacity = duration / 2;
                opacity *= opacity;
                m_CanvasGroup.alpha = Mathf.Clamp01(duration - 1);
                m_InsetMaterial.SetFloat(k_MaterialAlphaProperty, opacity);
                m_MenuInset.localScale = Vector3.Lerp(m_HiddenInsetLocalScale, m_VisibleInsetLocalScale, opacity);
                yield return null;
            }

            m_InsetMaterial.SetFloat(k_MaterialAlphaProperty, 1);
            m_MenuInset.localScale = m_VisibleInsetLocalScale;
            m_InsetRevealCoroutine = null;
        }

        IEnumerator AnimateHide()
        {
            this.HideTooltip(this);

            m_CanvasGroup.interactable = false;
            m_Pressed = false;
            m_Highlighted = false;

            var opacity = m_InsetMaterial.GetFloat(k_MaterialAlphaProperty);
            var opacityShaped = Mathf.Pow(opacity, opacity);
            while (opacity > 0)
            {
                var newScale = Vector3.one * opacity * opacityShaped * (opacity * 0.5f);
                transform.localScale = newScale;

                m_CanvasGroup.alpha = opacityShaped;
                m_BorderRendererMaterial.SetFloat(k_MaterialExpandProperty, opacityShaped);
                m_InsetMaterial.SetFloat(k_MaterialAlphaProperty, opacityShaped);
                m_MenuInset.localScale = Vector3.Lerp(m_HiddenInsetLocalScale, m_VisibleInsetLocalScale, opacityShaped);
                opacity -= Time.deltaTime * 1.5f;
                opacityShaped = Mathf.Pow(opacity, opacity);
                CorrectIconRotation();
                yield return null;
            }

            FadeOutCleanup();
            m_VisibilityCoroutine = null;
            gameObject.SetActive(false);
        }

        void FadeOutCleanup()
        {
            m_CanvasGroup.alpha = 0;
            m_InsetMaterial.SetColor(k_MaterialColorTopProperty, m_OriginalInsetGradientPair.a);
            m_InsetMaterial.SetColor(k_MaterialColorBottomProperty, m_OriginalInsetGradientPair.b);
            m_BorderRendererMaterial.SetFloat(k_MaterialExpandProperty, 1);
            m_InsetMaterial.SetFloat(k_MaterialAlphaProperty, 0);
            m_MenuInset.localScale = m_HiddenInsetLocalScale;
            CorrectIconRotation();
            transform.localScale = Vector3.zero;
            this.HideTooltip(this);
        }

        IEnumerator Highlight()
        {
            HighlightIcon();

            var opacity = Time.deltaTime;
            var topColor = m_OriginalInsetGradientPair.a;
            var bottomColor = m_OriginalInsetGradientPair.b;
            var initialFrameColor = m_FrameMaterial.color;
            var currentFrameColor = initialFrameColor;
            while (opacity > 0)
            {
                if (m_Highlighted)
                {
                    opacity = Mathf.Clamp01(opacity + Time.deltaTime * 4); // stay highlighted
                    currentFrameColor = Color.Lerp(initialFrameColor, s_FrameOpaqueColor, opacity);
                    m_FrameMaterial.SetColor(k_MaterialColorProperty, currentFrameColor);
                }
                else
                    opacity = Mathf.Clamp01(opacity - Time.deltaTime * 2);

                topColor = Color.Lerp(m_OriginalInsetGradientPair.a, s_GradientPair.a, opacity * 2f);
                bottomColor = Color.Lerp(m_OriginalInsetGradientPair.b, s_GradientPair.b, opacity);

                m_InsetMaterial.SetColor(k_MaterialColorTopProperty, topColor);
                m_InsetMaterial.SetColor(k_MaterialColorBottomProperty, bottomColor);

                if (!semiTransparent)
                    m_MenuInset.localScale = Vector3.Lerp(m_VisibleInsetLocalScale, m_HighlightedInsetLocalScale, opacity * opacity);

                yield return null;
            }

            m_BorderRendererMaterial.SetFloat(k_MaterialExpandProperty, 0);
            m_InsetMaterial.SetColor(k_MaterialColorTopProperty, m_OriginalInsetGradientPair.a);
            m_InsetMaterial.SetColor(k_MaterialColorBottomProperty, m_OriginalInsetGradientPair.b);

            m_HighlightCoroutine = null;
        }

        void HighlightIcon()
        {
            this.StopCoroutine(ref m_IconHighlightCoroutine);
            m_IconHighlightCoroutine = StartCoroutine(IconHighlightAnimatedShow());
        }

        void SetIconPressed()
        {
            this.StopCoroutine(ref m_IconHighlightCoroutine);
            m_IconHighlightCoroutine = StartCoroutine(IconHighlightAnimatedShow(true));
        }

        IEnumerator IconHighlightAnimatedShow(bool pressed = false)
        {
            var currentPosition = m_IconContainer.localPosition;
            var targetPosition = pressed == false ? m_IconHighlightedLocalPosition : m_IconPressedLocalPosition; // Raise up for highlight; lower for press
            var transitionAmount = Time.deltaTime;
            var transitionAddMultiplier = pressed == false ? 14 : 18; // Faster transition in for standard highlight; slower for pressed highlight
            while (transitionAmount < 1)
            {
                m_IconContainer.localPosition = Vector3.Lerp(currentPosition, targetPosition, transitionAmount);
                transitionAmount = transitionAmount + Time.deltaTime * transitionAddMultiplier * 2;
                yield return null;
            }

            m_IconContainer.localPosition = targetPosition;
            m_IconHighlightCoroutine = null;
        }

        IEnumerator IconEndHighlight()
        {
            var currentPosition = m_IconContainer.localPosition;
            var transitionAmount = 1f; // this should account for the magnitude difference between the highlightedYPositionOffset, and the current magnitude difference between the local Y and the original Y
            var transitionSubtractMultiplier = 5f;
            while (transitionAmount > 0)
            {
                m_IconContainer.localPosition = Vector3.Lerp(m_OriginalIconLocalPosition, currentPosition, transitionAmount);
                transitionAmount -= Time.deltaTime * transitionSubtractMultiplier;
                yield return null;
            }

            m_IconContainer.localPosition = m_OriginalIconLocalPosition;
            m_IconHighlightCoroutine = null;
        }

        IEnumerator AnimateSemiTransparent(bool makeSemiTransparent)
        {
            if (m_InsetRevealCoroutine != null)
            {
                // In case semiTransparency is triggered immediately upon showing the radial menu
                this.StopCoroutine(ref m_InsetRevealCoroutine);
                m_CanvasGroup.alpha = 1f;
                PostReveal();
            }

            const float kFasterMotionMultiplier = 2f;
            var transitionAmount = Time.deltaTime;
            var positionWait = (orderIndex + 4) * 0.25f; // pad the order index for a faster start to the transition
            var currentScale = transform.localScale;
            var targetScale = Vector3.one;
            var currentFrameColor = m_FrameMaterial.color;
            var transparentFrameColor = new Color(s_FrameOpaqueColor.r, s_FrameOpaqueColor.g, s_FrameOpaqueColor.b, 0f);
            var targetFrameColor = m_CanvasGroup.interactable ? (makeSemiTransparent ? m_SemiTransparentFrameColor : s_FrameOpaqueColor) : transparentFrameColor;
            var currentInsetAlpha = m_InsetMaterial.GetFloat(k_MaterialAlphaProperty);
            var targetInsetAlpha = makeSemiTransparent ? 0.25f : 1f;
            var currentIconColor = m_IconMaterial.GetColor(k_MaterialColorProperty);
            var targetIconColor = makeSemiTransparent ? m_SemiTransparentFrameColor : Color.white;
            var currentInsetScale = m_MenuInset.localScale;
            var targetInsetScale = makeSemiTransparent ? m_HighlightedInsetLocalScale * 4 : m_VisibleInsetLocalScale;
            var currentIconScale = m_IconContainer.localScale;
            var semiTransparentTargetIconScale = Vector3.one * 1.5f;
            var targetIconScale = makeSemiTransparent ? semiTransparentTargetIconScale : Vector3.one;
            while (transitionAmount < 1)
            {
                m_FrameMaterial.SetColor(k_MaterialColorProperty, Color.Lerp(currentFrameColor, targetFrameColor, transitionAmount * kFasterMotionMultiplier));
                m_MenuInset.localScale = Vector3.Lerp(currentInsetScale, targetInsetScale, transitionAmount * 2f);
                m_InsetMaterial.SetFloat(k_MaterialAlphaProperty, Mathf.Lerp(currentInsetAlpha, targetInsetAlpha, transitionAmount));
                m_IconMaterial.SetColor(k_MaterialColorProperty, Color.Lerp(currentIconColor, targetIconColor, transitionAmount));
                var shapedTransitionAmount = Mathf.Pow(transitionAmount, makeSemiTransparent ? 2 : 1) * kFasterMotionMultiplier;
                transform.localScale = Vector3.Lerp(currentScale, targetScale, shapedTransitionAmount);
                m_IconContainer.localScale = Vector3.Lerp(currentIconScale, targetIconScale, shapedTransitionAmount);
                transitionAmount += Time.deltaTime * positionWait * 3f;
                CorrectIconRotation();
                yield return null;
            }

            transform.localScale = targetScale;
            m_FrameMaterial.SetColor(k_MaterialColorProperty, targetFrameColor);
            m_InsetMaterial.SetFloat(k_MaterialAlphaProperty, targetInsetAlpha);
            m_IconMaterial.SetColor(k_MaterialColorProperty, targetIconColor);
            m_MenuInset.localScale = targetInsetScale;
            m_IconContainer.localScale = targetIconScale;
        }

        public void OnRayEnter(RayEventData eventData)
        {
            highlighted = true;
        }

        public void OnRayExit(RayEventData eventData)
        {
            highlighted = false;
        }
    }
}
