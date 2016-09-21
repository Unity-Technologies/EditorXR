using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;
using UnityEngine.Events;
using UnityEngine.InputNew;
using UnityEngine.UI;
using UnityEngine.VR.Actions;
using UnityEngine.VR.Handles;
using UnityEngine.VR.Tools;
using UnityEngine.VR.Utilities;

namespace UnityEngine.VR.Menus
{
	public class MainMenu : MonoBehaviour, IMainMenu, IInstantiateUI, ICustomActionMap, ICustomRay, ILockRay, IMenuOrigins, IUsesActions
	{
		public ActionMap actionMap { get {return m_MainMenuActionMap; } }
		[SerializeField]
		private ActionMap m_MainMenuActionMap;

		public ActionMapInput actionMapInput
		{
			get { return m_MainMenuInput; }
			set { m_MainMenuInput = (MainMenuInput) value; }
		}
		[SerializeField]
		private MainMenuInput m_MainMenuInput;

		// HACK: As of now Awake/Start get called together, so we have to cache the value and apply it later
		public Transform alternateMenuOrigin
		{
			get
		{
				return m_AlternateMenuOrigin;
		}
			set
			{
				m_AlternateMenuOrigin = value;
				if (m_MainMenuUI)
					m_MainMenuUI.alternateMenuOrigin = value;
			}
		}
		private Transform m_AlternateMenuOrigin;

		// HACK: As of now Awake/Start get called together, so we have to cache the value and apply it later
		public Transform menuOrigin
		{
			get { return m_MenuOrigin; }
			set
			{
				m_MenuOrigin = value;
				if (m_MainMenuUI)
					m_MainMenuUI.menuOrigin = value;
			}
		}
		private Transform m_MenuOrigin;

		[SerializeField]
		private MainMenuUI m_MainMenuPrefab;

		private MainMenuUI m_MainMenuUI;
		private BaseHandle m_MenuButton;
		private float m_RotationInputStartTime;
		private float m_RotationInputStartValue;
		private float m_RotationInputIdleTime;
		private float m_LastRotationInput;

		/// <summary>
		/// Event raised when showing the Main Menu
		/// This allows for informing the radial menu, or any other object of the Main Menu being shown
		/// </summary>
		public event EventHandler onShow;

		public Func<GameObject, GameObject> instantiateUI { private get; set; }
		public Transform rayOrigin { private get; set; }
		public Action hideDefaultRay { private get; set; }
		public Action showDefaultRay { private get; set; }
		public Func<object, bool> lockRay { private get; set; }
		public Func<object, bool> unlockRay { private get; set; }
		public List<Type> menuTools { private get; set; }
		public Func<Node, Type, bool> selectTool { private get; set; }
		public List<Type> menuWorkspaces { private get; set; }
		public Action<Type> createWorkspace { private get; set; }
		public Node? node { private get; set; }
		public Action setup { get { return Setup; } }
		public List<IAction> actions { private get; set; }
		public Func<IAction, bool> performAction { get; set; }
		public Action hide { get; private set; }
		public Action show { get; private set; }

		public bool visible
		{
			get { return m_MainMenuUI.visible; }
			set
			{
				if (m_MainMenuUI.visible != value)
				{
					m_MainMenuUI.visible = value;
					if (value == true)
					{
						hideDefaultRay();
						lockRay(this);

						if (onShow != null)
							onShow(this, null);
					}
					else
					{
						unlockRay(this);
						showDefaultRay();
					}
				}
			}
		}

		public void Setup()
		{
			m_MainMenuUI = instantiateUI(m_MainMenuPrefab.gameObject).GetComponent<MainMenuUI>();
			m_MainMenuUI.instantiateUI = instantiateUI;
			m_MainMenuUI.alternateMenuOrigin = alternateMenuOrigin;
			m_MainMenuUI.menuOrigin = menuOrigin;
			//m_MainMenuUI.menuButtonSelected = () => { visible = !visible; }; // allow the menu button in the UI to enable/disable the main menu
			m_MainMenuUI.Setup();
			
			CreateToolButtons(menuTools);
		}

