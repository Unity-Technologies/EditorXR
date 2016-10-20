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
		private ActionMap m_RadialMenuActionMap;

		[SerializeField]
		private RadialMenuUI m_RadialMenuPrefab;
		
		private RadialMenuUI m_RadialMenuUI;

		public ActionMapInput actionMapInput
		{
			get { return m_RadialMenuInput; }
			set { m_RadialMenuInput = (RadialMenuInput) value; }
		}
		[SerializeField]
		private RadialMenuInput m_RadialMenuInput;

		public List<ActionMenuData> menuActions
		{
			private get { return m_MenuActions; }
			set
			{
				m_MenuActions = value;

				if (m_RadialMenuUI)
					m_RadialMenuUI.actions = value;
			}
		}
		private List<ActionMenuData> m_MenuActions;

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
					m_RadialMenuUI.SelectionOccurred();
					if(itemSelected != null)
						itemSelected(node);
				}
			}
		}
		private bool m_SelectMenuItem;

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

				if (m_RadialMenuUI != null)
					m_RadialMenuUI.alternateMenuOrigin = value;
			}
		}
		private Transform m_AlternateMenuOrigin;

		private void Update()
		{
			if (m_RadialMenuInput == null || !visible)
				return;

			m_RadialMenuUI.buttonInputDirection = m_RadialMenuInput.navigate.vector2;
			m_RadialMenuUI.pressedDown = m_RadialMenuInput.selectItem.wasJustPressed;
			selectMenuItem = m_RadialMenuInput.selectItem.wasJustReleased;
		}

		public void Setup()
		{
			m_RadialMenuUI = instantiateUI(m_RadialMenuPrefab.gameObject).GetComponent<RadialMenuUI>();
			m_RadialMenuUI.alternateMenuOrigin = alternateMenuOrigin;
			m_RadialMenuUI.actions = menuActions;
			
			m_RadialMenuUI.Setup();
		}
	}
}