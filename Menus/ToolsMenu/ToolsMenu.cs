#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Menus
{
	sealed class ToolsMenu : MonoBehaviour, IToolsMenu, IConnectInterfaces, IInstantiateUI,
		IControlHaptics, IUsesViewerScale, IControlSpatialScrolling, IControlSpatialHinting, IRayVisibilitySettings, IUsesRayOrigin
	{
		const int k_ActiveToolOrderPosition = 1; // A active-tool button position used in this particular ToolButton implementation
		const int k_MaxButtonCount = 16;

		[SerializeField]
		Sprite m_MainMenuIcon;

		[SerializeField]
		ActionMap m_MainMenuActionMap;

		[SerializeField]
		ToolsMenuUI m_ToolsMenuPrefab;

		[SerializeField]
		ToolsMenuButton _mToolsMenuButtonTemplate;

		[SerializeField]
		HapticPulse m_ButtonClickPulse;

		[SerializeField]
		HapticPulse m_ButtonHoverPulse;

		[SerializeField]
		HapticPulse m_HidingPulse; // The pulse performed when ending a spatial selection

		Transform m_RayOrigin;
		float m_AllowToolToggleBeforeThisTime;
		Vector3 m_SpatialScrollStartPosition;
		ToolsMenuUI m_ToolsMenuUi;

		public Transform menuOrigin { get; set; }
		List<IToolsMenuButton> buttons { get { return m_ToolsMenuUi.buttons; } }
		public bool alternateMenuVisible { set { m_ToolsMenuUi.moveToAlternatePosition = value; } }

		public Action<Transform, int, bool> highlightSingleButton { get; set; }
		public Action<Transform> selectHighlightedButton { get; set; }
		public Action<Type, Sprite> setButtonForType { get; set; }
		public Action<Type, Type> deleteToolsMenuButton { get; set; }
		public Node? node { get; set; }
		public IToolsMenuButton PreviewToolsMenuButton { get; private set; }
		public Transform alternateMenuOrigin { get; set; }
		public SpatialScrollModule.SpatialScrollData spatialScrollData { get; set; }
		public ActionMap actionMap { get { return m_MainMenuActionMap; } }

		public Transform rayOrigin
		{
			get { return m_RayOrigin; }
			set
			{
				m_RayOrigin = value;
				// UI is created after RayOrigin is set here
				// Ray origin is then set in CreateToolsMenuUI()
				CreateToolsMenuUI();
			}
		}

		void Awake()
		{
			setButtonForType = CreateToolsMenuButton;
			deleteToolsMenuButton = DeleteToolsMenuButton;
		}

		void OnDestroy()
		{
			this.RemoveRayVisibilitySettings(rayOrigin, this);
		}

		void CreateToolsMenuUI()
		{
			m_ToolsMenuUi = m_ToolsMenuUi ?? this.InstantiateUI(m_ToolsMenuPrefab.gameObject).GetComponent<ToolsMenuUI>();
			m_ToolsMenuUi.maxButtonCount = k_MaxButtonCount;
			m_ToolsMenuUi.mainMenuActivatorSelected = this.MainMenuActivatorSelected;
			m_ToolsMenuUi.rayOrigin = rayOrigin;
			m_ToolsMenuUi.buttonHovered += OnButtonHover;
			m_ToolsMenuUi.buttonClicked += OnButtonClick;
			m_ToolsMenuUi.buttonSelected += OnButtonSelected;
			m_ToolsMenuUi.closeMenu += CloseMenu;

			// Alternate menu origin isn't set when awake or start run
			var toolsMenuUITransform = m_ToolsMenuUi.transform;
			toolsMenuUITransform.SetParent(alternateMenuOrigin);
			toolsMenuUITransform.localPosition = Vector3.zero;
			toolsMenuUITransform.localRotation = Quaternion.identity;
		}

		void CreateToolsMenuButton(Type toolType, Sprite buttonIcon)
		{
			// Select an existing ToolButton if the type is already present in a button
			if (buttons.Any( x => x.toolType == toolType))
			{
				m_ToolsMenuUi.SelectExistingToolType(toolType);
				return;
			}

			if (buttons.Count >= k_MaxButtonCount) // Return if tool type already occupies a tool button
				return;

			var buttonTransform = ObjectUtils.Instantiate(_mToolsMenuButtonTemplate.gameObject, m_ToolsMenuUi.buttonContainer, false).transform;
			var button = buttonTransform.GetComponent<ToolsMenuButton>();
			this.ConnectInterfaces(button);

			button.rayOrigin = rayOrigin;
			button.toolType = toolType; // Assign Tool Type before assigning order
			button.icon = toolType != typeof(IMainMenu) ? buttonIcon : m_MainMenuIcon;
			button.highlightSingleButton = highlightSingleButton;
			button.selectHighlightedButton = selectHighlightedButton;
			button.rayOrigin = rayOrigin;

			if (toolType == typeof(IMainMenu))
				PreviewToolsMenuButton = button;

			m_ToolsMenuUi.AddButton(button, buttonTransform);
		}

		void DeleteToolsMenuButton(Type toolTypeToDelete, Type toolTypeToSelectAfterDelete)
		{
			if (m_ToolsMenuUi.DeleteButtonOfType(toolTypeToDelete))
				m_ToolsMenuUi.SelectNextExistingToolButton();
		}

		public void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl)
		{
			var buttonCount = buttons.Count;
			if (buttonCount <= k_ActiveToolOrderPosition + 1)
				return;

			const float kAllowToggleDuration = 0.25f;
			var toolslMenuInput = (ToolslMenuInput) input;

			if (spatialScrollData != null && toolslMenuInput.cancel.wasJustPressed)
			{
				consumeControl(toolslMenuInput.cancel);
				consumeControl(toolslMenuInput.show);
				consumeControl(toolslMenuInput.select);
				OnButtonClick();
				CloseMenu(); // Also ends spatial scroll
				m_ToolsMenuUi.allButtonsVisible = false;
			}

			if (spatialScrollData == null && (toolslMenuInput.show.wasJustPressed || toolslMenuInput.show.isHeld) && toolslMenuInput.select.wasJustPressed)
			{
				m_SpatialScrollStartPosition = alternateMenuOrigin.position;
				m_AllowToolToggleBeforeThisTime = Time.realtimeSinceStartup + kAllowToggleDuration;
				this.SetSpatialHintControlNode(node);
				m_ToolsMenuUi.spatiallyScrolling = true; // Triggers the display of the directional hint arrows
				consumeControl(toolslMenuInput.show);
				consumeControl(toolslMenuInput.select);
				// Assign initial SpatialScrollData; begin scroll
				spatialScrollData = this.PerformSpatialScroll(node, m_SpatialScrollStartPosition, alternateMenuOrigin.position, 0.325f, m_ToolsMenuUi.buttons.Count, m_ToolsMenuUi.maxButtonCount);
			}
			else if (spatialScrollData != null && toolslMenuInput.show.isHeld)
			{
				consumeControl(toolslMenuInput.show);
				consumeControl(toolslMenuInput.select);
				// Attempt to close a button, if a scroll has passed the trigger threshold
				if (spatialScrollData != null && toolslMenuInput.select.wasJustPressed)
				{
					if (m_ToolsMenuUi.DeleteHighlightedButton())
						buttonCount = buttons.Count; // The MainMenu button will be hidden, subtract 1 from the activeButtonCount

					if (buttonCount <= k_ActiveToolOrderPosition + 1)
					{
						if (spatialScrollData != null)
							this.EndSpatialScroll();

						return;
					}
				}

				// normalized input should loop after reaching the 0.15f length
				buttonCount -= 1; // Decrement to disallow cycling through the main menu button
				spatialScrollData = this.PerformSpatialScroll(node, m_SpatialScrollStartPosition, alternateMenuOrigin.position, 0.325f, m_ToolsMenuUi.buttons.Count, m_ToolsMenuUi.maxButtonCount);
				var normalizedRepeatingPosition = spatialScrollData.normalizedLoopingPosition;
				if (!Mathf.Approximately(normalizedRepeatingPosition, 0f))
				{
					if (!m_ToolsMenuUi.allButtonsVisible)
					{
						m_ToolsMenuUi.spatialDragDistance = spatialScrollData.dragDistance;
						this.SetSpatialHintState(SpatialHintModule.SpatialHintStateFlags.CenteredScrolling);
						m_ToolsMenuUi.allButtonsVisible = true;
					}
					else if (spatialScrollData.spatialDirection != null)
					{
						m_ToolsMenuUi.startingDragOrigin = spatialScrollData.spatialDirection;
					}

					m_ToolsMenuUi.HighlightSingleButtonWithoutMenu((int)(buttonCount * normalizedRepeatingPosition) + 1);
				}
			}
			else if (toolslMenuInput.show.wasJustReleased)
			{
				consumeControl(toolslMenuInput.show);
				consumeControl(toolslMenuInput.select);

				if (spatialScrollData != null && spatialScrollData.passedMinDragActivationThreshold)
				{
					m_ToolsMenuUi.SelectHighlightedButton();
				}
				else if (Time.realtimeSinceStartup < m_AllowToolToggleBeforeThisTime)
				{
					// Allow for single press+release to cycle through tools
					m_ToolsMenuUi.SelectNextExistingToolButton();
					OnButtonClick();
				}

				CloseMenu();
			}
		}

		void OnButtonClick()
		{
			this.Pulse(node, m_ButtonClickPulse);
			this.SetSpatialHintState(SpatialHintModule.SpatialHintStateFlags.Hidden);
		}

		void OnButtonHover()
		{
			this.Pulse(node, m_ButtonHoverPulse);
		}

		void OnButtonSelected(Transform rayOrigin, Type buttonType)
		{
			this.SelectTool(rayOrigin, buttonType, false);
		}

		void CloseMenu()
		{
			this.Pulse(node, m_HidingPulse);
			this.EndSpatialScroll(); // Free the spatial scroll data owned by this object
		}
	}
}
#endif
