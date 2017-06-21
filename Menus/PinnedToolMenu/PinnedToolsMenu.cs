#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using UnityEditor.Experimental.EditorVR;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Helpers;
using UnityEditor.Experimental.EditorVR.UI;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR.Menus
{
	sealed class PinnedToolsMenu : MonoBehaviour, IPinnedToolsMenu, IConnectInterfaces, IInstantiateUI, IControlHaptics
	{
		const int k_MenuButtonOrderPosition = 0; // A shared menu button position used in this particular ToolButton implementation
		const int k_ActiveToolOrderPosition = 1; // A active-tool button position used in this particular ToolButton implementation
		const int k_MaxButtonCount = 16; //

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
		HapticPulse m_ScrollingPulse; // The pulse performed when spatially scrolling

		[SerializeField]
		HapticPulse m_HidingPulse; // The pulse performed when ending a spatial selection

		PinnedToolsMenuUI m_PinnedToolsMenuUI;

		//public int menuButtonOrderPosition { get { return k_MenuButtonOrderPosition; } }
		//public int activeToolOrderPosition { get { return k_ActiveToolOrderPosition; } }

		Transform m_RayOrigin;
		Transform m_AlternateMenuOrigin;

		public Transform menuOrigin { get; set; }
		List<IPinnedToolButton> buttons { get { return m_PinnedToolsMenuUI.buttons; } }
		public Dictionary<Type, Sprite> icons { get; set; }
		public int activeToolOrderPosition { get; private set; }
		public bool revealed { get; set; }
		public bool moveToAlternatePosition { set { m_PinnedToolsMenuUI.moveToAlternatePosition = value; } }
		public Type previewToolType { get; set; }
		public Vector3 alternateMenuItem { get; private set; }

		public Action<Transform, int, bool> HighlightSingleButton { get; set; }
		public Action<Transform> SelectHighlightedButton { get; set; }
		public Action<Transform> deleteHighlightedButton { get; set; }
		public Action<Transform> onButtonHoverEnter { get; set; }
		public Action<Transform> onButtonHoverExit { get; set; }
		public Action<Transform, GradientPair> highlightDevice { get; set; }
		public Action<Type, Sprite, Node> createPinnedToolButton { get; set; }
		public Action<Transform> mainMenuActivatorSelected { get; set; }
		public Node? node { get; set; }

		public Action<Transform, Type> selectTool { get; set; }

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

		void Awake()
		{
			createPinnedToolButton = CreatePinnedToolButton;
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
			button.icon = buttonIcon;
			button.highlightSingleButton = HighlightSingleButton;
			button.selectHighlightedButton = SelectHighlightedButton;
			//button.selected += OnMainMenuActivatorSelected;
			//button.hoverEnter += onButtonHoverExit;
			//button.hoverExit += onButtonHoverExit;
			button.rayOrigin = rayOrigin;

			m_PinnedToolsMenuUI.AddButton(button, buttonTransform);
		}

		float? continuedInputConsumptionStartTime;
		Vector3 spatialScrollStartPosition;
		Vector3? spatialDirection = null;
		Vector3 previousWorldPosition;
		float? allowSpatialScrollBeforeThisTime = null; // use to hide menu if input is consumed externally and no spatialDirection is define within a given duration
		float allowToolToggleBeforeThisTime;
		public void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl)
		{
			var buttonCount = buttons.Count; // The MainMenu button will be hidden, subtract 1 from the activeButtonCount
			if (buttonCount <= k_ActiveToolOrderPosition + 1)
				return;

			const float kAutoHideDuration = 1f;
			const float kAllowToggleDuration = 0.25f;
			var pinnedToolInput = (PinnedToolslMenuInput) input;
			/*
			if (continuedInputConsumptionStartTime != null)
			{
				// Continue consumption of the "show" input for period of time after releasing the button
				consumeControl(pinnedToolInput.show);
				if (Time.realtimeSinceStartup > continuedInputConsumptionStartTime.Value)
					continuedInputConsumptionStartTime = null;
			}
			*/

			if (pinnedToolInput.show.wasJustPressed)
				Debug.LogError("<color=black>SHOW pressed in PinnedToolButton</color>");

			if (pinnedToolInput.select.wasJustPressed)
				Debug.LogError("<color=black>SELECT pressed in PinnedToolButton</color>");

			if (allowSpatialScrollBeforeThisTime != null && spatialDirection == null && Time.realtimeSinceStartup > allowSpatialScrollBeforeThisTime.Value)
			{
				// Hide if no direction as been defined after a given duration
				m_PinnedToolsMenuUI.allButtonsVisible = false;
				allowSpatialScrollBeforeThisTime = null;
				return;
			}

			if (pinnedToolInput.show.wasJustPressed)
			{
				//consumeControl(pinnedToolInput.show);
				//m_PinnedToolsMenuUI.allButtonsVisible = true;
				spatialScrollStartPosition = m_PinnedToolsMenuUI.transform.position;
				allowSpatialScrollBeforeThisTime = Time.realtimeSinceStartup + kAutoHideDuration;
				allowToolToggleBeforeThisTime = Time.realtimeSinceStartup + kAllowToggleDuration;
				m_PinnedToolsMenuUI.spatialDragDistance = 0f; // Triggers the display of the directional hint arrows

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
						allowSpatialScrollBeforeThisTime = null;
						spatialDirection = null;
						return;
					}
				}

				// normalized input should loop after reaching the 0.15f length
				buttonCount -= 1; // Decrement to disallow cycling through the main menu button
				var normalizedRepeatingPosition = processSpatialScrolling(spatialScrollStartPosition, m_PinnedToolsMenuUI.transform.position, 0.15f, true);
				if (!Mathf.Approximately(normalizedRepeatingPosition, 0f))
				{
					if (!m_PinnedToolsMenuUI.allButtonsVisible)
						m_PinnedToolsMenuUI.allButtonsVisible = true;

					m_PinnedToolsMenuUI.HighlightSingleButtonWithoutMenu((int) (buttonCount * normalizedRepeatingPosition) + 1);
					consumeControl(pinnedToolInput.show);
					consumeControl(pinnedToolInput.select);
					
				}
				else // User hasn't dragged beyond the trigger magnitude; spatial scrolling hasn't been activated yet
				{
					this.Pulse(node, m_ActivationPulse);
				}
			}
			else if (pinnedToolInput.show.wasJustReleased)
			{
				if (allowSpatialScrollBeforeThisTime == null)
					return;

				const float kAdditionalConsumptionDuration = 0.25f;
				continuedInputConsumptionStartTime = Time.realtimeSinceStartup + kAdditionalConsumptionDuration;
				allowSpatialScrollBeforeThisTime = null;
				if (spatialDirection != null)
				{
					m_PinnedToolsMenuUI.SelectHighlightedButton();
					spatialDirection = null;
					consumeControl(pinnedToolInput.select);
					this.Pulse(node, m_HidingPulse);
				}
				else if (Time.realtimeSinceStartup < allowToolToggleBeforeThisTime)
				{
					// Allow for single press+release to cycle through tools
					m_PinnedToolsMenuUI.SelectNextExistingToolButton();
					OnButtonClick();
				}
			}

			// cache current position for delta comparison on next frame for fine tuned scrolling with low velocity
			previousWorldPosition = transform.position;
		}

		// TODO ADD SUPPORT FOR VIEWERSCALE SIZE CHANGES
		// TODO refact into ISpatialScrolling interface; allow axis locking/selection/isolation
		float processSpatialScrolling(Vector3 startingPosition, Vector3 currentPosition, float repeatingScrollLengthRange, bool velocitySensitive)
		{
			var normalizedLoopingPosition = 0f;
			var directionVector = currentPosition - startingPosition;
			const float kMaxFineTuneVelocity = 0.0005f;
			//const float kMinFineTuneVelocity = 0.000001f;
			if (spatialDirection == null)
			{
				const float kNewDirectionVectorThreshold = 0.0175f; // Initial magnitude beyond which spatial scrolling will be evaluated
				var dragAmount = Vector3.Magnitude(directionVector);
				m_PinnedToolsMenuUI.spatialDragDistance = dragAmount > 0 ? dragAmount / kNewDirectionVectorThreshold : 0f; // Set normalized value representing how much of the pre-scroll drag amount has occurred
				if (dragAmount > kNewDirectionVectorThreshold)
				{
					spatialDirection = directionVector; // initialize vector defining the spatial scroll direciton
					m_PinnedToolsMenuUI.spatialDirectionVector = spatialDirection;
				}
			}
			else
			{
				var rawVelocity = (previousWorldPosition - transform.position).sqrMagnitude;
				var velocity = rawVelocity * Time.unscaledDeltaTime;
				//Debug.LogError("<color=green>" + velocity + "</color> : Raw Velocity : " + rawVelocity + " : unscaled time : " + Time.unscaledDeltaTime);

				if (velocity < kMaxFineTuneVelocity) // && velocity > kMinFineTuneVelocity)
				{
					// OFfset the vector increasingly as velocity slows, in order to lessen the perceived scrolling magnitude
					//spatialDirection -= spatialDirection.Value * ( 100f * (kMaxFineTuneVelocity - velocity)); // TODO: support this offset in either direction/inverse
					//spatialScrollStartPosition -= spatialScrollStartPosition * ( 0.1f * (kMaxFineTuneVelocity - velocity));
					//repeatingScrollLengthRange += repeatingScrollLengthRange * (10000f * (kMaxFineTuneVelocity - velocity));
					//Debug.LogError("<color=red>" + repeatingScrollLengthRange + "</color>");
				}

				//Debug.LogError(directionVector.magnitude);
				var projectedAmount = Vector3.Project(directionVector, spatialDirection.Value).magnitude;
				normalizedLoopingPosition = (Mathf.Abs(projectedAmount) % repeatingScrollLengthRange) * (1 / repeatingScrollLengthRange);

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

		void OnButtonClick()
		{
			this.Pulse(node, m_ButtonClickPulse);
		}

		void OnButtonHover()
		{
			this.Pulse(node, m_ButtonHoverPulse);
		}
	}
}
#endif