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
	sealed class PinnedToolsMenu : MonoBehaviour, IPinnedToolsMenu, IConnectInterfaces, IInstantiateUI, IControlHaptics, IUsesViewerScale, IControlSpatialHinting, ISetDefaultRayVisibility
	{
		const int k_MenuButtonOrderPosition = 0; // A shared menu button position used in this particular ToolButton implementation
		const int k_ActiveToolOrderPosition = 1; // A active-tool button position used in this particular ToolButton implementation
		const int k_MaxButtonCount = 16; //

		[SerializeField]
		Sprite m_UnityIcon;

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

		PinnedToolsMenuUI m_PinnedToolsMenuUI;

		//public int menuButtonOrderPosition { get { return k_MenuButtonOrderPosition; } }
		//public int activeToolOrderPosition { get { return k_ActiveToolOrderPosition; } }

		Transform m_RayOrigin;
		Transform m_AlternateMenuOrigin;
		IPinnedToolButton m_MainMenuButton;
		float? continuedInputConsumptionStartTime;
		Vector3 m_SpatialScrollStartPosition;
		Vector3 previousWorldPosition;
		float allowToolToggleBeforeThisTime;
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
		public Action<Transform> mainMenuActivatorSelected { get; set; }
		public Node? node { get; set; }

		public Action<Transform, Type> selectTool { get; set; }
		public IPinnedToolButton previewToolButton { get { return m_MainMenuButton; } }

		public Transform rayOrigin
		{
			get { return m_RayOrigin; }
			set
			{
				m_RayOrigin = value;
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
		/*
		public bool visible { get; set; }
		public GameObject menuContent { get; private set; }
		public List<ActionMenuData> menuActions { get; set; }
		public Transform rayOrigin { get; set; }
		public event Action<Transform> itemWasSelected;
		*/

		// Spatial Hint Module implementation
		

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
			if (m_PinnedToolsMenuUI == null)
				m_PinnedToolsMenuUI = this.InstantiateUI(m_PinnedToolsMenuPrefab.gameObject).GetComponent<PinnedToolsMenuUI>();

			this.ConnectInterfaces(m_PinnedToolsMenuUI);
			m_PinnedToolsMenuUI.maxButtonCount = k_MaxButtonCount;
			m_PinnedToolsMenuUI.mainMenuActivatorSelected = mainMenuActivatorSelected;
			m_PinnedToolsMenuUI.rayOrigin = rayOrigin;
			m_PinnedToolsMenuUI.buttonHovered += OnButtonHover;
			m_PinnedToolsMenuUI.buttonClicked += OnButtonClick;

			// Alternate menu origin isnt set when awake or start run
			var pinnedToolsUITransform = m_PinnedToolsMenuUI.transform;
			pinnedToolsUITransform.SetParent(m_AlternateMenuOrigin);
			pinnedToolsUITransform.localPosition = Vector3.zero;
			pinnedToolsUITransform.localRotation = Quaternion.identity;
		}

		public void CreatePinnedToolButton(Type toolType, Sprite buttonIcon, Node node)
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
			button.icon = toolType != typeof(IMainMenu) ? buttonIcon : m_UnityIcon;
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

					//KEEP deleteHighlightedButton(rayOrigin); // convert to method in IPinnedToolsMenu interface
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
				//var normalizedRepeatingPosition = processSpatialScrolling(m_SpatialScrollStartPosition, m_AlternateMenuOrigin.position, 0.25f, m_PinnedToolsMenuUI.buttons.Count, m_PinnedToolsMenuUI.maxButtonCount);
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

			// cache current position for delta comparison on next frame for fine tuned scrolling with low velocity
			previousWorldPosition = transform.position;
		}

		/*
		float processSpatialScrolling(Vector3 startingPosition, Vector3 currentPosition, float repeatingScrollLengthRange, int scrollableItemCount, int maxItemCount = -1)
		{
			var normalizedLoopingPosition = 0f;
			var directionVector = currentPosition - startingPosition;
			const float kMaxFineTuneVelocity = 0.0005f;
			if (spatialDirection == null)
			{
				var newDirectionVectorThreshold = 0.0175f * this.GetViewerScale(); // Initial magnitude beyond which spatial scrolling will be evaluated
				var dragMagnitude = Vector3.Magnitude(directionVector);
				var dragPercentage = dragMagnitude / newDirectionVectorThreshold;
				var repeatingPulseAmount = Mathf.Sin(Time.realtimeSinceStartup * 20) > 0.5f ? 1f : 0f;
				m_PinnedToolsMenuUI.spatialDragDistance = dragMagnitude > 0 ? dragPercentage : 0f; // Set normalized value representing how much of the pre-scroll drag amount has occurred
				this.Pulse(node, m_ActivationPulse, repeatingPulseAmount, repeatingPulseAmount);
				if (dragMagnitude > newDirectionVectorThreshold)
				{
					spatialDirection = directionVector; // initialize vector defining the spatial scroll direciton
					m_PinnedToolsMenuUI.startingDragOrigin = spatialDirection;
				}
			}
			else
			{
				var rawVelocity = (previousWorldPosition - transform.position).sqrMagnitude;
				var velocity = rawVelocity * Time.unscaledDeltaTime;
				if (velocity < kMaxFineTuneVelocity) // && velocity > kMinFineTuneVelocity)
				{
					// OFfset the vector increasingly as velocity slows, in order to lessen the perceived scrolling magnitude
					//spatialDirection -= spatialDirection.Value * ( 100f * (kMaxFineTuneVelocity - velocity)); // TODO: support this offset in either direction/inverse
					//spatialScrollStartPosition -= spatialScrollStartPosition * ( 0.1f * (kMaxFineTuneVelocity - velocity));
					//repeatingScrollLengthRange += repeatingScrollLengthRange * (10000f * (kMaxFineTuneVelocity - velocity));
					//Debug.LogError("<color=red>" + repeatingScrollLengthRange + "</color>");
				}

				//Debug.LogError(directionVector.magnitude);
				var projectedAmount = Vector3.Project(directionVector, spatialDirection.Value).magnitude / this.GetViewerScale();
				normalizedLoopingPosition = (Mathf.Abs(projectedAmount * (maxItemCount / scrollableItemCount)) % repeatingScrollLengthRange) * (1 / repeatingScrollLengthRange);

				//Debug.LogError("<color=green>" + velocity + "</color>");
				//if (velocity < kMaxFineTuneVelocity && velocity > kMinFineTuneVelocity)
				//{
				//Debug.LogError("<color=green>" + projectedAmount + "</color> : Spatial Direction : " + spatialDirection.Value);
				// OFfset the vector increasingly as velocity slows, in order to lessen the perceived scrolling magnitude
				//spatialDirection -= spatialDirection.Value * ( 100f * (kMaxFineTuneVelocity - velocity)); // TODO: support this offset in either direction/inverse
				//spatialScrollStartPosition -= spatialScrollStartPosition * ( 0.1f * (kMaxFineTuneVelocity - velocity));
				//}
			}

			return normalizedLoopingPosition;
		}
		*/

		void OnButtonClick()
		{
			this.Pulse(node, m_ButtonClickPulse);
			this.SetSpatialHintState(SpatialHintModule.SpatialHintStateFlags.Hidden);
		}

		void OnButtonHover()
		{
			this.Pulse(node, m_ButtonHoverPulse);
		}
	}
}
#endif