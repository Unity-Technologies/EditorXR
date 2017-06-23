#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Proxies;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEditor.Experimental.EditorVR.Workspaces;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Menus
{
	sealed class MainMenu : MonoBehaviour, IMainMenu, IConnectInterfaces, IInstantiateUI, ICreateWorkspace,
		ICustomActionMap, IUsesMenuOrigins, IUsesProxyType, IControlHaptics, IUsesNode, IRayToNode, IUsesRayOrigin
	{
		const string k_SettingsMenuSectionName = "Settings";

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
		HapticPulse m_FaceRotationPulse;

		[SerializeField]
		HapticPulse m_ShowPulse;

		[SerializeField]
		HapticPulse m_HidePulse;

		[SerializeField]
		MainMenuUI m_MainMenuPrefab;

		[SerializeField]
		HapticPulse m_ButtonClickPulse;

		[SerializeField]
		HapticPulse m_ButtonHoverPulse;

		MainMenuUI m_MainMenuUI;
		float m_LastRotationInput;
		readonly Dictionary<Type, MainMenuButton> m_ToolButtons = new Dictionary<Type, MainMenuButton>();

		public List<Type> menuTools { private get; set; }
		public List<Type> menuWorkspaces { private get; set; }
		public Dictionary<KeyValuePair<Type, Transform>, ISettingsMenuProvider> settingsMenuProviders { private get; set; }
		public Dictionary<KeyValuePair<Type, Transform>, ISettingsMenuItemProvider> settingsMenuItemProviders { private get; set; }
		public List<ActionMenuData> menuActions { get; set; }
		public Transform targetRayOrigin { private get; set; }
		public Type proxyType { private get; set; }
		public Node? node { get; set; }

		public GameObject menuContent { get { return m_MainMenuUI.gameObject; } }

		public Transform rayOrigin { private get; set; }

		public float hideDistance { get { return m_MainMenuUI.menuHeight; } }

		void Start()
		{
			m_MainMenuUI = this.InstantiateUI(m_MainMenuPrefab.gameObject).GetComponent<MainMenuUI>();
			this.ConnectInterfaces(m_MainMenuUI);
			m_MainMenuUI.alternateMenuOrigin = alternateMenuOrigin;
			m_MainMenuUI.menuOrigin = menuOrigin;
			m_MainMenuUI.Setup();
			m_MainMenuUI.visible = m_Visible;
			m_MainMenuUI.buttonHovered += OnButtonHovered;
			m_MainMenuUI.buttonClicked += OnButtonClicked;

			var types = new HashSet<Type>();
			types.UnionWith(menuTools);
			types.UnionWith(menuWorkspaces);
			types.UnionWith(settingsMenuProviders.Keys.Select(provider => provider.Key));
			types.UnionWith(settingsMenuItemProviders.Keys.Select(provider => provider.Key));

			CreateFaceButtons(types.ToList());
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
				this.Pulse(node, m_FaceRotationPulse);
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
				var isTool = typeof(ITool).IsAssignableFrom(type) && menuTools.Contains(type);
				var isWorkspace = typeof(Workspace).IsAssignableFrom(type);
				var isSettingsProvider = typeof(ISettingsMenuProvider).IsAssignableFrom(type);
				var isSettingsItemProvider = typeof(ISettingsMenuItemProvider).IsAssignableFrom(type);

				ITooltip tooltip = null;
				MainMenuUI.ButtonData buttonData = null;

				var selectedType = type; // Local variable for closure
				if (customMenuAttribute != null && customMenuAttribute.shown)
				{
					tooltip = customMenuAttribute.tooltip;

					buttonData = new MainMenuUI.ButtonData(customMenuAttribute.name)
					{
						sectionName = customMenuAttribute.sectionName,
						description = customMenuAttribute.description
					};
				}

				if (isTool)
				{
					if (buttonData == null)
						buttonData = new MainMenuUI.ButtonData(type.Name);

					CreateFaceButton(buttonData, tooltip, () =>
					{
						if (targetRayOrigin)
						{
							this.SelectTool(targetRayOrigin, selectedType);
							UpdateToolButtons();
						}
					});
				}

				if (isWorkspace)
				{
					// For workspaces that haven't specified a custom attribute, do some menu categorization automatically
					if (buttonData == null)
						buttonData = new MainMenuUI.ButtonData(type.Name) { sectionName = "Workspaces" };

					CreateFaceButton(buttonData, tooltip, () =>
					{
						this.CreateWorkspace(selectedType);
					});
				}

				if (isSettingsProvider)
				{
					foreach (var providerPair in settingsMenuProviders)
					{
						var kvp = providerPair.Key;
						if (kvp.Key == type && (kvp.Value == null || kvp.Value == rayOrigin))
						{
							var menuProvider = providerPair.Value;
							if (buttonData == null)
								buttonData = new MainMenuUI.ButtonData(type.Name);

							buttonData.sectionName = k_SettingsMenuSectionName;

							CreateFaceButton(buttonData, tooltip, () =>
							{
								menuProvider.settingsMenuInstance = m_MainMenuUI.AddSubmenu(k_SettingsMenuSectionName, menuProvider.settingsMenuPrefab);
							});
						}
					}
				}

				if (isSettingsItemProvider)
				{
					foreach (var providerPair in settingsMenuItemProviders)
					{
						var kvp = providerPair.Key;
						if (kvp.Key == type && (kvp.Value == null || kvp.Value == rayOrigin))
						{
							var itemProvider = providerPair.Value;
							if (buttonData == null)
								buttonData = new MainMenuUI.ButtonData(type.Name);

							buttonData.sectionName = "Settings";

							itemProvider.settingsMenuItemInstance = m_MainMenuUI.CreateCustomButton(itemProvider.settingsMenuItemPrefab, buttonData);
						}
					}
				}
			}
		}

		void CreateFaceButton(MainMenuUI.ButtonData buttonData, ITooltip tooltip, Action buttonClickCallback)
		{
			var mainMenuButton = m_MainMenuUI.CreateFaceButton(buttonData);
			mainMenuButton.button.onClick.RemoveAllListeners();
			mainMenuButton.button.onClick.AddListener(() =>
			{
				if (visible)
					buttonClickCallback();
			});

			mainMenuButton.tooltip = tooltip;
		}

		void UpdateToolButtons()
		{
			foreach (var kvp in m_ToolButtons)
			{
				kvp.Value.selected = this.IsToolActive(targetRayOrigin, kvp.Key);
			}
		}

		void OnButtonClicked(Transform rayOrigin)
		{
			this.Pulse(this.RequestNodeFromRayOrigin(rayOrigin), m_ButtonClickPulse);
		}

		void OnButtonHovered(Transform rayOrigin)
		{
			this.Pulse(this.RequestNodeFromRayOrigin(rayOrigin), m_ButtonHoverPulse);
		}

		public void SendVisibilityPulse()
		{
			this.Pulse(node, visible ? m_HidePulse : m_ShowPulse);
		}
	}
}
#endif
