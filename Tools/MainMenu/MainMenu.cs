using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputNew;
using UnityEngine.VR.Actions;
using UnityEngine.VR.Handles;
using UnityEngine.VR.Tools;
using UnityEngine.VR.Utilities;
using UnityEngine.VR.Workspaces;

namespace UnityEngine.VR.Menus
{
	public class MainMenu : MonoBehaviour, IMainMenu, IConnectInterfaces, IInstantiateUI, ICreateWorkspace, ICustomActionMap, ICustomRay, ILockRay, IMenuOrigins
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

		public bool visible
		{
			get { return m_Visible.HasValue && m_Visible.Value; }
			set
			{
				if (!m_Visible.HasValue || m_Visible != value)
				{
					m_Visible = value;

					if (m_MainMenuUI)
						m_MainMenuUI.visible = value;

					if (value)
					{
						hideDefaultRay();
						lockRay(this);
					}
					else
					{
						unlockRay(this);
						showDefaultRay();
					}

					menuVisibilityChanged(this);
				}
			}
		}
		private bool? m_Visible;

		[SerializeField]
		private MainMenuUI m_MainMenuPrefab;

		private MainMenuUI m_MainMenuUI;
		private float m_RotationInputStartTime;
		private float m_RotationInputStartValue;
		private float m_RotationInputIdleTime;
		private float m_LastRotationInput;
		float m_RotationDragStartValue;
		bool m_RotationDragThresholdExceeded;

		public Func<GameObject, GameObject> instantiateUI { private get; set; }
		public Transform rayOrigin { private get; set; }
		public Action hideDefaultRay { private get; set; }
		public Action showDefaultRay { private get; set; }
		public Func<object, bool> lockRay { private get; set; }
		public Func<object, bool> unlockRay { private get; set; }
		public List<Type> menuTools { private get; set; }
		public Func<Node, Type, bool> selectTool { private get; set; }
		public List<Type> menuWorkspaces { private get; set; }
		public List<IModule> menuModules { private get; set; }
		public List<ActionMenuData> menuActions { get; set; }
		public CreateWorkspaceDelegate createWorkspace { private get; set; }
		public Node? node { private get; set; }
		public event Action<IMainMenu> menuVisibilityChanged = delegate {};
		public Action<object> connectInterfaces { private get; set; }

		void Start()
		{
			m_MainMenuUI = instantiateUI(m_MainMenuPrefab.gameObject).GetComponent<MainMenuUI>();
			connectInterfaces(m_MainMenuUI);
			m_MainMenuUI.alternateMenuOrigin = alternateMenuOrigin;
			m_MainMenuUI.menuOrigin = menuOrigin;
			m_MainMenuUI.Setup();
			m_MainMenuUI.visible = visible;

			CreateFaceButtons(menuTools);
			CreateFaceButtons(menuWorkspaces);
			CreateModuleFaceButtons(menuModules);
			m_MainMenuUI.SetupMenuFaces();
		}

