#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Helpers;
using UnityEditor.Experimental.EditorVR.Tools;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Menus
{
	sealed class PinnedToolsMenuUI : MonoBehaviour, ISelectTool
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
		int m_VisibleButtonCount;
		bool m_MoveToAlternatePosition;
		Vector3 m_OriginalLocalScale;

		public Dictionary<Type, IPinnedToolButton> pinnedToolButtons { get; set; }
		public Node node { get; set; }
		public int maxButtonCount { get; set; }
		public Transform buttonContainer { get { return m_ButtonContainer; } }
		public Transform rayOrigin { get; set; }
		public Action<Transform> mainMenuActivatorSelected { get; set; }

		public bool allButtonsVisible
		{
			get { return m_AllButtonsVisible; }
			set
			{
				//if (m_AllButtonsVisible == value)
					//return;

				m_AllButtonsVisible = value;

				if (m_AllButtonsVisible)
				{
					this.StopCoroutine(ref m_ShowHideAllButtonsCoroutine);
					ShowAllExceptMenuButton();
				}
				else
					ShowOnlyMenuAndActiveToolButtons();
			}
		}

		public bool moveToAlternatePosition
		{
			set
			{
				if (m_MoveToAlternatePosition == value)
					return;

				m_MoveToAlternatePosition = value;
				transform.localScale = m_MoveToAlternatePosition ? m_AlternateLocalScale : m_OriginalLocalScale;
				transform.localPosition = m_MoveToAlternatePosition ? m_AlternatePosition : Vector3.zero;
			}
		}

		private bool aboveMinimumButtonCount { get { return m_OrderedButtons.Count > k_ActiveToolOrderPosition + 1; } }

		void Awake()
		{
			m_OriginalLocalScale = transform.localScale;
			m_OrderedButtons = new List<IPinnedToolButton>();
			Debug.LogError("<color=green>PinnedToolsMenuUI initialized</color>");
		}

		public void AddButton(IPinnedToolButton button, Transform buttonTransform)
		{
			button.allButtonsVisible = allButtonsVisible;
			button.maxButtonCount = maxButtonCount;
			button.selectTool = SetupButtonOrderThenSelectTool;
			button.visibileButtonCount = VisibleButtonCount; // allow buttons to fetch local buttonCount

			var insertPosition = k_InactiveButtonInitialOrderPosition;
			if (IsMainMenuButton(button))
			{
				insertPosition = k_MenuButtonOrderPosition;
				//m_VisibleButtonCount = 2; // Show only the MainMenu and select buttons initiall
			}
			else
			{
				insertPosition = k_ActiveToolOrderPosition;
			}

			m_OrderedButtons.Insert(insertPosition, button);
			m_VisibleButtonCount = m_OrderedButtons.Count;

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
				button.order = i;
			}
		}

		void ShowAllExceptMenuButton()
		{
			Debug.LogError("ShowAllExceptMenuButton");
			m_VisibleButtonCount = Mathf.Max(0, m_OrderedButtons.Count - 1); // The MainMenu button will be hidden, subtract 1 from the m_VisibleButtonCount
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
			if (mainMenu)
			{
				mainMenuActivatorSelected(rayOrigin);
				return;
			}
			else if (!aboveMinimumButtonCount)
			{
				return;
			}

			Reinsert(pinnedToolButton, k_ActiveToolOrderPosition);

			this.RestartCoroutine(ref m_ShowHideAllButtonsCoroutine, ShowThenHideAllButtons(1f, false));

			bool existingButton = m_OrderedButtons.Any((x) => x.toolType == pinnedToolButton.toolType);
			if (!existingButton)
				this.SelectTool(rayOrigin, pinnedToolButton.toolType);
		}

		/// <summary>
		/// Utilized by PinnedToolsMenu to select an existing button by type, without created a new button
		/// </summary>
		/// <param name="type">Button ToolType to compare against existing button types</param>
		public void SelectExistingType(Type type)
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
			var button = m_OrderedButtons[k_ActiveToolOrderPosition + 1];
			SetupButtonOrderThenSelectTool(button);
			this.SelectTool(rayOrigin, button.toolType);
		}

		public void HighlightSingleButtonWithoutMenu(int buttonOrderPosition)
		{
			//Debug.LogError("Highlighting SINGLE BUTTON at position : "+ buttonOrderPosition);
			for (int i = 1; i < m_OrderedButtons.Count; ++i)
			{
				m_OrderedButtons[i].highlighted = i == buttonOrderPosition;
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
					return;
				}
			}
		}

		bool IsMainMenuButton(IPinnedToolButton button)
		{
			return button.toolType == typeof(IMainMenu);
		}

		bool IsSelectionButton(IPinnedToolButton button)
		{
			return button.toolType == typeof(SelectionTool);
		}

		int VisibleButtonCount()
		{
			return m_VisibleButtonCount;
		}
	}
}
#endif
