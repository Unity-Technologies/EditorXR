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
	sealed class PinnedToolsMenu : MonoBehaviour, IPinnedToolsMenu, IConnectInterfaces, IInstantiateUI,
		IControlHaptics, IUsesViewerScale, IControlSpatialHinting, ISetDefaultRayVisibility, IUsesRayOrigin
	{
		const int k_MenuButtonOrderPosition = 0; // A shared menu button position used in this particular ToolButton implementation
		const int k_ActiveToolOrderPosition = 1; // A active-tool button position used in this particular ToolButton implementation
		const int k_MaxButtonCount = 16; //

		[SerializeField]
		Sprite m_MainMenuIcon;

		[SerializeField]
		ActionMap m_MainMenuActionMap;

		[SerializeField]
		PinnedToolsMenuUI m_PinnedToolsMenuPrefab;

		[SerializeField]
		PinnedToolButton m_PinnedToolButtonTemplate;

		[SerializeField]
		HapticPulse m_ButtonClickPulse;

		[SerializeField]
		HapticPulse m_ButtonHoverPulse;

		[SerializeField]
		HapticPulse m_ActivationPulse; // The pulse performed when initial activating spatial selection, but not passing the trigger threshold

		[SerializeField]
		HapticPulse m_HidingPulse; // The pulse performed when ending a spatial selection

		Transform m_RayOrigin;
		Transform m_AlternateMenuOrigin;
		float allowToolToggleBeforeThisTime;
		float? continuedInputConsumptionStartTime;
		Vector3 m_SpatialScrollStartPosition;
		IPinnedToolButton m_MainMenuButton;
		PinnedToolsMenuUI m_PinnedToolsMenuUI;
		SpatialScrollModule.SpatialScrollData m_SpatialScrollData;

		public Transform menuOrigin { get; set; }
		List<IPinnedToolButton> buttons { get { return m_PinnedToolsMenuUI.buttons; } }
		public Dictionary<Type, Sprite> icons { get; set; }
		public int activeToolOrderPosition { get; private set; }
		public bool revealed { get; set; }
		public bool alternateMenuVisible { set { m_PinnedToolsMenuUI.moveToAlternatePosition = value; } }
		public Vector3 alternateMenuItem { get; private set; }

		public Action<Transform, int, bool> HighlightSingleButton { get; set; }
		public Action<Transform> SelectHighlightedButton { get; set; }
		public Action<Transform> deleteHighlightedButton { get; set; }
		public Action<Transform> onButtonHoverEnter { get; set; }
		public Action<Transform> onButtonHoverExit { get; set; }
		public Action<Type, Sprite, Node> createPinnedToolButton { get; set; }
		public Node? node { get; set; }
		public IPinnedToolButton previewToolButton { get { return m_MainMenuButton; } }

		public Transform rayOrigin
		{
			get { return m_RayOrigin; }
			set
			{
				m_RayOrigin = value;
				// UI is created after RayOrigin is set here
				// Ray origin is then set in CreatePinnedToolsUI()
				CreatePinnedToolsUI();
			}
		}

		public ActionMap actionMap
		{
			get { return m_MainMenuActionMap; }
		}

		public Transform alternateMenuOrigin
		{
			get { return m_AlternateMenuOrigin; }
			set
			{
				if (m_AlternateMenuOrigin == value)
					return;

				m_AlternateMenuOrigin = value;
			}
		}

		public event Action<Transform> hoverEnter;
		public event Action<Transform> hoverExit;
		public event Action<Transform> selected;

		void Awake()
		{
			createPinnedToolButton = CreatePinnedToolButton;
		}

		void OnDestroy()
		{
			this.SetDefaultRayVisibility(rayOrigin, true);
		}

		void CreatePinnedToolsUI()
		{
			Debug.LogWarning("Spawing pinned tools menu UI");
			m_PinnedToolsMenuUI = m_PinnedToolsMenuUI ?? this.InstantiateUI(m_PinnedToolsMenuPrefab.gameObject).GetComponent<PinnedToolsMenuUI>();
			m_PinnedToolsMenuUI.maxButtonCount = k_MaxButtonCount;
			m_PinnedToolsMenuUI.mainMenuActivatorSelected = this.MainMenuActivatorSelected;
			m_PinnedToolsMenuUI.rayOrigin = rayOrigin;
			m_PinnedToolsMenuUI.buttonHovered += OnButtonHover;
			m_PinnedToolsMenuUI.buttonClicked += OnButtonClick;
			m_PinnedToolsMenuUI.buttonSelected += OnButtonSelected;

			// Alternate menu origin isnt set when awake or start run
			var pinnedToolsUITransform = m_PinnedToolsMenuUI.transform;
			pinnedToolsUITransform.SetParent(m_AlternateMenuOrigin);
			pinnedToolsUITransform.localPosition = Vector3.zero;
			pinnedToolsUITransform.localRotation = Quaternion.identity;
		}

		void CreatePinnedToolButton(Type toolType, Sprite buttonIcon, Node node)
		{
			Debug.LogError("<color=green>SPAWNING pinned tool button for type of : </color>" + toolType);
			//var buttons = deviceData.buttons;
			if (buttons.Count >= k_MaxButtonCount) // Return if tooltype already occupies a pinned tool button
			{
				// TODO: kick out the oldest tool, and allow the new tool to become the active tool
				Debug.LogWarning("New pinned tool button cannot be added.  The maximum number of pinned tool buttons are currently being displayed");
				return;
			}

			// Select an existing ToolButton if the type is already present in a button
			if (buttons.Any( (x) => x.toolType == toolType))
			{
				m_PinnedToolsMenuUI.SelectExistingToolType(toolType);
				return;
			}

			// Before adding new button, offset each button to a position greater than the zeroth/active tool position
			//foreach (var pair in buttons)
			//{
		//		if (pair.Value.order != pair.Value.menuButtonOrderPosition) // don't move the main menu button
		//			pair.Value.order++;
		//	}

			//var button = SpawnPinnedToolButton(deviceData.inputDevice);
			//var buttonTransform = this.InstantiateUI(m_PinnedToolButtonTemplate.gameObject, m_PinnedToolsMenuUI.buttonContainer, false).transform;
			var buttonTransform = ObjectUtils.Instantiate(m_PinnedToolButtonTemplate.gameObject, m_PinnedToolsMenuUI.buttonContainer, false).transform;
			var button = buttonTransform.GetComponent<PinnedToolButton>();
			this.ConnectInterfaces(button);

			// Initialize button in alternate position if the alternate menu is hidden
			/*
			IPinnedToolButton mainMenu = null;
			if (toolType == typeof(IMainMenu))
				mainMenu = button;
			else
				buttons.TryGetValue(typeof(IMainMenu), out mainMenu);
			*/

			//button.moveToAlternatePosition = mainMenu != null && mainMenu.moveToAlternatePosition;
			//button.node = deviceData.node;
			button.rayOrigin = rayOrigin;
			button.toolType = toolType; // Assign Tool Type before assigning order
			button.icon = toolType != typeof(IMainMenu) ? buttonIcon : m_MainMenuIcon;
			button.highlightSingleButton = HighlightSingleButton;
			button.selectHighlightedButton = SelectHighlightedButton;
			//button.selected += OnMainMenuActivatorSelected;
			//button.hoverEnter += onButtonHoverExit;
			//button.hoverExit += onButtonHoverExit;
			button.rayOrigin = rayOrigin;

			if (toolType == typeof(IMainMenu))
				m_MainMenuButton = button;

			m_PinnedToolsMenuUI.AddButton(button, buttonTransform);
		}

		public void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl)
		{
			var buttonCount = buttons.Count; // The MainMenu button will be hidden, subtract 1 from the activeButtonCount
			if (buttonCount <= k_ActiveToolOrderPosition + 1)
				return;

			const float kAutoHideDuration = 10f;
			const float kAllowToggleDuration = 0.25f;
			var pinnedToolInput = (PinnedToolslMenuInput) input;

			if (pinnedToolInput.show.wasJustPressed)
				Debug.LogError("<color=black>SHOW pressed in PinnedToolButton</color>");

			if (pinnedToolInput.select.wasJustPressed)
				Debug.LogError("<color=black>SELECT pressed in PinnedToolButton</color>");

			if (pinnedToolInput.cancel.wasJustPressed)
				Debug.LogError("CANCELLING SPATIAL SELECTION!!!!");

			/*
			if (spatialDirection == null)
			{
				// Hide if no direction as been defined after a given duration
				Debug.LogWarning("Perform an increasing visual presence of visuals as time progresses, and the drag threshold hasn't been met.");
				m_PinnedToolsMenuUI.allButtonsVisible = false;
				this.SetSpatialHintControlObject(null); // Hide Spatial Hint Visuals

				return;
			}
			*/

			if (pinnedToolInput.show.wasJustPressed)
			{
				this.SetDefaultRayVisibility(rayOrigin, false);
				this.LockRay(rayOrigin, this);
				//consumeControl(pinnedToolInput.show);
				//m_PinnedToolsMenuUI.allButtonsVisible = true;
				m_SpatialScrollStartPosition = m_AlternateMenuOrigin.position;
				Debug.LogError("Start position : <color=green>" + m_SpatialScrollStartPosition + "</color>");
				allowToolToggleBeforeThisTime = Time.realtimeSinceStartup + kAllowToggleDuration;
				this.SetSpatialHintControlObject(rayOrigin);
				m_PinnedToolsMenuUI.spatiallyScrolling = true; // Triggers the display of the directional hint arrows

				//Dont show if the user hasnt passed the threshold in the given duration
			}
			else if (pinnedToolInput.show.isHeld && !pinnedToolInput.select.isHeld && !pinnedToolInput.select.wasJustPressed)
			{
				// Don't scroll if the trigger is held, allowing the user to setting on a single button to select with release
				if (pinnedToolInput.select.wasJustReleased)
				{
					Debug.LogError("<color=red>DELETING PinnedToolButton</color>");
					//selectHighlightedButton(rayOrigin);
					//OnActionButtonHoverExit(false);

					if (m_PinnedToolsMenuUI.DeleteHighlightedButton())
					{
						buttonCount = buttons.Count; // The MainMenu button will be hidden, subtract 1 from the activeButtonCount
						//if (buttonCount <= k_ActiveToolOrderPosition + 1)
							//return;

						//allowSpatialScrollBeforeThisTime = null;
						//spatialDirection = null; 
					}

					if (buttonCount <= k_ActiveToolOrderPosition + 1)
					{
						this.EndSpatialScroll(this);
						return;
					}
				}

				// normalized input should loop after reaching the 0.15f length
				buttonCount -= 1; // Decrement to disallow cycling through the main menu button
				m_SpatialScrollData = this.PerformSpatialScroll(this, node, m_SpatialScrollStartPosition, m_AlternateMenuOrigin.position, 0.25f, m_PinnedToolsMenuUI.buttons.Count, m_PinnedToolsMenuUI.maxButtonCount);
				var normalizedRepeatingPosition = m_SpatialScrollData.normalizedLoopingPosition;
				if (!Mathf.Approximately(normalizedRepeatingPosition, 0f))
				{
					if (!m_PinnedToolsMenuUI.allButtonsVisible)
					{
						m_PinnedToolsMenuUI.spatialDragDistance = m_SpatialScrollData.dragDistance;
						this.SetSpatialHintState(SpatialHintModule.SpatialHintStateFlags.Scrolling);
						m_PinnedToolsMenuUI.allButtonsVisible = true;
					}
					else if (m_SpatialScrollData.spatialDirection != null)// && m_PinnedToolsMenuUI.startingDragOrigin != m_SpatialScrollData.spatialDirection)
					{
						m_PinnedToolsMenuUI.startingDragOrigin = m_SpatialScrollData.spatialDirection;
					}

					m_PinnedToolsMenuUI.HighlightSingleButtonWithoutMenu((int)(buttonCount * normalizedRepeatingPosition) + 1);
					consumeControl(pinnedToolInput.show);
					consumeControl(pinnedToolInput.select);
				}
			}
			else if (pinnedToolInput.show.wasJustReleased)
			{
				const float kAdditionalConsumptionDuration = 0.25f;
				continuedInputConsumptionStartTime = Time.realtimeSinceStartup + kAdditionalConsumptionDuration;
				if (m_SpatialScrollData.passedMinDragActivationThreshold)
				{
					Debug.LogWarning("PinnedToolButton was just released");
					m_PinnedToolsMenuUI.SelectHighlightedButton();
					//m_PinnedToolsMenuUI.spatialDragDistance = 0f; // Triggers the display of the directional hint arrows
					consumeControl(pinnedToolInput.select);
					this.Pulse(node, m_HidingPulse);
				}
				else if (Time.realtimeSinceStartup < allowToolToggleBeforeThisTime)
				{
					// Allow for single press+release to cycle through tools
					m_PinnedToolsMenuUI.SelectNextExistingToolButton();
					OnButtonClick();
				}

				this.SetDefaultRayVisibility(rayOrigin, true);
				this.UnlockRay(rayOrigin, this);
				this.SetSpatialHintState(SpatialHintModule.SpatialHintStateFlags.Hidden);
				this.EndSpatialScroll(this); // Free the spatial scroll data owned by this object
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
			Debug.LogError("<color=green>Selecting Tool in PinnedToolsMenu</color> : " + buttonType.ToString());
			this.SelectTool(rayOrigin, buttonType);
		}
	}
}
#endif