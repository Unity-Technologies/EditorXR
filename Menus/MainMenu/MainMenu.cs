#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Proxies;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEditor.Experimental.EditorVR.Workspaces;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Menus
{
	sealed class MainMenu : MonoBehaviour, IMainMenu, IConnectInterfaces, IInstantiateUI, ICreateWorkspace, ICustomActionMap, IUsesMenuOrigins, IUsesProxyType
	{
		public ActionMap actionMap { get {return m_MainMenuActionMap; } }
		[SerializeField]
		ActionMap m_MainMenuActionMap;

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
		Transform m_AlternateMenuOrigin;

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
		Transform m_MenuOrigin;

		public bool visible
		{
			get { return m_Visible; }
			set
			{
				if (m_Visible != value)
				{
					m_Visible = value;
					if (m_MainMenuUI)
						m_MainMenuUI.visible = value;
				}
			}
		}
		bool m_Visible;

		[SerializeField]
		MainMenuUI m_MainMenuPrefab;

		MainMenuUI m_MainMenuUI;
		float m_LastRotationInput;
		readonly Dictionary<Type, MainMenuButton> m_ToolButtons = new Dictionary<Type, MainMenuButton>();

		public List<Type> menuTools { private get; set; }
		public List<Type> menuWorkspaces { private get; set; }
		public Dictionary<Type, ISettingsMenuProvider> settingsMenuProviders { private get; set; }
		public List<ActionMenuData> menuActions { get; set; }
		public Transform targetRayOrigin { private get; set; }
		public Type proxyType { private get; set; }

		public GameObject menuContent { get { return m_MainMenuUI.gameObject; } }

		void Start()
		{
			m_MainMenuUI = this.InstantiateUI(m_MainMenuPrefab.gameObject).GetComponent<MainMenuUI>();
			this.ConnectInterfaces(m_MainMenuUI);
			m_MainMenuUI.alternateMenuOrigin = alternateMenuOrigin;
			m_MainMenuUI.menuOrigin = menuOrigin;
			m_MainMenuUI.Setup();
			m_MainMenuUI.visible = m_Visible;

			CreateFaceButtons(menuTools);
			CreateFaceButtons(menuWorkspaces);
			CreateFaceButtons(settingsMenuProviders.Keys.ToList());
			m_MainMenuUI.SetupMenuFaces();
			UpdateToolButtons();
		}

		public void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl)
		{
			var mainMenuInput = (MainMenuInput)input;
			var rotationInput = -mainMenuInput.rotate.rawValue;

			consumeControl(mainMenuInput.rotate);
			consumeControl(mainMenuInput.blockY);

			const float kFlickDeltaThreshold = 0.5f;
			if ((proxyType != typeof(ViveProxy) && Mathf.Abs(rotationInput) >= kFlickDeltaThreshold && Mathf.Abs(m_LastRotationInput) < kFlickDeltaThreshold)
				|| mainMenuInput.flickFace.wasJustReleased)
			{
				m_MainMenuUI.targetFaceIndex += (int)Mathf.Sign(rotationInput);

				consumeControl(mainMenuInput.flickFace);
			}

			m_LastRotationInput = rotationInput;
		}

		void OnDestroy()
		{
			if (m_MainMenuUI)
				ObjectUtils.Destroy(m_MainMenuUI.gameObject);
		}

		void CreateFaceButtons(List<Type> types)
		{
			foreach (var type in types)
			{
				var customMenuAttribute = (MainMenuItemAttribute)type.GetCustomAttributes(typeof(MainMenuItemAttribute), false).FirstOrDefault();
				if (customMenuAttribute != null && !customMenuAttribute.shown)
					continue;

				var isTool = typeof(ITool).IsAssignableFrom(type);
				var isWorkspace = typeof(Workspace).IsAssignableFrom(type);
				var isSettingsProvider = typeof(ISettingsMenuProvider).IsAssignableFrom(type);

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
				else if (isSettingsProvider)
				{
					// For workspaces that haven't specified a custom attribute, do some menu categorization automatically
					buttonData.name = type.Name.Replace("Module", string.Empty);
					buttonData.sectionName = "Settings";
				}

				var selectedType = type; // Local variable for proper closure
				m_MainMenuUI.CreateFaceButton(buttonData, b =>
				{
					b.button.onClick.RemoveAllListeners();
					if (isTool)
					{
						m_ToolButtons[selectedType] = b;

						b.button.onClick.AddListener(() =>
						{
							if (visible && targetRayOrigin)
							{
								this.SelectTool(targetRayOrigin, selectedType);
								UpdateToolButtons();
							}
						});
					}
					else if (isWorkspace)
					{
						b.button.onClick.AddListener(() =>
						{
							if (visible)
								this.CreateWorkspace(selectedType);
						});
					}
					else if (isSettingsProvider)
					{
						b.button.onClick.AddListener(() =>
						{
							var provider = settingsMenuProviders[selectedType];
							provider.settingsMenuInstance = m_MainMenuUI.AddSubmenu(buttonData.sectionName, provider.settingsMenuPrefab);
						});
					}

					if (customMenuAttribute != null && customMenuAttribute.tooltip != null)
						b.tooltipText = customMenuAttribute.tooltip.tooltipText;
				});
			}
		}

		void UpdateToolButtons()
		{
			foreach (var kvp in m_ToolButtons)
			{
				kvp.Value.selected = this.IsToolActive(targetRayOrigin, kvp.Key);
			}
		}
	}
}
#endif
