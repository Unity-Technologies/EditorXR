using System;
using System.Collections;
using System.Text;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Helpers;
using UnityEditor.Experimental.EditorVR.UI;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR.Menus
{
    sealed class ToolsMenuButton : MonoBehaviour, IToolsMenuButton, ITooltip, ITooltipPlacement, ISetTooltipVisibility
    {
        static Color s_FrameOpaqueColor;

        const float k_AlternateLocalScaleMultiplier = 0.85f; // Meets outer bounds of the radial menu
        const string k_MaterialColorProperty = "_Color";
        const string k_SelectionTooltipText = "Selection Tool (cannot be closed)";
        const string k_MainMenuTipText = "Main Menu";
        const string k_MaterialStencilRefProperty = "_StencilRef";
        readonly Vector3 k_ToolButtonActivePosition = new Vector3(0f, 0f, -0.035f);

        [SerializeField]
        GradientButton m_GradientButton;

        [SerializeField]
        Transform m_IconContainer;

        [SerializeField]
        Transform m_PrimaryUIContentContainer;

        [SerializeField]
        CanvasGroup m_IconContainerCanvasGroup;

        [SerializeField]
        SkinnedMeshRenderer m_FrameRenderer;

        [SerializeField]
        SkinnedMeshRenderer m_InsetMeshRenderer;

        [SerializeField]
        Renderer m_MaskRenderer;

        [SerializeField]
        Collider[] m_PrimaryButtonColliders;

        [SerializeField]
        GradientButton m_CloseButton;

        [SerializeField]
        CanvasGroup m_CloseButtonContainerCanvasGroup;

        [SerializeField]
        SkinnedMeshRenderer m_CloseInsetMeshRenderer;

        [SerializeField]
        SkinnedMeshRenderer m_CloseInsetMaskMeshRenderer;

        [SerializeField]
        Collider[] m_CloseButtonColliders; // disable for the main menu button & solitary primary tool button

        [SerializeField]
        Transform m_TooltipTarget;

        [SerializeField]
        Transform m_TooltipSource;

        [SerializeField]
        Vector3 m_AlternateLocalPosition;

        [SerializeField]
        Image m_ButtonIcon;

        Coroutine m_PositionCoroutine;
        Coroutine m_VisibilityCoroutine;
        Coroutine m_HighlightCoroutine;
        Coroutine m_ActivatorMoveCoroutine;
        Coroutine m_HoverCheckCoroutine;
        Coroutine m_SecondaryButtonVisibilityCoroutine;

        string m_TooltipText;
        string m_PreviewToolDescription;
        bool m_MoveToAlternatePosition;
        int m_Order = -1;
        Type m_PreviewToolType;
        Type m_ToolType;
        GradientPair m_GradientPair;
        Material m_FrameMaterial;
        Material m_InsetMaterial;
        Vector3 m_OriginalLocalPosition;
        Vector3 m_OriginalLocalScale;
        Material m_IconMaterial;
        Material m_MaskMaterial;
        Material m_CloseButtonMaskMaterial;
        Material m_CloseInsetMaterial;
        Vector3 m_OriginalIconContainerLocalScale;
        Sprite m_Icon;
        Sprite m_PreviewIcon;
        bool m_Highlighted;
        bool m_ActiveTool;
        bool m_ImplementsSecondaryButton;

        public Transform tooltipTarget
        {
            get { return m_TooltipTarget; }
            set { m_TooltipTarget = value; }
        }

        public Transform tooltipSource { get { return m_TooltipSource; } }

        public TextAlignment tooltipAlignment { get; private set; }
        public Transform rayOrigin { get; set; }
        public Node node { get; set; }
        public ITooltip tooltip { private get; set; } // Overrides text

        public bool isSelectionTool { get { return m_ToolType != null && m_ToolType == typeof(Tools.SelectionTool); } }

        public bool isMainMenu { get { return m_ToolType != null && m_ToolType == typeof(IMainMenu); } }

        public int activeButtonCount { get; set; }
        public int maxButtonCount { get; set; }
        public Transform menuOrigin { get; set; }

        public Action<Transform, Transform> openMenu { get; set; }
        public Action<Type> selectTool { get; set; }
        public Func<bool> closeButton { get; set; }
        public Action<Transform, int, bool> highlightSingleButton { get; set; }
        public Action<Transform> selectHighlightedButton { get; set; }

        public Vector3 toolButtonActivePosition { get { return k_ToolButtonActivePosition; } } // Shared active button offset from the alternate menu
        public Func<Type, int> visibleButtonCount { get; set; }

        public Action destroy { get { return DestroyButton; } }

        public Action<IToolsMenuButton> showAllButtons { private get; set; }
        public Action hoverExit { get; set; }

        public Type toolType
        {
            get { return m_ToolType; }

            set
            {
                m_ToolType = value;

                m_GradientButton.gameObject.SetActive(true);

                if (m_ToolType != null)
                {
                    gradientPair = UnityBrandColorScheme.saturatedSessionGradient;

                    if (isSelectionTool || isMainMenu)
                    {
                        tooltipText = isSelectionTool ? k_SelectionTooltipText : k_MainMenuTipText;
                        secondaryButtonCollidersEnabled = false;
                    }
                    else
                    {
                        tooltipText = toolType.Name;
                    }

                    isActiveTool = isActiveTool;
                    m_GradientButton.visible = true;
                }
                else
                {
                    m_GradientButton.visible = false;
                    gradientPair = UnityBrandColorScheme.grayscaleSessionGradient;
                }
            }
        }

        public int order
        {
            get { return m_Order; }
            set
            {
                m_Order = value; // Position of this button in relation to other tool buttons

                highlighted = false;

                this.RestartCoroutine(ref m_PositionCoroutine, AnimatePosition(m_Order));

                if (m_Order == -1)
                    this.HideTooltip(this);
            }
        }

        public bool implementsSecondaryButton
        {
            get { return m_ImplementsSecondaryButton; }
            set
            {
                m_ImplementsSecondaryButton = value;

                if (!value)
                {
                    foreach (var collider in m_CloseButtonColliders)
                    {
                        collider.enabled = false;
                    }
                }
            }
        }

        /// <summary>
        /// GradientPair should be set with new random gradientPair each time a new Tool is associated with this Button
        /// This gradientPair is also used to highlight the input device when appropriate
        /// </summary>
        public GradientPair gradientPair
        {
            get { return m_GradientPair; }
            set { m_GradientPair = value; }
        }

        /// <summary>
        /// Type, that if not null, denotes that preview-mode is enabled
        /// This is enabled when highlighting a tool on the main menu
        /// </summary>
        public Type previewToolType
        {
            private get { return m_PreviewToolType; }
            set
            {
                m_PreviewToolType = value;

                if (m_PreviewToolType != null) // Show the highlight if the preview type is valid; hide otherwise
                {
                    var tempToolGo = ObjectUtils.AddComponent(m_PreviewToolType, gameObject);
                    var tempTool = tempToolGo as ITool;
                    if (tempTool != null)
                    {
                        var iMenuIcon = tempTool as IMenuIcon;
                        if (iMenuIcon != null)
                            previewIcon = iMenuIcon.icon;

                        ObjectUtils.Destroy(tempToolGo);
                    }

                    // Show the grayscale highlight when previewing a tool on this button
                    m_GradientButton.highlightGradientPair = UnityBrandColorScheme.saturatedSessionGradient; // UnityBrandColorScheme.grayscaleSessionGradient;

                    if (!previewIcon)
                        m_GradientButton.SetContent(GetTypeAbbreviation(m_PreviewToolType));
                }
                else
                {
                    previewToolDescription = null; // Clear the preview tooltip
                    isActiveTool = isActiveTool; // Set active tool back to pre-preview state
                    icon = icon; // Gradient button will set its icon back to that representing the current tool, if one existed before previewing new tool type in this button
                    m_GradientButton.highlightGradientPair = gradientPair;
                }

                m_GradientButton.highlighted = m_PreviewToolType != null;
            }
        }

        public string previewToolDescription
        {
            get { return m_PreviewToolDescription; }
            set
            {
                if (value != null)
                {
                    m_PreviewToolDescription = value;
                    this.ShowTooltip(this);
                }
                else
                {
                    m_PreviewToolDescription = null;
                    tooltipVisible = false;
                }
            }
        }

        public string tooltipText
        {
            get
            {
                if (!interactable && toolType == typeof(IMainMenu))
                    return "Main Menu hidden";

                return tooltip != null ? tooltip.tooltipText : (previewToolType == null ? m_TooltipText : previewToolDescription);
            }

            private set { m_TooltipText = value; }
        }

        public bool isActiveTool
        {
            private get { return m_ActiveTool; }
            set
            {
                m_ActiveTool = value;

                m_GradientButton.normalGradientPair = m_ActiveTool ? gradientPair : UnityBrandColorScheme.darkGrayscaleSessionGradient;
                m_GradientButton.highlightGradientPair = m_ActiveTool ? UnityBrandColorScheme.darkGrayscaleSessionGradient : gradientPair;

                m_GradientButton.highlighted = true;
                m_GradientButton.highlighted = false;
            }
        }

        public bool highlighted
        {
            get { return m_Highlighted; }
            set
            {
                if (m_Highlighted == value)
                    return;

                m_Highlighted = value;
                m_GradientButton.highlighted = m_Highlighted;

                if (!m_Highlighted)
                    this.HideTooltip(this);
                else
                    this.ShowTooltip(this);

                if (implementsSecondaryButton && (!isMainMenu || !isSelectionTool))
                {
                    // This show/hide functionality utilized by spatial scrolling
                    if (m_Highlighted)
                        this.RestartCoroutine(ref m_SecondaryButtonVisibilityCoroutine, ShowSecondaryButton());
                    else
                        this.RestartCoroutine(ref m_SecondaryButtonVisibilityCoroutine, HideSecondaryButton());
                }
            }
        }

        public bool interactable
        {
            get { return m_GradientButton.interactable; }
            set { m_GradientButton.interactable = value; }
        }

        public bool secondaryButtonHighlighted { get { return m_CloseButton.highlighted; } }

        public bool tooltipVisible
        {
            set
            {
                if (!value)
                    this.HideTooltip(this);
            }
        }

        bool primaryButtonCollidersEnabled
        {
            set
            {
                foreach (var collider in m_PrimaryButtonColliders)
                {
                    collider.enabled = value;
                }
            }
        }

        bool secondaryButtonCollidersEnabled
        {
            set
            {
                // Prevent secondary button colliders from being enabled on ToolsMenuButtons without a secondary button
                if (!implementsSecondaryButton)
                    return;

                foreach (var collider in m_CloseButtonColliders)
                {
                    collider.enabled = value;
                }
            }
        }

        public Sprite icon
        {
            private get { return m_Icon; }
            set
            {
                m_PreviewIcon = null; // clear any cached preview icons
                m_Icon = value;

                if (m_Icon)
                    m_GradientButton.SetContent(m_Icon);
                else
                    m_GradientButton.SetContent(GetTypeAbbreviation(m_ToolType)); // Set backup tool abbreviation if no icon is set
            }
        }

        public Sprite previewIcon
        {
            get { return m_PreviewIcon; }
            set
            {
                m_PreviewIcon = value;
                m_GradientButton.SetContent(m_PreviewIcon);
            }
        }

        public bool moveToAlternatePosition
        {
            get { return m_MoveToAlternatePosition; }
            set
            {
                if (m_MoveToAlternatePosition == value)
                    return;

                m_MoveToAlternatePosition = value;

                this.StopCoroutine(ref m_ActivatorMoveCoroutine);

                m_ActivatorMoveCoroutine = StartCoroutine(AnimateMoveActivatorButton(m_MoveToAlternatePosition));
            }
        }

        bool visible { set { this.RestartCoroutine(ref m_VisibilityCoroutine, AnimateVisibility(value)); } }

        public Vector3 primaryUIContentContainerLocalScale
        {
            get { return m_PrimaryUIContentContainer.localScale; }
            set { m_PrimaryUIContentContainer.localScale = value; }
        }

        public float iconHighlightedLocalZOffset { set { m_GradientButton.iconHighlightedLocalZOffset = value; } }


        // All buttons in a given menu share the same stencil ID which is fetched in the UI, then assigned to each button in the same menu
        public byte stencilRef { get; set; }

        public event Action hovered;

        void Awake()
        {
            m_OriginalLocalPosition = transform.localPosition;
            m_OriginalLocalScale = transform.localScale;
            m_FrameMaterial = MaterialUtils.GetMaterialClone(m_FrameRenderer);
            var frameMaterialColor = m_FrameMaterial.color;
            s_FrameOpaqueColor = new Color(frameMaterialColor.r, frameMaterialColor.g, frameMaterialColor.b, 1f);
            m_FrameMaterial.SetColor(k_MaterialColorProperty, s_FrameOpaqueColor);
            m_IconMaterial = MaterialUtils.GetMaterialClone(m_ButtonIcon);
            m_OriginalIconContainerLocalScale = m_IconContainer.localScale;
        }

        void Start()
        {
            if (m_ToolType == null)
                m_GradientButton.gameObject.SetActive(false);

            tooltipAlignment = TextAlignment.Center;

            m_GradientButton.hoverEnter += OnBackgroundHoverEnter; // Display the foreground button actions
            m_GradientButton.hoverExit += OnActionButtonHoverExit;
            m_GradientButton.click += OnBackgroundButtonClick;

            m_FrameRenderer.SetBlendShapeWeight(1, 0f);
            m_CloseInsetMeshRenderer.SetBlendShapeWeight(0, 100f);
            m_CloseInsetMaskMeshRenderer.SetBlendShapeWeight(0, 100f);

            m_CloseButton.hoverEnter += OnBackgroundHoverEnter; // Display the foreground button actions
            m_CloseButton.hoverExit += OnActionButtonHoverExit;
            m_CloseButton.click += OnSecondaryButtonClicked;
            m_CloseButtonContainerCanvasGroup.alpha = 0f;

            // These materials have already been cloned when instantiating this button
            m_InsetMaterial = m_InsetMeshRenderer.sharedMaterial;
            m_CloseInsetMaterial = m_CloseInsetMeshRenderer.sharedMaterial;
            // These materials have NOT been cloned when instantiating this button
            m_MaskMaterial = MaterialUtils.GetMaterialClone(m_MaskRenderer);
            m_CloseButtonMaskMaterial = MaterialUtils.GetMaterialClone(m_CloseInsetMaskMeshRenderer);

            m_InsetMaterial.SetInt(k_MaterialStencilRefProperty, stencilRef);
            m_MaskMaterial.SetInt(k_MaterialStencilRefProperty, stencilRef);
            m_CloseInsetMaterial.SetInt(k_MaterialStencilRefProperty, stencilRef);
            m_CloseButtonMaskMaterial.SetInt(k_MaterialStencilRefProperty, stencilRef);
        }

        void OnDestroy()
        {
            ObjectUtils.Destroy(m_InsetMaterial);
            ObjectUtils.Destroy(m_IconMaterial);
            ObjectUtils.Destroy(m_CloseInsetMaterial);
            ObjectUtils.Destroy(m_CloseButtonMaskMaterial);
            ObjectUtils.Destroy(m_FrameMaterial);

            this.StopCoroutine(ref m_PositionCoroutine);
            this.StopCoroutine(ref m_VisibilityCoroutine);
            this.StopCoroutine(ref m_HighlightCoroutine);
            this.StopCoroutine(ref m_ActivatorMoveCoroutine);
            this.StopCoroutine(ref m_HoverCheckCoroutine);
            this.StopCoroutine(ref m_SecondaryButtonVisibilityCoroutine);
        }

        void DestroyButton()
        {
            this.RestartCoroutine(ref m_VisibilityCoroutine, AnimateHideAndDestroy());
        }

        static string GetTypeAbbreviation(Type type)
        {
            // Create periodic table-style names for types
            var abbreviation = new StringBuilder();
            foreach (var ch in type.Name.ToCharArray())
            {
                if (char.IsUpper(ch))
                    abbreviation.Append(abbreviation.Length > 0 ? char.ToLower(ch) : ch);

                if (abbreviation.Length >= 2)
                    break;
            }

            return abbreviation.ToString();
        }

        void OnBackgroundHoverEnter()
        {
            if (hovered != null) // Raised in order to trigger the haptic in the Tools Menu
                hovered();

            if (isMainMenu)
            {
                m_GradientButton.highlighted = true;
                return;
            }

            if (implementsSecondaryButton)
                this.RestartCoroutine(ref m_SecondaryButtonVisibilityCoroutine, ShowSecondaryButton());

            showAllButtons(this);
        }

        void OnActionButtonHoverExit()
        {
            ActionButtonHoverExit();
        }

        void ActionButtonHoverExit()
        {
            if (m_PositionCoroutine != null)
                return;

            if (isMainMenu)
            {
                m_GradientButton.highlighted = false;
                return;
            }

            if (!m_CloseButton.highlighted)
                this.RestartCoroutine(ref m_SecondaryButtonVisibilityCoroutine, HideSecondaryButton());

            hoverExit();
        }

        void OnBackgroundButtonClick()
        {
            if (!interactable)
                return;

            selectTool(toolType);

            if (!isMainMenu)
                ActionButtonHoverExit();

            m_GradientButton.UpdateMaterialColors();
        }

        void OnSecondaryButtonClicked()
        {
            if (!implementsSecondaryButton)
                return;

            this.RestartCoroutine(ref m_VisibilityCoroutine, AnimateHideAndDestroy());
            closeButton();
            ActionButtonHoverExit();
        }

        IEnumerator AnimateHideAndDestroy()
        {
            this.StopCoroutine(ref m_PositionCoroutine);
            this.StopCoroutine(ref m_HighlightCoroutine);
            this.StopCoroutine(ref m_ActivatorMoveCoroutine);
            this.StopCoroutine(ref m_HoverCheckCoroutine);
            this.StopCoroutine(ref m_SecondaryButtonVisibilityCoroutine);

            this.HideTooltip(this);
            const int kDurationScalar = 3;
            var duration = 0f;
            var currentScale = transform.localScale;
            var targetScale = Vector3.zero;
            while (duration < 1)
            {
                var durationShaped = Mathf.Pow(MathUtilsExt.SmoothInOutLerpFloat(duration += Time.unscaledDeltaTime * kDurationScalar), 4);
                transform.localScale = Vector3.Lerp(currentScale, targetScale, durationShaped);
                yield return null;
            }

            transform.localScale = targetScale;
            m_VisibilityCoroutine = null;
            ObjectUtils.Destroy(gameObject, 0.1f);
        }

        IEnumerator AnimateVisibility(bool show = true)
        {
            const float kSpeedScalar = 8f;
            var targetPosition = show ? (moveToAlternatePosition ? m_AlternateLocalPosition : m_OriginalLocalPosition) : Vector3.zero;
            var targetScale = show ? (moveToAlternatePosition ? m_OriginalLocalScale : m_OriginalLocalScale * k_AlternateLocalScaleMultiplier) : Vector3.zero;
            var currentPosition = transform.localPosition;
            var currentIconScale = m_IconContainer.localScale;
            var targetIconContainerScale = show ? m_OriginalIconContainerLocalScale : Vector3.zero;
            var transitionAmount = 0f;
            var currentScale = transform.localScale;
            while (transitionAmount < 1)
            {
                var shapedAmount = MathUtilsExt.SmoothInOutLerpFloat(transitionAmount += Time.unscaledDeltaTime * kSpeedScalar);
                m_IconContainer.localScale = Vector3.Lerp(currentIconScale, targetIconContainerScale, shapedAmount);
                transform.localPosition = Vector3.Lerp(currentPosition, targetPosition, shapedAmount);
                transform.localScale = Vector3.Lerp(currentScale, targetScale, shapedAmount);
                yield return null;
            }

            m_IconContainer.localScale = targetIconContainerScale;
            transform.localScale = targetScale;
            transform.localPosition = targetPosition;
            m_VisibilityCoroutine = null;
        }

        IEnumerator AnimatePosition(int orderPosition)
        {
            primaryButtonCollidersEnabled = false;
            secondaryButtonCollidersEnabled = false;

            this.RestartCoroutine(ref m_SecondaryButtonVisibilityCoroutine, HideSecondaryButton());

            visible = orderPosition != -1;

            const float kTimeScalar = 6f;
            const float kCenterLocationAmount = 0.5f;
            const float kCircularRange = 360f;
            const int kDurationShapeAmount = 3;
            var rotationSpacing = kCircularRange / maxButtonCount; // dividend should be the count of tool buttons showing at this time

            // Center the MainMenu & Active tool buttons at the bottom of the RadialMenu
            var phaseOffset = orderPosition > -1 ? rotationSpacing * kCenterLocationAmount - (visibleButtonCount(m_ToolType) * kCenterLocationAmount) * rotationSpacing : 0;
            var targetRotation = orderPosition > -1 ? Quaternion.AngleAxis(phaseOffset + rotationSpacing * Mathf.Max(0f, orderPosition), Vector3.down) : Quaternion.identity;

            var duration = 0f;
            var currentCanvasAlpha = m_IconContainerCanvasGroup.alpha;
            var targetCanvasAlpha = orderPosition > -1 ? 1f : 0f;
            var currentRotation = transform.localRotation;
            var positionWait = 1f;
            while (duration < 1)
            {
                duration += Time.unscaledDeltaTime * kTimeScalar * positionWait;
                var durationShaped = Mathf.Pow(MathUtilsExt.SmoothInOutLerpFloat(duration), kDurationShapeAmount);
                transform.localRotation = Quaternion.Lerp(currentRotation, targetRotation, durationShaped);
                m_IconContainerCanvasGroup.alpha = Mathf.Lerp(currentCanvasAlpha, targetCanvasAlpha, durationShaped);
                CorrectIconRotation();
                yield return null;
            }

            transform.localRotation = targetRotation;
            CorrectIconRotation();
            primaryButtonCollidersEnabled = orderPosition > -1;
            secondaryButtonCollidersEnabled = orderPosition > -1;
            m_PositionCoroutine = null;

            if (implementsSecondaryButton && orderPosition > -1 && m_GradientButton.highlighted)
                this.RestartCoroutine(ref m_SecondaryButtonVisibilityCoroutine, ShowSecondaryButton());
        }

        IEnumerator AnimateMoveActivatorButton(bool moveToAlternatePosition = true)
        {
            const float kSpeedDecreaseScalar = 0.275f;
            var amount = 0f;
            var currentPosition = transform.localPosition;
            var targetPosition = moveToAlternatePosition ? m_AlternateLocalPosition : m_OriginalLocalPosition;
            var currentLocalScale = transform.localScale;
            var targetLocalScale = moveToAlternatePosition ? m_OriginalLocalScale : m_OriginalLocalScale * k_AlternateLocalScaleMultiplier;
            var speed = moveToAlternatePosition ? 5f : 4.5f; // Perform faster is returning to original position
            speed += (order + 1) * kSpeedDecreaseScalar;
            while (amount < 1f)
            {
                var shapedAmount = MathUtilsExt.SmoothInOutLerpFloat(amount += Time.unscaledDeltaTime * speed);
                transform.localPosition = Vector3.Lerp(currentPosition, targetPosition, shapedAmount);
                transform.localScale = Vector3.Lerp(currentLocalScale, targetLocalScale, shapedAmount);
                yield return null;
            }

            transform.localPosition = targetPosition;
            transform.localScale = targetLocalScale;
            m_ActivatorMoveCoroutine = null;
        }

        void CorrectIconRotation()
        {
            const float kIconLookForwardOffset = 0.5f;
            var iconLookDirection = m_IconContainer.transform.position + transform.forward * kIconLookForwardOffset; // set a position offset above the icon, regardless of the icon's rotation
            m_IconContainer.LookAt(iconLookDirection);
            m_IconContainer.localEulerAngles = new Vector3(0f, 0f, m_IconContainer.localEulerAngles.z);
        }

        IEnumerator ShowSecondaryButton()
        {
            // Don't perform additional animated visuals if already in a fully revealed state
            if (Mathf.Approximately(m_CloseButtonContainerCanvasGroup.alpha, 1f))
            {
                m_SecondaryButtonVisibilityCoroutine = null;
                yield break;
            }

            const float kSecondaryButtonFrameVisibleBlendShapeWeight = 16f; // The extra amount of the frame to show on hover-before the full reveal of the secondary button
            const float kTargetDuration = 1f;
            const int kIntroDurationMultiplier = 10;
            var currentVisibilityAmount = m_FrameRenderer.GetBlendShapeWeight(1);
            var currentDuration = 0f;
            while (currentDuration < kTargetDuration)
            {
                var shapedAmount = MathUtilsExt.SmoothInOutLerpFloat(currentDuration += Time.unscaledDeltaTime * kIntroDurationMultiplier);
                m_FrameRenderer.SetBlendShapeWeight(1, Mathf.Lerp(currentVisibilityAmount, kSecondaryButtonFrameVisibleBlendShapeWeight, shapedAmount));
                yield return null;
            }

            const float kDelayBeforeSecondaryButtonReveal = 0.25f;
            currentDuration = 0f; // Reset current duration
            while (currentDuration < kDelayBeforeSecondaryButtonReveal)
            {
                currentDuration += Time.unscaledDeltaTime;
                yield return null;
            }

            const float kFrameSecondaryButtonVisibleBlendShapeWeight = 61f;
            const float kSecondaryButtonVisibleBlendShapeWeight = 46f;
            const int kDurationMultiplier = 25;

            this.StopCoroutine(ref m_HighlightCoroutine);

            var currentSecondaryButtonVisibilityAmount = m_CloseInsetMeshRenderer.GetBlendShapeWeight(0);
            var currentSecondaryCanvasGroupAlpha = m_CloseButtonContainerCanvasGroup.alpha;
            currentVisibilityAmount = m_FrameRenderer.GetBlendShapeWeight(1);
            currentDuration = 0f;
            while (currentDuration < 1f)
            {
                var shapedAmount = MathUtilsExt.SmoothInOutLerpFloat(currentDuration += Time.unscaledDeltaTime * kDurationMultiplier);
                m_FrameRenderer.SetBlendShapeWeight(1, Mathf.Lerp(currentVisibilityAmount, kFrameSecondaryButtonVisibleBlendShapeWeight, shapedAmount));
                m_CloseInsetMeshRenderer.SetBlendShapeWeight(0, Mathf.Lerp(currentSecondaryButtonVisibilityAmount, kSecondaryButtonVisibleBlendShapeWeight, shapedAmount));
                m_CloseInsetMaskMeshRenderer.SetBlendShapeWeight(0, Mathf.Lerp(currentSecondaryButtonVisibilityAmount, kSecondaryButtonVisibleBlendShapeWeight, shapedAmount));
                m_CloseButtonContainerCanvasGroup.alpha = Mathf.Lerp(currentSecondaryCanvasGroupAlpha, 1f, shapedAmount);
                yield return null;
            }

            m_SecondaryButtonVisibilityCoroutine = null;
        }

        IEnumerator HideSecondaryButton()
        {
            const float kMaxDelayDuration = 0.125f;
            var delayDuration = 0f;
            while (delayDuration < kMaxDelayDuration)
            {
                delayDuration += Time.unscaledDeltaTime;
                yield return null;
            }

            const float kSecondaryButtonHiddenBlendShapeWeight = 100f;
            const int kDurationMultiplier = 12;
            var currentVisibilityAmount = m_FrameRenderer.GetBlendShapeWeight(1);
            var currentSecondaryButtonVisibilityAmount = m_CloseInsetMeshRenderer.GetBlendShapeWeight(0);
            var currentSecondaryCanvasGroupAlpha = m_CloseButtonContainerCanvasGroup.alpha;
            var amount = 0f;
            while (amount < 1f)
            {
                yield return null;

                if (m_CloseButton.highlighted)
                {
                    m_SecondaryButtonVisibilityCoroutine = null;
                    yield break;
                }

                this.StopCoroutine(ref m_HighlightCoroutine);

                var shapedAmount = MathUtilsExt.SmoothInOutLerpFloat(amount += Time.unscaledDeltaTime * kDurationMultiplier);
                m_FrameRenderer.SetBlendShapeWeight(1, Mathf.Lerp(currentVisibilityAmount, 0f, shapedAmount));
                m_CloseInsetMeshRenderer.SetBlendShapeWeight(0, Mathf.Lerp(currentSecondaryButtonVisibilityAmount, kSecondaryButtonHiddenBlendShapeWeight, shapedAmount));
                m_CloseInsetMaskMeshRenderer.SetBlendShapeWeight(0, Mathf.Lerp(currentSecondaryButtonVisibilityAmount, kSecondaryButtonHiddenBlendShapeWeight, shapedAmount));
                m_CloseButtonContainerCanvasGroup.alpha = Mathf.Lerp(currentSecondaryCanvasGroupAlpha, 0f, shapedAmount);
            }

            m_SecondaryButtonVisibilityCoroutine = null;
        }
    }
}
