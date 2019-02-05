#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEditor.Experimental.EditorVR.Tools;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Menus
{
    sealed class ToolsMenuUI : MonoBehaviour, IUsesViewerScale, IInstantiateUI,
        IConnectInterfaces, IControlSpatialHinting, IUsesRayOrigin
    {
        const int k_MenuButtonOrderPosition = 0; // Menu button position used in this particular ToolButton implementation
        const int k_ActiveToolOrderPosition = 1; // Active-tool button position used in this particular ToolButton implementation
        const int k_InactiveButtonInitialOrderPosition = -1;
        const float k_RaySelectIconHighlightedZOffset = -0.0075f;
        const float k_SpatialSelectIconHighlightedZOffset = -0.02f;

        [SerializeField]
        Transform m_ButtonContainer;

        [Header("Used when displaying Alternate Menu")]
        [SerializeField]
        Vector3 m_AlternatePosition;

        [SerializeField]
        Vector3 m_AlternateLocalScale;

        [SerializeField]
        Transform m_ButtonTooltipTarget;

        bool m_AllButtonsVisible;
        List<IToolsMenuButton> m_OrderedButtons;
        Coroutine m_ShowHideAllButtonsCoroutine;
        Coroutine m_MoveCoroutine;
        Coroutine m_ButtonHoverExitDelayCoroutine;
        int m_VisibleButtonCount;
        bool m_MoveToAlternatePosition;
        Vector3 m_OriginalLocalScale;
        bool m_RayHovered;
        float m_SpatialDragDistance;
        Quaternion m_HintContentContainerInitialRotation;
        Vector3 m_HintContentWorldPosition;
        Vector3 m_DragTarget;

        public int maxButtonCount { get; set; }

        public Transform buttonContainer { get { return m_ButtonContainer; } }

        public Transform rayOrigin { private get; set; }
        public Action<Transform> mainMenuActivatorSelected { get; set; }

        public List<IToolsMenuButton> buttons { get { return m_OrderedButtons; } }

        public bool allButtonsVisible
        {
            get { return m_AllButtonsVisible; }
            set
            {
                m_AllButtonsVisible = value;

                if (m_AllButtonsVisible)
                {
                    this.StopCoroutine(ref m_ShowHideAllButtonsCoroutine);
                    ShowAllExceptMenuButton();
                }
                else
                {
                    ShowOnlyMenuAndActiveToolButtons();
                    spatiallyScrolling = false;
                    this.SetSpatialHintShowHideRotationTarget(Vector3.zero);
                }
            }
        }

        public bool moveToAlternatePosition
        {
            set
            {
                if (m_MoveToAlternatePosition == value)
                    return;

                m_MoveToAlternatePosition = value;
                var newPosition = m_MoveToAlternatePosition ? m_AlternatePosition : Vector3.zero;
                var newScale = m_MoveToAlternatePosition ? m_AlternateLocalScale : m_OriginalLocalScale;
                this.RestartCoroutine(ref m_MoveCoroutine, MoveToLocation(newPosition, newScale));
            }
        }

        bool aboveMinimumButtonCount
        {
            get
            {
                const int kSelectionToolButtonHideCount = 2;
                var count = m_OrderedButtons.Count;
                var aboveMinCount = count > kSelectionToolButtonHideCount; // Has at least one tool been added beyond the default MainMenu & SelectionTool

                // Prevent the display of the SelectionTool button, if only the MainMenu and SelectionTool buttons reside in the buttons collection
                if (count == kSelectionToolButtonHideCount)
                    aboveMinCount = buttons.All(x => x.toolType != typeof(SelectionTool));

                return aboveMinCount;
            }
        }

        public bool spatiallyScrolling
        {
            set
            {
                for (int i = 0; i < m_OrderedButtons.Count; ++i)
                {
                    var button = m_OrderedButtons[i];
                    button.iconHighlightedLocalZOffset = value ? k_SpatialSelectIconHighlightedZOffset : k_RaySelectIconHighlightedZOffset;
                }

                if (value)
                {
                    var currentRotation = transform.rotation.eulerAngles;
                    m_SpatialDragDistance = 0f;
                    m_HintContentContainerInitialRotation = Quaternion.Euler(0f, currentRotation.y, 0f);
                    m_HintContentWorldPosition = transform.position;
                    this.SetSpatialHintPosition(m_HintContentWorldPosition);
                }
            }
        }

        public float spatialDragDistance { set { m_SpatialDragDistance = value; } }

        public Vector3? startingDragOrigin
        {
            set
            {
                if (value != null)
                    this.SetSpatialHintLookAtRotation(value.Value);
            }
        }

        public event Action buttonHovered;
        public event Action buttonClicked;
        public event Action<Transform, Type> buttonSelected;
        public event Action closeMenu;

        void Awake()
        {
            m_OriginalLocalScale = transform.localScale;
            m_OrderedButtons = new List<IToolsMenuButton>();
        }

        void Update()
        {
            var newHintContainerRotation = m_HintContentContainerInitialRotation;

            // Perform activation of visuals after the user has dragged beyond the initial drag trigger threshold
            // The drag distance is a 0-1 lerped value, based off of the origin to trigger magnitude
            if (m_SpatialDragDistance >= 1f && m_SpatialDragDistance < 2)
            {
                if (Mathf.Approximately(m_SpatialDragDistance, 1f))
                {
                    m_DragTarget = transform.position; // Cache the initial drag target position, before performing any extra shaping to the target Vec3
                    this.SetSpatialHintState(SpatialHintModule.SpatialHintStateFlags.Scrolling);
                }

                // Follow the user's input for a short additional period of time
                // Update the dragTarget with the current device position, to allow for visuals to better match the expected rotation/position
                m_DragTarget = transform.position;
                this.SetSpatialHintDragThresholdTriggerPosition(transform.position);
                this.SetSpatialHintContainerRotation(newHintContainerRotation);

                // Perform a smooth lerp of the hint contents after dragging beyond the distance trigger threshold
                m_SpatialDragDistance += Time.unscaledDeltaTime * 8; // Continue to increase the amount
            }
            else if (m_AllButtonsVisible && m_SpatialDragDistance > 2)
            {
                this.SetSpatialHintDragThresholdTriggerPosition(transform.position);
                this.SetSpatialHintContainerRotation(newHintContainerRotation);
                this.SetSpatialHintShowHideRotationTarget(m_DragTarget);
            }
        }

        public void AddButton(IToolsMenuButton button, Transform buttonTransform)
        {
            button.interactable = true;
            button.showAllButtons = ShowAllButtons;
            button.hoverExit = ButtonHoverExitPerformed;
            button.maxButtonCount = maxButtonCount;
            button.selectTool = SelectExistingToolTypeFromButton;
            button.closeButton = DeleteHighlightedButton;
            button.visibleButtonCount = VisibleButtonCount; // allow buttons to fetch local buttonCount
            button.iconHighlightedLocalZOffset = k_RaySelectIconHighlightedZOffset;
            button.tooltipTarget = m_ButtonTooltipTarget;
            button.hovered += OnButtonHover;

            bool allowSecondaryButton = false; // Secondary button is the close button
            var insertPosition = k_MenuButtonOrderPosition;
            if (!IsMainMenuButton(button))
            {
                insertPosition = k_ActiveToolOrderPosition;
                allowSecondaryButton = !IsSelectionToolButton(button);
            }

            var initializingButtons = m_OrderedButtons.Count == 1;
            m_OrderedButtons.Insert(insertPosition, button);

            // If only the MainMenu & SelectionTool buttons exist, set visible button count to 1
            m_VisibleButtonCount = aboveMinimumButtonCount || initializingButtons ? m_OrderedButtons.Count : 1;
            button.implementsSecondaryButton = allowSecondaryButton;
            button.isActiveTool = true;
            button.order = insertPosition;

            buttonTransform.rotation = Quaternion.identity;
            buttonTransform.localPosition = Vector3.zero;
            buttonTransform.localScale = Vector3.zero;

            if (aboveMinimumButtonCount) // aboveMinimumCount will change throughout function, don't cache for re-use
                this.RestartCoroutine(ref m_ShowHideAllButtonsCoroutine, ShowThenHideAllButtons(1.25f, false));
            else
                SetupButtonOrder(); // Setup the MainMenu and active tool buttons only
        }

        IEnumerator ShowThenHideAllButtons(float delayBeforeHiding = 1.25f, bool showMenuButton = true)
        {
            if (showMenuButton)
                SetupButtonOrder();
            else
                ShowAllExceptMenuButton();

            if (delayBeforeHiding > 0)
            {
                var duration = Time.unscaledDeltaTime;
                while (duration < delayBeforeHiding)
                {
                    duration += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            // Hide all but menu and active tool buttons after visually adding new button
            allButtonsVisible = false;
            m_ShowHideAllButtonsCoroutine = null;
        }

        void Reinsert(IToolsMenuButton button, int newOrderPosition, bool updateButtonOrder = false)
        {
            var removed = m_OrderedButtons.Remove(button);
            if (!removed)
                return;

            m_OrderedButtons.Insert(newOrderPosition, button);

            if (updateButtonOrder)
                button.order = newOrderPosition;
        }

        void SetupButtonOrder()
        {
            for (int i = 0; i < m_OrderedButtons.Count; ++i)
            {
                var button = m_OrderedButtons[i];
                button.isActiveTool = i == k_ActiveToolOrderPosition;

                // Allow settings of regular button order if there are more buttons that just the MainMenu & SelectionTool
                button.order = aboveMinimumButtonCount || IsMainMenuButton(button) ? i : k_InactiveButtonInitialOrderPosition;
            }
        }

        void ShowAllExceptMenuButton()
        {
            // The MainMenu button will be hidden, subtract 1 from the m_VisibleButtonCount
            m_VisibleButtonCount = Mathf.Max(0, m_OrderedButtons.Count - 1);
            for (int i = 0; i < m_OrderedButtons.Count; ++i)
            {
                var button = m_OrderedButtons[i];
                button.isActiveTool = i == k_ActiveToolOrderPosition;
                button.order = i == k_MenuButtonOrderPosition ? k_InactiveButtonInitialOrderPosition : i - 1; // Hide the menu buttons when revealing all tools buttons
            }
        }

        void ShowOnlyMenuAndActiveToolButtons()
        {
            if (!aboveMinimumButtonCount) // If only the Selection and MainMenu buttons exist, don't proceed
                return;

            m_VisibleButtonCount = 2; // Show only the menu and active tool button
            for (int i = 0; i < m_OrderedButtons.Count; ++i)
            {
                var button = m_OrderedButtons[i];
                button.tooltipVisible = false;
                if (IsMainMenuButton(button))
                    Reinsert(button, k_MenuButtonOrderPosition, true); // Return the main menu button to its original position after being hidden when showing tool buttons
                else
                    m_OrderedButtons[i].order = i > k_ActiveToolOrderPosition ? k_InactiveButtonInitialOrderPosition : i; // Hide buttons beyond the active tool button threshold
            }
        }

        void SetupButtonOrderThenSelectTool(IToolsMenuButton toolsMenuButton, bool selectAfterSettingButtonOrder = true)
        {
            var mainMenu = IsMainMenuButton(toolsMenuButton);
            if (mainMenu)
            {
                mainMenuActivatorSelected(rayOrigin);
                return;
            }

            var showMenuButton = !aboveMinimumButtonCount;

            Reinsert(toolsMenuButton, k_ActiveToolOrderPosition);

            this.RestartCoroutine(ref m_ShowHideAllButtonsCoroutine, ShowThenHideAllButtons(1f, showMenuButton));

            if (selectAfterSettingButtonOrder && buttonSelected != null)
            {
                bool existingButton = m_OrderedButtons.Any((x) => x.toolType == toolsMenuButton.toolType);
                if (existingButton)
                    buttonSelected(rayOrigin, toolsMenuButton.toolType); // Select the tool in the Tools Menu
            }
        }

        /// <summary>
        /// Utilized by Tools Menu to select an existing button by type, without creating a new button
        /// </summary>
        /// <param name="type">Button ToolType to compare against existing button types</param>
        public void SelectExistingToolType(Type type)
        {
            foreach (var button in m_OrderedButtons)
            {
                if (button.toolType == type)
                {
                    SetupButtonOrderThenSelectTool(button, false);
                    return;
                }
            }
        }

        /// <summary>
        /// Utilized by ToolsMenuButtons to select an existing button by type, without creating a new button
        /// </summary>
        /// <param name="type">Button ToolType to compare against existing button types</param>
        void SelectExistingToolTypeFromButton(Type type)
        {
            foreach (var button in m_OrderedButtons)
            {
                if (button.toolType == type)
                {
                    SetupButtonOrderThenSelectTool(button);
                    return;
                }
            }
        }

        public void SelectNextExistingToolButton()
        {
            var button = m_OrderedButtons[aboveMinimumButtonCount ? k_ActiveToolOrderPosition + 1 : k_ActiveToolOrderPosition];
            SetupButtonOrderThenSelectTool(button);
        }

        public void HighlightSingleButtonWithoutMenu(int buttonOrderPosition)
        {
            for (int i = 1; i < m_OrderedButtons.Count; ++i)
            {
                var button = m_OrderedButtons[i];
                if (i == buttonOrderPosition)
                {
                    if (!button.highlighted && buttonHovered != null)
                    {
                        // Process haptic pulse if button was not already highlighted
                        this.PulseSpatialHintScrollArrows();

                        if (buttonHovered != null)
                            buttonHovered();
                    }

                    button.highlighted = true;
                }
                else
                {
                    button.highlighted = false;
                }
            }
        }

        /// <summary>
        /// Used when spatially selecting a highlighted button
        /// </summary>
        public void SelectHighlightedButton()
        {
            for (int i = 0; i < m_OrderedButtons.Count; ++i)
            {
                var button = m_OrderedButtons[i];
                var isHighlighted = button.highlighted;
                if (isHighlighted)
                {
                    // Force the selection of the button regardless of it previously existing via a call to EVR that triggers a call to SelectExistingType()
                    if (buttonSelected != null)
                        buttonSelected(rayOrigin, button.toolType);

                    if (buttonClicked != null)
                        buttonClicked();

                    allButtonsVisible = false;

                    return;
                }
            }
        }

        /// <summary>
        /// Delete a highlighted button, then select the next active tool button
        /// </summary>
        /// <returns>Bool denoting that a highlighted button other than the selection tool button was deleted</returns>
        public bool DeleteHighlightedButton()
        {
            IToolsMenuButton button = null;
            for (int i = 0; i < m_OrderedButtons.Count; ++i)
            {
                button = m_OrderedButtons[i];
                if ((button.highlighted || button.secondaryButtonHighlighted) && !IsSelectionToolButton(button))
                    break;

                button = null;
            }

            if (button != null)
            {
                m_OrderedButtons.Remove(button);
                button.destroy();

                // Return to the selection tool, as the active tool, when closing a tool via the secondary close button on a ToolsMenuButton
                for (int i = 0; i < m_OrderedButtons.Count; ++i)
                {
                    if (IsSelectionToolButton(m_OrderedButtons[i]))
                    {
                        button = m_OrderedButtons[i];
                        break;
                    }
                }

                if (buttonSelected != null)
                    buttonSelected(rayOrigin, button.toolType); // Select the new active tool button
            }

            if (!aboveMinimumButtonCount && closeMenu != null)
                closeMenu(); // Close the menu if below the minimum button count (only MainMenu & SelectionTool are active)

            return button != null;
        }

        /// <summary>
        /// Delete a button of a given type, then select the next active tool button
        /// </summary>
        /// <returns>Bool denoting that a button of the given type was deleted</returns>
        public bool DeleteButtonOfType(Type type)
        {
            bool buttonDeleted = false;
            for (int i = 0; i < m_OrderedButtons.Count; ++i)
            {
                var button = m_OrderedButtons[i];
                if (button.toolType == type && !IsMainMenuButton(button) && !IsSelectionToolButton(button))
                {
                    buttonDeleted = true;
                    m_OrderedButtons.Remove(button);
                    button.destroy();
                    break;
                }
            }

            return buttonDeleted;
        }

        static bool IsMainMenuButton(IToolsMenuButton button)
        {
            return button.toolType == typeof(IMainMenu);
        }

        static bool IsSelectionToolButton(IToolsMenuButton button)
        {
            return button.toolType == typeof(SelectionTool);
        }

        int VisibleButtonCount(Type buttonType)
        {
            // If button type is main menu, and only the selection tool and main menu buttons are available
            // return a value of ZERO so the main menu button is centered
            return m_VisibleButtonCount - (aboveMinimumButtonCount ? 0 : 1);
        }

        IEnumerator MoveToLocation(Vector3 targetPosition, Vector3 targetScale)
        {
            const float kPrimaryButtonUIAlternatePositionScalar = 0.65f;
            const int kDurationMultiplier = 6;
            const int kShapeMultiplier = 4;
            var currentPosition = transform.localPosition;
            var currentScale = transform.localScale;
            var currentPrimaryButtonUIContainerLocalScale = m_OrderedButtons[0].primaryUIContentContainerLocalScale;
            var targetPrimaryButtonUIContainerLocalScale = Vector3.one * (m_MoveToAlternatePosition ? kPrimaryButtonUIAlternatePositionScalar : 1f);
            var duration = 0f;
            while (duration < 1)
            {
                duration += Time.unscaledDeltaTime * kDurationMultiplier;
                var durationShaped = Mathf.Pow(MathUtilsExt.SmoothInOutLerpFloat(duration), kShapeMultiplier);
                transform.localScale = Vector3.Lerp(currentScale, targetScale, durationShaped);
                transform.localPosition = Vector3.Lerp(currentPosition, targetPosition, durationShaped);

                var newPrimaryButtonScale = Vector3.Lerp(currentPrimaryButtonUIContainerLocalScale, targetPrimaryButtonUIContainerLocalScale, durationShaped);
                for (int i = 0; i < m_OrderedButtons.Count; ++i)
                    m_OrderedButtons[i].primaryUIContentContainerLocalScale = newPrimaryButtonScale;

                yield return null;
            }

            transform.localScale = targetScale;
            transform.localPosition = targetPosition;
            m_MoveCoroutine = null;
        }

        void ShowAllButtons(IToolsMenuButton button)
        {
            m_RayHovered = true;
            if (!allButtonsVisible && aboveMinimumButtonCount && !IsMainMenuButton(button) && m_ButtonHoverExitDelayCoroutine == null)
                allButtonsVisible = true;
        }

        void ButtonHoverExitPerformed()
        {
            this.RestartCoroutine(ref m_ButtonHoverExitDelayCoroutine, DelayedHoverExitCheck());
        }

        IEnumerator DelayedHoverExitCheck()
        {
            var duration = Time.unscaledDeltaTime;
            m_RayHovered = false;
            while (duration < 0.25f)
            {
                duration += Time.unscaledDeltaTime;
                yield return null;

                if (m_RayHovered)
                    yield break;
            }

            // Only proceed if no other button is being hovered
            allButtonsVisible = false;
            m_ButtonHoverExitDelayCoroutine = null;
        }

        void OnButtonHover()
        {
            if (buttonHovered != null)
                buttonHovered();
        }
    }
}
#endif
