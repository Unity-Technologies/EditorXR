 using System;
using System.Collections.Generic;
using UnityEngine.InputNew;
using UnityEngine.VR.Actions;
using UnityEngine.VR.Tools;

namespace UnityEngine.VR.Menus
{
	public class RadialMenu : MonoBehaviour, IInstantiateUI, IAlternateMenu, IMenuOrigins, ICustomActionMap
	{
		public ActionMap actionMap { get {return m_RadialMenuActionMap; } }
		[SerializeField]
		ActionMap m_RadialMenuActionMap;

		[SerializeField]
		RadialMenuUI m_RadialMenuPrefab;
		
		RadialMenuUI m_RadialMenuUI;

		public ActionMapInput actionMapInput
		{
			get { return m_RadialMenuInput; }
			set { m_RadialMenuInput = (RadialMenuInput) value; }
		}
		[SerializeField]
		RadialMenuInput m_RadialMenuInput;

		public List<ActionMenuData> menuActions
		{
			get { return m_MenuActions; }
			set
			{
				m_MenuActions = value;

				if (m_RadialMenuUI)
					m_RadialMenuUI.actions = value;
			}
		}
		List<ActionMenuData> m_MenuActions;

		public Transform alternateMenuOrigin
		{
			get
			{
				return m_AlternateMenuOrigin;
			}
			set
			{
				m_AlternateMenuOrigin = value;

				if (m_RadialMenuUI != null)
					m_RadialMenuUI.alternateMenuOrigin = value;
			}
		}
		Transform m_AlternateMenuOrigin;

		public Node? node { get; set; }
		public Action setup { get {return Setup; } }

		public event Action<Node?> itemSelected = delegate {};

		public bool visible
		{
			get { return m_RadialMenuUI.visible; }
			set { m_RadialMenuUI.visible = value; }
		}

		public Func<GameObject, GameObject> instantiateUI { get; set; }

		public bool selectMenuItem
		{
			get { return m_SelectMenuItem; }

			set
			{
				if (m_SelectMenuItem == value)
					return;

				m_SelectMenuItem = value;

				if (m_SelectMenuItem)
				{
					itemSelected(node);
				}
			}
		}
		bool m_SelectMenuItem;

		public Transform menuOrigin { get; set; }

		void Update()
		{
			if (m_RadialMenuInput == null || !visible)
				return;

			m_RadialMenuUI.buttonInputDirection = m_RadialMenuInput.navigate.vector2;
			m_RadialMenuUI.highlighted = !m_RadialMenuInput.deselectItem.wasJustReleased; // Deselect any highlghted menu items when the thumbstick/trackpad-button is released
		}

		public void Setup()
		{
			m_RadialMenuUI = instantiateUI(m_RadialMenuPrefab.gameObject).GetComponent<RadialMenuUI>();
			m_RadialMenuUI.alternateMenuOrigin = alternateMenuOrigin;
			m_RadialMenuUI.actions = menuActions;
			m_RadialMenuUI.selectItem = () => selectMenuItem = true;

			m_RadialMenuUI.Setup();

			// Default is to show the radial menu
			visible = true;
		}
	}
}