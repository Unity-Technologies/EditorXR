#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEditor.Experimental.EditorVR.Tools;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Menus
{
	sealed class PinnedToolsMenuUI : MonoBehaviour, ISelectTool, IUsesViewerScale, IUsesNode, IInstantiateUI, IConnectInterfaces, IControlSpatialHinting, IControlHaptics
	{
		const int k_MenuButtonOrderPosition = 0; // Menu button position used in this particular ToolButton implementation
		const int k_ActiveToolOrderPosition = 1; // Active-tool button position used in this particular ToolButton implementation
		const int k_InactiveButtonInitialOrderPosition = -1;

		[SerializeField]
		Transform m_ButtonContainer;

		[Header("Used when displaying Alternate Menu")]
		[SerializeField]
		Vector3 m_AlternatePosition;

		[SerializeField]
		Vector3 m_AlternateLocalScale;

		bool m_AllButtonsVisible;
		List<IPinnedToolButton> m_OrderedButtons;
		Coroutine m_ShowHideAllButtonsCoroutine;
		Coroutine m_MoveCoroutine;
		Coroutine m_ButtonHoverExitDelayCoroutine;
		int m_VisibleButtonCount;
		bool m_MoveToAlternatePosition;
		Vector3 m_OriginalLocalScale;
		bool m_RayHovered;
		float m_SpatialDragDistance;
		Quaternion m_HintContentContainerInitialRotation;
		Quaternion m_HintContentContainerCurrentRotation;
		Vector3 m_HintContentWorldPosition;

		public int maxButtonCount { get; set; }
		public Transform buttonContainer { get { return m_ButtonContainer; } }
		public Transform rayOrigin { get; set; }
		public Action<Transform> mainMenuActivatorSelected { get; set; }
		public List<IPinnedToolButton> buttons { get { return m_OrderedButtons; } }

		public bool allButtonsVisible
		{
			get { return m_AllButtonsVisible; }
			set
			{
				m_AllButtonsVisible = value;
				Debug.LogError("AllButtonsVisible set to : " + value);

				if (m_AllButtonsVisible)
				{
					this.StopCoroutine(ref m_ShowHideAllButtonsCoroutine);
					ShowAllExceptMenuButton();
				}
				else
				{
					//this.Pulse(rayOrigin, 0.5f, 0.065f, false, true);
					//spatialDragDistance = 0f;
					ShowOnlyMenuAndActiveToolButtons();

					this.SetSpatialHintRotationTarget(Vector3.zero);
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

		private bool aboveMinimumButtonCount
		{
			get
			{
				const int selectionToolButtonHideCount = 2;
				var count = m_OrderedButtons.Count;
				var aboveMinCount = count > selectionToolButtonHideCount; // Has at least one tool been added beyond the default MainMenu & SelectionTool

				// Prevent the display of the SelectionTool button, if only the MainMenu and SelectionTool buttons reside in the buttons collection
				if (count == selectionToolButtonHideCount)
					aboveMinCount = buttons.All( x => x.toolType != typeof(SelectionTool) );

				return aboveMinCount;
			}
		}

		public bool beginningSpatialScrolling
		{
			set
			{
				if (value)
				{
					Debug.LogWarning("SETTING Spatial Drag Distance  : " + value);
					m_SpatialDragDistance = 0f;
					var currentRotation = transform.rotation.eulerAngles;
					m_HintContentContainerInitialRotation = Quaternion.Euler(0f, currentRotation.y, 0f); // Quaternion.AngleAxis(transform.forward.y, Vector3.up);
					m_HintContentWorldPosition = transform.position;
					this.SetSpatialHintPosition(m_HintContentWorldPosition);
					//spatialHintContentContainer.position = m_HintContentWorldPosition;
				}
			}
		}

		public float spatialDragDistance
		{
			set
			{
				m_SpatialDragDistance = value;
				//m_SpatialScrollOrientation = transform.rotation;
			}
		}

		public Vector3? startingDragOrigin
		{
			set
			{
				Debug.LogWarning("SETTING STARTING DRAG DEFINITON POSITION : " + value.Value.ToString("F4"));
				m_StartingDragOrigin = transform.position;

				//var orig = spatialHintContentContainer.rotation;
				//this.SetSpatialHintLookAT(value.Value);
				//spatialHintContentContainer.LookAt(value.Value);
				Debug.LogError(value.Value.ToString("F4"));
				//m_SpatialScrollOrientation = Quaternion.Euler(value.Value); // Quaternion.FromToRotation(m_HintContentContainer.forward, value.Value); // Quaternion.Euler(value.Value); Quaternion.RotateTowards(m_HintContentContainerInitialRotation, Quaternion.Euler(value.Value), 180f);
				//this.SetSpatialHintRotation(orig);
				//spatialHintContentContainer.rotation = orig;
				this.SetSpatialHintLookATRotation(value.Value);
			}
		}

		public event Action buttonHovered;
		public event Action buttonClicked;

		private Vector3 m_StartingDragOrigin;
		private Vector3 m_DragTarget;
		private IUsesNode m_UsesNodeImplementation;

		void Awake()
		{
			m_OriginalLocalScale = transform.localScale;
			m_OrderedButtons = new List<IPinnedToolButton>();
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
					m_DragTarget = transform.position; // Cache the initial drag target position, before performing any extra shapting to the target Vec3
					this.SetSpatialHintState(SpatialHintModule.SpatialHintStateFlags.Scrolling);
				}

				// Follow the user's input for a short additional period of time
				// Update the dragTarget with the current device position, to allow for visuals to better match the expected rotation/position
				m_DragTarget = transform.position;
				this.SetSpatialHintDragThresholdTriggerPosition(transform.position);
				this.SetSpatialHintRotation(newHintContainerRotation);

				// Perform a smooth lerp of the hint contents after dragging beyond the distance trigger threshold
				m_SpatialDragDistance += Time.unscaledDeltaTime * 8; // Continue to increase the amount
				/////var extentedDragDurationSmoothed = 1 - MathUtilsExt.SmoothInOutLerpFloat(m_SpatialDragDistance - 1f);
				/////m_SpatialHintUI.scrollVisualsRotation = Vector3.Lerp(m_InitialDragTarget, transform.position, extentedDragDurationSmoothed);

				// Add additional smoothed rotation to the scroll visuals to better align them with the user's continued device movemet after crossing the threshold
				// The m_InitialDragTarget is the actualy vector being scrolled against, however adding a slight continuation of the user's device input position
				// allows for the scroll visuals to appear to be more closely aligned to the user's expected rotation.

				//Debug.LogError("INSIDE rotation update loop");
				/*
					var shapedDragAmount = Mathf.Pow(MathUtilsExt.SmoothInOutLerpFloat(m_SmoothedSpatialDragDistance), 6);
					m_HintContentContainerCurrentRotation = Quaternion.Lerp(m_HintContentContainerInitialRotation, m_SpatialScrollOrientation, shapedDragAmount);
					newHintContainerRotation = m_HintContentContainerCurrentRotation;
				*/
			}
			else if (m_AllButtonsVisible && m_SpatialDragDistance > 2)
			{
				//spatialHintScrollVisualsRotation = m_DragTarget;
				this.SetSpatialHintDragThresholdTriggerPosition(transform.position);
				this.SetSpatialHintRotation(newHintContainerRotation);
				this.SetSpatialHintRotationTarget(m_DragTarget);
			}

			//spatialHintScrollVisualsDragThresholdTriggerPosition = transform.position;
			
			//m_SpatialHintUI.scrollVisualsRotation = Quaternion.Euler(endingDragDefinitionPosition - startingDragDefinitionPosition);
			/*
			else if (Mathf.Approximately(m_SmoothedSpatialDragDistance, 1f))
				newHintContainerRotation = m_HintContentContainerCurrentRotation;
			else
				m_HintContentWorldPosition = Vector3.Lerp(m_HintContentWorldPosition, transform.position, Time.unscaledDeltaTime * 6);
			*/

			//Debug.LogError("Hint Container" + m_HintContentContainerInitialRotation);
			//Debug.LogError("UI" + transform.rotation);

			//spatialHintContentContainer.rotation = newHintContainerRotation;
			
			//m_SpatialHintContentContainer.position = m_HintContentWorldPosition;

			//Debug.LogError(gameObject.GetInstanceID() + " : <color=green>World position of UI : " + transform.position + " - starting drag definition position : " + m_StartingDragOrigin + "</color>");
		}

		public void AddButton(IPinnedToolButton button, Transform buttonTransform)
		{
			button.showAllButtons = ShowAllButtons;
			button.hoverExit = ButtonHoverExitPerformed;
			button.maxButtonCount = maxButtonCount;
			button.selectTool = SelectExistingToolType;
			button.closeButton = DeleteHighlightedButton;
			button.visibileButtonCount = VisibleButtonCount; // allow buttons to fetch local buttonCount
			button.hovered += OnButtonHover;

			bool allowSecondaryButton = false; // Secondary button is the close button
			var insertPosition = k_InactiveButtonInitialOrderPosition;
			if (IsMainMenuButton(button))
			{
				insertPosition = k_MenuButtonOrderPosition;
			}
			else
			{
				insertPosition = k_ActiveToolOrderPosition;
				allowSecondaryButton = !IsSelectionToolButton(button);
			}

			m_OrderedButtons.Insert(insertPosition, button);
			// If only the MainMenu & SelectionTool buttons exist, set visible button count to 1
			m_VisibleButtonCount = aboveMinimumButtonCount ? m_OrderedButtons.Count : 1;

			button.implementsSecondaryButton = allowSecondaryButton;
			button.activeTool = true;
			button.order = insertPosition;

			buttonTransform.rotation = Quaternion.identity;
			buttonTransform.localPosition = Vector3.zero;
			buttonTransform.localScale = Vector3.zero;

			Debug.LogWarning("Setting up button : " + button.toolType + " - ORDER : " + button.order);

			if (aboveMinimumButtonCount)
				this.RestartCoroutine(ref m_ShowHideAllButtonsCoroutine, ShowThenHideAllButtons(1.25f, false));
			else
				SetupButtonOrder(); // Setup the MainMenu and active tool buttons only
			/*

			foreach (var pair in pinnedToolButtons)
			{
				if (pair.Value.order != pair.Value.menuButtonOrderPosition) // don't move the main menu button
					pair.Value.order++;
			}
			*/
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

		IPinnedToolButton PreviewToolInPinnedToolButton (Transform rayOrigin, Type toolType)
		{
			// Prevents menu buttons of types other than ITool from triggering any pinned tool button preview actions
			if (!toolType.GetInterfaces().Contains(typeof(ITool)))
				return null;

			IPinnedToolButton pinnedToolButton = null;
			/*
			Rays.ForEachProxyDevice((deviceData) =>
			{
				if (deviceData.rayOrigin == rayOrigin) // enable pinned tool preview on the opposite (handed) device
				{
					var pinnedToolButtons = deviceData.pinnedToolButtons;
					foreach (var pair in pinnedToolButtons)
					{
						var button = pair.Value;
						if (button.order == button.activeToolOrderPosition)
						{
							pinnedToolButton = button;
							pinnedToolButton.previewToolType = toolType;
							break;
						}
					}
				}
			});
			*/
			return pinnedToolButton;
		}

		void Reinsert(IPinnedToolButton button, int newOrderPosition, bool updateButtonOrder = false)
		{
			var removed = m_OrderedButtons.Remove(button);
			if (!removed)
			{
				Debug.LogError("Could not remove button");
				return;
			}

			m_OrderedButtons.Insert(newOrderPosition, button);

			if (updateButtonOrder)
				button.order = newOrderPosition;
		}

		void SetupButtonOrder()
		{
			Debug.LogError("SetupButtonOrder");
			for (int i = 0; i < m_OrderedButtons.Count; ++i)
			{
				var button = m_OrderedButtons[i];
				button.activeTool = i == k_ActiveToolOrderPosition;
				// Allow settings of regular button order if there are more buttons that just the MainMenu & SelectionTool
				button.order = aboveMinimumButtonCount || IsMainMenuButton(button) ? i : k_InactiveButtonInitialOrderPosition;
			}
		}

		void ShowAllExceptMenuButton()
		{
			Debug.LogError("ShowAllExceptMenuButton");
			// The MainMenu button will be hidden, subtract 1 from the m_VisibleButtonCount
			m_VisibleButtonCount = Mathf.Max(0, m_OrderedButtons.Count - 1);
			for (int i = 0; i < m_OrderedButtons.Count; ++i)
			{
				var button = m_OrderedButtons[i];
				button.activeTool = i == k_ActiveToolOrderPosition; // Set the button gradients // TODO Consider handling insid button via k_ActiveToolOrder position comparison
				button.order = i == k_MenuButtonOrderPosition ? k_InactiveButtonInitialOrderPosition : i - 1; // Hide the menu buttons when revealing all tools buttons
			}
		}

		void ShowOnlyMenuAndActiveToolButtons()
		{
			Debug.LogError("Showing only the MainMenu and ACTIVE tool buttons");
			if (!aboveMinimumButtonCount) // If only the Selection and MainMenu buttons exist, don't proceed
				return;

			m_VisibleButtonCount = 2; // Show only the menu and active tool button
			for (int i = 0; i < m_OrderedButtons.Count; ++i)
			{
				var button = m_OrderedButtons[i];
				button.toolTipVisible = false;
				if (IsMainMenuButton(button))
					Reinsert(button, k_MenuButtonOrderPosition, true); // Return the main menu button to its original position after being hidden when showing tool buttons
				else
					m_OrderedButtons[i].order = i > k_ActiveToolOrderPosition ? k_InactiveButtonInitialOrderPosition : i; // Hide buttons beyond the active tool button threshold
			}
		}

		void SetupButtonOrderThenSelectTool(IPinnedToolButton pinnedToolButton)
		{
			Debug.LogError("<color=white> SetupButtonOrderThenSelectTool - Selecting  of type : </color>" + pinnedToolButton.toolType);
			var mainMenu = IsMainMenuButton(pinnedToolButton);
			var showMenuButton = false;
			if (mainMenu)
			{
				mainMenuActivatorSelected(rayOrigin);
				return;
			}
			else if (!aboveMinimumButtonCount)
			{
				showMenuButton = true;
			}

			Reinsert(pinnedToolButton, k_ActiveToolOrderPosition);

			this.RestartCoroutine(ref m_ShowHideAllButtonsCoroutine, ShowThenHideAllButtons(1f, showMenuButton));

			bool existingButton = m_OrderedButtons.Any((x) => x.toolType == pinnedToolButton.toolType);
			if (!existingButton)
				this.SelectTool(rayOrigin, pinnedToolButton.toolType);

			Debug.LogError("Perform Pulse up in PinnedToolsMenu level");
			//this.Pulse(rayOrigin, 0.5f, 0.1f, true, true);
		}

		/// <summary>
		/// Utilized by PinnedToolsMenu to select an existing button by type, without created a new button
		/// </summary>
		/// <param name="type">Button ToolType to compare against existing button types</param>
		public void SelectExistingToolType(Type type)
		{
			Debug.LogError("Selecting Existing ToolType : " + type.ToString());
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
			this.SelectTool(rayOrigin, button.toolType);
		}

		public void HighlightSingleButtonWithoutMenu(int buttonOrderPosition)
		{
			//Debug.LogError("Highlighting SINGLE BUTTON at position : "+ buttonOrderPosition);
			for (int i = 1; i < m_OrderedButtons.Count; ++i)
			{
				var button = m_OrderedButtons[i];
				if (i == buttonOrderPosition)
				{
					if (!button.highlighted && buttonHovered != null)
					{
						// Process haptic pulse if button was not already highlighted
						this.PulseSpatialHintScrollArrows();
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

		public void SelectHighlightedButton()
		{
			for (int i = 0; i < m_OrderedButtons.Count; ++i)
			{
				var button = m_OrderedButtons[i];
				var isHighlighted = button.highlighted;
				if (isHighlighted)
				{
					Debug.LogError("<color=orange>Selecting highlighted button : </color>"+ button.toolType);
					// Force the selection of the button regardless of it previously existing via a call to EVR that triggers a call to SelectExistingType()
					this.SelectTool(rayOrigin, button.toolType);
					if (buttonClicked != null)
						buttonClicked();

					allButtonsVisible = false;
					//SpatialHintState = SpatialHintModule.SpatialHintStateFlags.Hidden;

					return;
				}
			}
		}

		public bool DeleteHighlightedButton()
		{
			IPinnedToolButton button = null;
			for (int i = 0; i < m_OrderedButtons.Count; ++i)
			{
				button = m_OrderedButtons[i];
				if ((button.highlighted || button.secondaryButtonHighlighted) && !IsSelectionToolButton(button))
				{
					Debug.LogError("<color=blue>DeleteHighlightedButton : </color>" + button.toolType);
					break;
				}

				button = null;
			}

			if (button != null)
			{
				m_OrderedButtons.Remove(button);
				button.destroy();
				button = m_OrderedButtons[k_ActiveToolOrderPosition];
				this.SelectTool(rayOrigin, button.toolType);
			}

			return button != null;
		}

		bool IsMainMenuButton(IPinnedToolButton button)
		{
			return button.toolType == typeof(IMainMenu);
		}

		bool IsSelectionToolButton(IPinnedToolButton button)
		{
			return button.toolType == typeof(SelectionTool);
		}

		int VisibleButtonCount()
		{
			return m_VisibleButtonCount;
		}

		IEnumerator MoveToLocation(Vector3 targetPosition, Vector3 targetScale)
		{
			const float kPrimaryButtonUIAlternatePositionScalar = 0.65f;
			var currentPosition = transform.localPosition;
			var currentScale = transform.localScale;
			var currentPrimaryButtonUIContainerLocalScale = m_OrderedButtons[0].primaryUIContentContainerLocalScale;
			var targetPrimaryButtonUIContainerLocalScale = Vector3.one * (m_MoveToAlternatePosition ? kPrimaryButtonUIAlternatePositionScalar : 1f);
			var duration = 0f;
			while (duration < 1)
			{
				duration += Time.unscaledDeltaTime * 6f;
				var durationShaped = Mathf.Pow(MathUtilsExt.SmoothInOutLerpFloat(duration), 4);
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

		void ShowAllButtons(IPinnedToolButton button)
		{
			Debug.LogError("<color=blue>ShowAllButtons : </color>" + button.toolType);
			m_RayHovered = true;
			if (!allButtonsVisible && aboveMinimumButtonCount && !IsMainMenuButton(button) && m_ButtonHoverExitDelayCoroutine == null)
				allButtonsVisible = true;
		}

		void HideAllButtons(IPinnedToolButton button)
		{
			Debug.LogError("<color=blue>HideAllButtons : </color>" + button.toolType);
			if (allButtonsVisible && !IsMainMenuButton(button))
				allButtonsVisible = false;
		}

		void ButtonHoverExitPerformed()
		{
			this.RestartCoroutine(ref m_ButtonHoverExitDelayCoroutine, DelayedHoverExitCheck());
		}

		IEnumerator DelayedHoverExitCheck()
		{
			m_RayHovered = false;

			var duration = Time.unscaledDeltaTime;
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

		public Node? node
		{
			set { m_UsesNodeImplementation.node = value; }
		}
	}
}
#endif