		private void Update()
		{
			var rotationInput = -m_MainMenuInput.rotate.rawValue;

			// Flick the face when the button is released by adding this value to the flick rotation
			if (m_MainMenuInput.flickFace.wasJustReleased)
				m_MainMenuUI.targetFaceIndex = m_MainMenuUI.targetFaceIndex - (int) Mathf.Sign(-rotationInput);

			if (Mathf.Approximately(rotationInput, m_LastRotationInput) && Mathf.Approximately(rotationInput, 0f))
			{
				m_RotationInputIdleTime += Time.unscaledDeltaTime;
				m_RotationDragStartValue = 0f;
				m_RotationDragThresholdExceeded = false;
			}
			else
			{
				const float kRotatationDragMoveThreshold = 0.25f;
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

				if (Mathf.Approximately(m_LastRotationInput, 0))
					m_RotationDragStartValue = 0f;
				else if (Mathf.Approximately(m_RotationDragStartValue, 0))
					m_RotationDragStartValue = m_LastRotationInput; // set new drag start value

				if (m_RotationDragThresholdExceeded || (!Mathf.Approximately(m_RotationDragStartValue, 0) && !Mathf.Approximately(rotationInput, 0) && Mathf.Abs((Mathf.Abs(m_RotationDragStartValue) - Mathf.Abs(rotationInput))) > kRotatationDragMoveThreshold))
				{
					const float kFlickDurationThreshold = 0.3f;
					m_RotationDragThresholdExceeded = true;
					// Perform a quick single face rotation if a quick flick of the input axis occurred
					float flickRotation = rotationInput - m_RotationInputStartValue;
					if (Mathf.Abs(flickRotation) >= kFlickDeltaThreshold
						&& (Time.realtimeSinceStartup - m_RotationInputStartTime) < kFlickDurationThreshold)
					{
						m_MainMenuUI.targetFaceIndex = m_MainMenuUI.targetFaceIndex - (int) Mathf.Sign(flickRotation);

						// Don't allow another flick until rotation resets
						m_RotationInputStartTime = 0f;
					}
					else
					{
						const float kRotationSpeed = 250;

						// Otherwise, apply manual rotation to the main menu faces
						m_MainMenuUI.targetRotation += rotationInput * kRotationSpeed * Time.unscaledDeltaTime;
					}
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

		private void CreateFaceButtons(List<Type> types)
		{
			foreach (var type in types)
			{
				var customMenuAttribute = (MainMenuItemAttribute)type.GetCustomAttributes(typeof(MainMenuItemAttribute), false).FirstOrDefault();
				if (customMenuAttribute != null && !customMenuAttribute.shown)
					continue;

				var isTool = typeof(ITool).IsAssignableFrom(type);
				var isWorkspace = typeof(Workspace).IsAssignableFrom(type);

				var buttonData = new MainMenuUI.ButtonData();
				buttonData.name = type.Name;

				if (customMenuAttribute != null)
				{
					buttonData.name = customMenuAttribute.name;
					buttonData.sectionName = customMenuAttribute.sectionName;
					buttonData.description = customMenuAttribute.description;
				}
				else if (isTool)
				{
					buttonData.name = type.Name.Replace("Tool", string.Empty);
				}
				else if (isWorkspace)
				{
					// For workspaces that haven't specified a custom attribute, do some menu categorization automatically
					buttonData.name = type.Name.Replace("Workspace", string.Empty);
					buttonData.sectionName = "Workspaces";
				}

				var selectedType = type; // Local variable for proper closure
				m_MainMenuUI.CreateFaceButton(buttonData, (b) =>
				{
					b.button.onClick.RemoveAllListeners();
					if (isTool)
					{
						b.button.onClick.AddListener(() =>
						{
							if (visible && b.node.HasValue)
							{
								selectTool(b.node.Value, selectedType);
								// Close menu after selecting something for performance reasons
								visible = false;
							}
						});
					}
					else if (isWorkspace)
					{
						b.button.onClick.AddListener(() =>
						{
							if (visible && b.node.HasValue)
							{
								createWorkspace(selectedType);
								// Close menu after selecting something for performance reasons
								visible = false;
							}
						});
					}
				});
			}
		}

		private void CreateModuleFaceButtons(List<IModule> modules)
		{
			foreach (var module in modules)
			{
				var moduleType = module.GetType();
				var buttonData = new MainMenuUI.ButtonData();
				buttonData.name = moduleType.Name;

				var customMenuAttribute = (MainMenuItemAttribute)moduleType.GetCustomAttributes(typeof(MainMenuItemAttribute), false).FirstOrDefault();
				if (customMenuAttribute != null)
				{
					buttonData.name = customMenuAttribute.name;
					buttonData.sectionName = customMenuAttribute.sectionName;
					buttonData.description = customMenuAttribute.description;
				}
				else
					buttonData.name = moduleType.Name.Replace("Module", string.Empty);

				var currentModule = module; // Local variable for proper closure
				m_MainMenuUI.CreateFaceButton(buttonData, (b) =>
				{
					b.button.onClick.RemoveAllListeners();
					b.button.onClick.AddListener(() =>
					{
						m_MainMenuUI.AddSubmenu(buttonData.sectionName, currentModule.moduleMenuPrefab);
					});
				});
			}
		}

	}
}