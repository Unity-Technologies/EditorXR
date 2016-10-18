using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.InputNew;
using UnityEngine.VR.Actions;
using UnityEngine.VR.Tools;

namespace UnityEngine.VR.Menus
{
	public class RadialMenu : MonoBehaviour, IInstantiateUI, IAlternateMenu, IMenuOrigins, ICustomActionMap
	{
		public ActionMap actionMap { get {return m_RadialMenuActionMap; } }
		[SerializeField]
		private ActionMap m_RadialMenuActionMap;

		[SerializeField]
		private RadialMenuUI m_RadialMenuPrefab;
		
		private RadialMenuUI m_RadialMenuUI;
		public RadialMenuUI radialMenuUI { get; private set; }

		public ActionMapInput actionMapInput
		{
			get { return m_RadialMenuInput; }
			set { m_RadialMenuInput = (RadialMenuInput) value; }
		}
		[SerializeField]
		private RadialMenuInput m_RadialMenuInput;

		public List<IAction> menuActions
		{
			private get { return m_RadialMenuActions; }
			set
			{
				m_RadialMenuActions = new List<IAction>();

				if (value != null && value.Count > 0)
				{
					foreach (var action in value)
					{
						if (action.sectionName == kActionSectionName) // Verify that the action is in the DefaulActions category
						{
							m_RadialMenuActions.Add(action);
						}
					}

					// Order DefaultActions by their indexPosition
					m_RadialMenuActions = m_RadialMenuActions.OrderByDescending(x => x.indexPosition).ToList();
				}
			}
		}
		private List<IAction> m_RadialMenuActions;

		public Node? node { get; set; }
		public Action setup { get {return Setup; } }

		private const string kActionSectionName = "DefaultActions";

		public Action<Node?> selected { get; set; }

		public bool visible
		{
			get { return m_RadialMenuUI.visible; }
			set
			{
				if (m_RadialMenuUI.visible != value)
				{
					m_RadialMenuUI.visible = value;

					if (value)
						Show();
					else
						Hide();
				}
			}
		}

		private static List<RadialMenu> sRadialMenus = new List<RadialMenu>();
		private Func<IAction, bool> m_performAction;
		public Func<IAction, bool> performAction { get { return m_performAction; } set { m_performAction = value; } }

		public Func<GameObject, GameObject> instantiateUI { get; set; }

		public Vector2 buttonInputDirection { set { m_RadialMenuUI.buttonInputDirection = value; } }

		private bool m_PressedDown;
		public bool pressedDown
		{
			get { return m_PressedDown; }
			set
			{
				if (m_PressedDown != value) // only send changes of the value
				{
					m_PressedDown = value;
					m_RadialMenuUI.pressedDown = value;
				}
			}
		}

		private bool m_SelectMenuItem;
		public bool selectMenuItem
		{
			get { return m_SelectMenuItem; }

			set
			{
				if (m_SelectMenuItem != value)
				{
					m_SelectMenuItem = value;

					if (m_SelectMenuItem == false)
					{
						m_RadialMenuUI.SelectionOccurred();
						if(selected != null)
							selected(node);
					}
				}
			}
		}

		public Transform menuOrigin { get; set; }

		public Transform alternateMenuOrigin
		{
			get
			{
				return m_AlternateMenuOrigin;
			}
			set
			{
				m_AlternateMenuOrigin = value;

				if (radialMenuUI != null)
					m_RadialMenuUI.alternateMenuOrigin = value;
			}
		}
		private Transform m_AlternateMenuOrigin;

		private void Awake()
		{
			radialMenuUI = m_RadialMenuUI;
			sRadialMenus.Add(this); // Add this radial menu to the collection of radial menus, allowing for "pushing" of the radial menu to another hand if the menu is opened on a hand currently displaying the radial menu
		}

		private void Update()
		{
			if (m_RadialMenuInput == null || visible == false)
				return;

			m_RadialMenuUI.buttonInputDirection = m_RadialMenuInput.navigate.vector2;
			pressedDown = m_RadialMenuInput.selectItem.wasJustPressed;
			selectMenuItem = m_RadialMenuInput.selectItem.wasJustReleased;
		}

		public void Setup()
		{
			m_RadialMenuUI = instantiateUI(m_RadialMenuPrefab.gameObject).GetComponent<RadialMenuUI>();
			m_RadialMenuUI.instantiateUI = instantiateUI;
			m_RadialMenuUI.alternateMenuOrigin = alternateMenuOrigin;
			m_RadialMenuUI.performAction = performAction;

			m_RadialMenuUI.Setup();
		}

		private void Show()
		{
			if (Selection.objects.Length == 0)
				Hide();
			else
				m_RadialMenuUI.actions = menuActions;
		}

		public void Hide()
		{
			m_RadialMenuUI.actions = null; // Hide the radial menu
		}
	}
}