#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Helpers;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Menus
{
	sealed class PinnedToolsMenuUI : MonoBehaviour, IInstantiateUI
	{
		const int k_MenuButtonOrderPosition = 0; // A shared menu button position used in this particular ToolButton implementation
		const int k_ActiveToolOrderPosition = 1; // A active-tool button position used in this particular ToolButton implementation

		[SerializeField]
		Transform m_ButtonContainer;

		bool m_AllButtonsVisible;
		List<IPinnedToolButton> m_OrderedButtons;
		Coroutine m_ShowHideAllButtonsCoroutine;
		int m_VisibleButtonCount;

		public Dictionary<Type, IPinnedToolButton> pinnedToolButtons { get; set; }
		public Node node { get; set; }
		public int maxButtonCount { get; set; }
		public Transform buttonContainer { get { return m_ButtonContainer; } }
		public Action<Transform, Type> selectTool { get; set; }
		public Transform rayOrigin { get; set; }

		public bool allButtonsVisible
		{
			get { return m_AllButtonsVisible; }
			set
			{
				//if (m_AllButtonsVisible == value)
					//return;

				m_AllButtonsVisible = value;

				if (m_AllButtonsVisible)
					ShowAllButtons();
				else
					HideAllButtons();
			}
		}

		private int visibleButtonCount
		{
			get
			{
				return m_VisibleButtonCount;
				/*
				int count = 0;
				for (int i = 0; i < m_OrderedButtons.Count; ++i)
				{
					if (m_OrderedButtons[i].order > -1)
						++count;
				}

				return count;
				*/
			}
		}

		void Awake()
		{
			m_OrderedButtons = new List<IPinnedToolButton>();
			Debug.LogError("<color=green>PinnedToolsMenuUI initialized</color>");
		}

		public void AddButton(IPinnedToolButton button, Transform buttonTransform)
		{
			button.allButtonsVisible = allButtonsVisible;
			button.maxButtonCount = maxButtonCount;
			button.selectTool = SelectTool;
			button.visibileButtonCount = visibleButtonCount;

			var insertPosition = -1;
			if (button.toolType == typeof(IMainMenu))
				insertPosition = k_MenuButtonOrderPosition;
			else
				insertPosition = k_ActiveToolOrderPosition;

			m_OrderedButtons.Insert(insertPosition, button);
			m_VisibleButtonCount = m_VisibleButtonCount = m_OrderedButtons.Count;

			button.activeTool = true;

			buttonTransform.rotation = Quaternion.identity;
			buttonTransform.localPosition = Vector3.zero;
			buttonTransform.localScale = Vector3.zero;

			button.order = insertPosition;

			Debug.LogWarning("Setting up button : " + button.toolType + " - ORDER : " + button.order);

			if (m_OrderedButtons.Count > k_ActiveToolOrderPosition)
				this.RestartCoroutine(ref m_ShowHideAllButtonsCoroutine, ShowThenHideAllButtons());
			/*
			foreach (var pair in pinnedToolButtons)
			{
				if (pair.Value.order != pair.Value.menuButtonOrderPosition) // don't move the main menu button
					pair.Value.order++;
			}
			*/
		}

		IEnumerator ShowThenHideAllButtons(bool waitBeforeClosingAllButtons = true)
		{
			SetupButtonOrder();

			if (waitBeforeClosingAllButtons)
			{
				var duration = Time.unscaledDeltaTime;
				while (duration < 1.25f)
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

		void SetupButtonOrder()
		{
			for (int i = 0; i < m_OrderedButtons.Count; ++i)
			{
				var button = m_OrderedButtons[i];
				button.activeTool = i == k_ActiveToolOrderPosition;
				button.order = i;
			}
		}

		void ShowAllButtons()
		{
			m_VisibleButtonCount = m_OrderedButtons.Count - 1; // subtract the menu button from the total
			for (int i = 0; i < m_OrderedButtons.Count; ++i)
				m_OrderedButtons[i].order = i == 0 ? -1 : i; // hide the menu buttons when revealing all tools buttons

			SetupButtonOrder();
		}

		void HideAllButtons()
		{
			Debug.LogError("Hiding all buttons");
			m_VisibleButtonCount = 2; // Show only the menu and active tool button
			const int kInactiveButtonInitialOrderPosition = -1;
			for (int i = 0; i < m_OrderedButtons.Count; ++i)
				m_OrderedButtons[i].order = i > k_ActiveToolOrderPosition ? kInactiveButtonInitialOrderPosition : i; // maintain menu and active tool positions
		}

		void SelectTool(IPinnedToolButton pinnedToolButton)
		{
			for (int i = 0; i < m_OrderedButtons.Count; ++i)
			{
				var button = m_OrderedButtons[i];;
				if (button == pinnedToolButton && button.order > k_ActiveToolOrderPosition)
				{
					m_OrderedButtons.Remove(button);
					m_OrderedButtons.Insert(k_ActiveToolOrderPosition, button);
				}
			}

			SetupButtonOrder(); // after setting the new order of the active tool button, reposition the buttons
			selectTool(rayOrigin, pinnedToolButton.toolType);
		}

		public void HighlightSingleButton(int buttonOrderPosition)
		{
			Debug.LogError("Highlighting SINGLE BUTTON at position : "+ buttonOrderPosition);
			for (int i = 0; i < m_OrderedButtons.Count; ++i)
				m_OrderedButtons[i].highlighted = i == buttonOrderPosition;
		}

		public void SelectHighlightedButton()
		{
			for (int i = 0; i < m_OrderedButtons.Count; ++i)
			{
				var button = m_OrderedButtons[i];
				var isHighlighted = button.highlighted;
				if (isHighlighted)
				{
					SelectTool(button);
					return;
				}
			}
		}
	}
}
#endif