		private void Update()
		{
			var rotationInput = -m_MainMenuInput.rotate.rawValue;
			if (Mathf.Approximately(rotationInput, m_LastRotationInput) && Mathf.Approximately(rotationInput, 0f))
			{
				m_RotationInputIdleTime += Time.unscaledDeltaTime;
			}
			else
			{
				const float kFlickDeltaThreshold = 0.5f;
				const float kRotationInputIdleDurationThreshold = 0.05f; // Limits how often a flick can happen

				// Track values for a new rotation when input has changed
				if (m_RotationInputIdleTime > kRotationInputIdleDurationThreshold)
				{
					m_RotationInputStartTime = Time.realtimeSinceStartup;
					// Low sampling can affect our latch value, so sometimes the last rotation is a better choice because
					// the current rotation may be high by the time it is sampled
					m_RotationInputStartValue = Mathf.Abs(rotationInput) < Mathf.Abs(m_LastRotationInput) ? rotationInput : m_LastRotationInput;
				}

				const float kFlickDurationThreshold = 0.3f;

				// Perform a quick single face rotation if a quick flick of the input axis occurred
				float flickRotation = rotationInput - m_RotationInputStartValue;
				if (Mathf.Abs(flickRotation) >= kFlickDeltaThreshold && (Time.realtimeSinceStartup - m_RotationInputStartTime) < kFlickDurationThreshold)
				{
					m_MainMenuUI.targetFaceIndex = m_MainMenuUI.targetFaceIndex + (int) Mathf.Sign(flickRotation);

					// Don't allow another flick until rotation resets
					m_RotationInputStartTime = 0f;
				}
				else
				{
					const float kRotationSpeed = 250;

					// Otherwise, apply manual rotation to the main menu faces
					m_MainMenuUI.targetRotation += rotationInput * kRotationSpeed * Time.unscaledDeltaTime;
				}

				// Reset the idle time if we are no longer idle (i.e. rotation is happening)
				m_RotationInputIdleTime = 0f;
			}

			m_LastRotationInput = rotationInput;
		}

		private void OnDisable()
		{
			unlockRay(this);
		}

		private void OnDestroy()
		{
			U.Object.Destroy(m_MainMenuUI.gameObject);

			unlockRay(this);
			showDefaultRay();
		}

		private void CreateToolButtons(List<Type> toolTypes)
		{
			foreach (var type in toolTypes)
			{
				var buttonData = new MainMenuUI.ButtonData();
				buttonData.name = type.Name;

				var customMenuAttribute = (MainMenuItemAttribute)type.GetCustomAttributes(typeof(MainMenuItemAttribute), false).FirstOrDefault();
				if (customMenuAttribute != null)
				{
					buttonData.name = customMenuAttribute.name;
					buttonData.sectionName = customMenuAttribute.sectionName;
					buttonData.description = customMenuAttribute.description;
				}

				var toolType = type; // Local variable for proper closure
				m_MainMenuUI.CreateToolButton(buttonData, (b) =>
				{
					b.button.onClick.RemoveAllListeners();
					b.button.onClick.AddListener(() =>
					{
						if (visible && b.node.HasValue)
							selectTool(b.node.Value, toolType);
					});
				});
			}

			m_MainMenuUI.SetupMenuFaces();
		}

		private void Show()
		{
			Debug.LogError("SHOW CALLED IN MAIN MENU");
		}

		private void Hide()
		{
			Debug.LogError("HIDE CALLED IN MAIN MENU");
		}

		/*
		public void MenuActivatorToAlternatePosition(object sender, EventArgs eventArgs)
		{
			Debug.LogError("Move main menu activator to alternate position!");
			m_MainMenuUI.activatorButtonMoveAway = true; // TODO: handle for returning button
		}

		public void MenuActivatorToOriginalPosition(object sender, EventArgs eventArgs)
		{
			Debug.LogError("Move main menu activator to original position!");
			m_MainMenuUI.activatorButtonMoveAway = false; // TODO: handle for returning button
		}
		*/
	}
}