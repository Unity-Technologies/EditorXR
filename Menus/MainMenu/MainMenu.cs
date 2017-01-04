using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputNew;
using UnityEngine.Experimental.EditorVR.Actions;
using UnityEngine.Experimental.EditorVR.Handles;
using UnityEngine.Experimental.EditorVR.Proxies;
using UnityEngine.Experimental.EditorVR.Tools;
using UnityEngine.Experimental.EditorVR.Utilities;
using UnityEngine.Experimental.EditorVR.Workspaces;

namespace UnityEngine.Experimental.EditorVR.Menus
{
	public class MainMenu : MonoBehaviour, IMainMenu, IConnectInterfaces, IInstantiateUI, ICreateWorkspace, ICustomActionMap, IMenuOrigins, IUsesProxyType
	{
		public ActionMap actionMap { get {return m_MainMenuActionMap; } }
		[SerializeField]
		private ActionMap m_MainMenuActionMap;

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
		private bool m_Visible;

		[SerializeField]
		private MainMenuUI m_MainMenuPrefab;

		private MainMenuUI m_MainMenuUI;
		private float m_LastRotationInput;
		readonly Dictionary<Type, MainMenuButton> m_ToolButtons = new Dictionary<Type, MainMenuButton>();

		public InstantiateUIDelegate instantiateUI { private get; set; }
		public List<Type> menuTools { private get; set; }
		public Func<Transform, Type, bool> selectTool { private get; set; }
		public List<Type> menuWorkspaces { private get; set; }
		public CreateWorkspaceDelegate createWorkspace { private get; set; }
		public List<ActionMenuData> menuActions { get; set; }
		public ConnectInterfacesDelegate connectInterfaces { private get; set; }
		public Transform targetRayOrigin { private get; set; }
		public Func<Transform, Type, bool> isToolActive { private get; set; }
		public Type proxyType { private get; set; }

		public GameObject menuContent { get { return m_MainMenuUI.gameObject; } }

		void Start()
		{
			m_MainMenuUI = instantiateUI(m_MainMenuPrefab.gameObject).GetComponent<MainMenuUI>();
			connectInterfaces(m_MainMenuUI);
			m_MainMenuUI.alternateMenuOrigin = alternateMenuOrigin;
			m_MainMenuUI.menuOrigin = menuOrigin;
			m_MainMenuUI.Setup();
			m_MainMenuUI.visible = m_Visible;

			CreateFaceButtons(menuTools);
			CreateFaceButtons(menuWorkspaces);
			m_MainMenuUI.SetupMenuFaces();
			UpdateToolButtons();
		}

		public void ProcessInput(ActionMapInput input, Action<InputControl> consumeControl)
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

		private void OnDestroy()
		{
			U.Object.Destroy(m_MainMenuUI.gameObject);
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
						m_ToolButtons[type] = b;

						b.button.onClick.AddListener(() =>
						{
							if (visible && targetRayOrigin)
							{
								selectTool(targetRayOrigin, selectedType);
								UpdateToolButtons();
							}
						});
					}
					else if (isWorkspace)
					{
						b.button.onClick.AddListener(() =>
						{
							if (visible)
								createWorkspace(selectedType);
						});
					}
				});
			}
		}

		void UpdateToolButtons()
		{
			foreach (var kvp in m_ToolButtons)
			{
				kvp.Value.selected = isToolActive(targetRayOrigin, kvp.Key);
			}
		}
	}
}