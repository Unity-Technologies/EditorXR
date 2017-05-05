#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor.Experimental.EditorVR;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Helpers;
using UnityEditor.Experimental.EditorVR.UI;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR.Menus
{
	sealed class PinnedToolsMenu : MonoBehaviour, IPinnedToolsMenu, IConnectInterfaces, IInstantiateUI
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

		PinnedToolsMenuUI m_PinnedToolsMenuUI;

		//public int menuButtonOrderPosition { get { return k_MenuButtonOrderPosition; } }
		//public int activeToolOrderPosition { get { return k_ActiveToolOrderPosition; } }

		Transform m_RayOrigin;

		public Transform menuOrigin { get; set; }
		public Dictionary<Type, IPinnedToolButton> pinnedToolButtons { get; set; }
		public Dictionary<Type, Sprite> icons { get; set; }
		public int activeToolOrderPosition { get; private set; }
		public bool revealed { get; set; }
		public bool moveToAlternatePosition { get; set; }
		public Type previewToolType { get; set; }
		public Vector3 alternateMenuItem { get; private set; }
		public Node node { set { m_PinnedToolsMenuUI.node = value; } }
		public Action<Transform, int, bool> HighlightSingleButton { get; set; }
		public Action<Transform> SelectHighlightedButton { get; set; }
		public Action<Transform> deleteHighlightedButton { get; set; }
		public Action<Transform> onButtonHoverEnter { get; set; }
		public Action<Transform> onButtonHoverExit { get; set; }
		public Action<Transform, GradientPair> highlightDevice { get; set; }
		public Action<Type, Sprite, Node> createPinnedToolButton { get; set; }

		Transform m_AlternateMenuOrigin;

		public Action<Transform, Type> selectTool
		{
			set { m_PinnedToolsMenuUI.selectTool = value; }
		}

		public Transform rayOrigin
		{
			get { return m_RayOrigin; }
			set
			{
				m_RayOrigin = value;
				m_PinnedToolsMenuUI.rayOrigin = value;
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

				if (m_PinnedToolsMenuUI == null)
					m_PinnedToolsMenuUI = this.InstantiateUI(m_PinnedToolsMenuPrefab.gameObject).GetComponent<PinnedToolsMenuUI>();

				m_PinnedToolsMenuUI.maxButtonCount = k_MaxButtonCount;

				var pinnedToolsUITransform = m_PinnedToolsMenuUI.transform;
				pinnedToolsUITransform.SetParent(m_AlternateMenuOrigin);
				pinnedToolsUITransform.localPosition = Vector3.zero;
				pinnedToolsUITransform.localRotation = Quaternion.identity;
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
			pinnedToolButtons = new Dictionary<Type, IPinnedToolButton>();
			createPinnedToolButton = CreatePinnedToolButton;
		}

		public void CreatePinnedToolButton(Type toolType, Sprite buttonIcon, Node node)
		{
			Debug.LogWarning("<color=green>SPAWNING pinned tool button for type of : </color>" + toolType);
			//var pinnedToolButtons = deviceData.pinnedToolButtons;
			if (pinnedToolButtons.ContainsKey(toolType) || pinnedToolButtons.Count >= k_MaxButtonCount) // Return if tooltype already occupies a pinned tool button
				return;

			// Before adding new button, offset each button to a position greater than the zeroth/active tool position
			//foreach (var pair in pinnedToolButtons)
			//{
		//		if (pair.Value.order != pair.Value.menuButtonOrderPosition) // don't move the main menu button
		//			pair.Value.order++;
		//	}

			//var button = SpawnPinnedToolButton(deviceData.inputDevice);
			//var buttonTransform = this.InstantiateUI(m_PinnedToolButtonTemplate.gameObject, m_PinnedToolsMenuUI.buttonContainer, false).transform;
			var buttonTransform = ObjectUtils.Instantiate(m_PinnedToolButtonTemplate.gameObject, m_PinnedToolsMenuUI.buttonContainer, false).transform;
			var button = buttonTransform.GetComponent<PinnedToolButton>();
			this.ConnectInterfaces(button);
			pinnedToolButtons.Add(toolType, button);

			// Initialize button in alternate position if the alternate menu is hidden
			/*
			IPinnedToolButton mainMenu = null;
			if (toolType == typeof(IMainMenu))
				mainMenu = button;
			else
				pinnedToolButtons.TryGetValue(typeof(IMainMenu), out mainMenu);
			*/

			//button.moveToAlternatePosition = mainMenu != null && mainMenu.moveToAlternatePosition;
			//button.node = deviceData.node;
			button.rayOrigin = rayOrigin;
			button.toolType = toolType; // Assign Tool Type before assigning order
			button.icon = buttonIcon;
			button.deleteHighlightedButton = deleteHighlightedButton;
			button.highlightSingleButton = HighlightSingleButton;
			button.selectHighlightedButton = SelectHighlightedButton;
			//button.selected += OnMainMenuActivatorSelected;
			button.hoverEnter += onButtonHoverExit;
			button.hoverExit += onButtonHoverExit;
			button.rayOrigin = rayOrigin;

			m_PinnedToolsMenuUI.AddButton(button, buttonTransform);
		}

		Vector3 spatialScrollStartPosition;
		Vector3? spatialDirection = null;
		Vector3 previousWorldPosition;
		public void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl)
		{
			var buttonCount = pinnedToolButtons.Count; // The MainMenu button will be hidden, subtract 1 from the activeButtonCount
			if (buttonCount <= k_ActiveToolOrderPosition + 1)
				return;

			//Debug.LogError("<color=BLUE>PinnedToolButton</color>");
			var pinnedToolInput = (PinnedToolslMenuInput) input;

			if (pinnedToolInput.show.wasJustPressed)
				Debug.LogError("<color=black>SHOW pressed in PinnedToolButton</color>");

			if (pinnedToolInput.select.wasJustPressed)
				Debug.LogError("<color=black>SELECT pressed in PinnedToolButton</color>");

			if (pinnedToolInput.show.wasJustPressed)
			{
				//Debug.LogError("<color=yellow>Processing input in PinnedToolButton</color>");
				//consumeControl(directSelectInput.select);
				m_PinnedToolsMenuUI.allButtonsVisible = true;
				spatialScrollStartPosition = m_PinnedToolsMenuUI.transform.position;
			}
			else if (pinnedToolInput.show.isHeld && !pinnedToolInput.select.isHeld && !pinnedToolInput.select.wasJustPressed)
			{
				if (pinnedToolInput.select.wasJustReleased)
				{
					//selectHighlightedButton(rayOrigin);
					//OnActionButtonHoverExit(false);
					deleteHighlightedButton(rayOrigin);
					m_PinnedToolsMenuUI.allButtonsVisible = false;
					spatialDirection = null;
					return;
				}

				// normalized input should loop after reaching the 0.15f length
				var normalizedRepeatingPosition = processSpatialScrolling(spatialScrollStartPosition, m_PinnedToolsMenuUI.transform.position, 0.15f, true);
				m_PinnedToolsMenuUI.HighlightSingleButton((int)(buttonCount * normalizedRepeatingPosition));
				consumeControl(pinnedToolInput.show);
				consumeControl(pinnedToolInput.select);
			}
			else if (pinnedToolInput.show.wasJustReleased)
			{
				m_PinnedToolsMenuUI.allButtonsVisible = false;
				m_PinnedToolsMenuUI.SelectHighlightedButton();
				spatialDirection = null;
				consumeControl(pinnedToolInput.select);
			}

			// cache current position for delta comparison on next frame for fine tuned scrolling with low velocity
			previousWorldPosition = transform.position;
		}

		// TODO ADD SUPPORT FOR VIEWERSCALE SIZE CHANGES
		// TODO refact into ISpatialScrolling interface; allow axis locking/selection/isolation
		float processSpatialScrolling(Vector3 startingPosition, Vector3 currentPosition, float repeatingScrollLengthRange,bool velocitySensitive)
		{
			var normalizedLoopingPosition = 0f;
			var directionVector = currentPosition - startingPosition;
			const float kMaxFineTuneVelocity = 0.0005f;
			//const float kMinFineTuneVelocity = 0.000001f;
			if (spatialDirection == null)
			{
				const float newDirectionVectorThreshold =
					0.025f; // Initial magnitude beyond which spatial scrolling will be evaluated
				directionVector = currentPosition - startingPosition;
				if (Vector3.Magnitude(directionVector) > newDirectionVectorThreshold)
					spatialDirection = directionVector; // initialize vector defining the spatial scroll direciton
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
	}
}
#endif