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

		public bool visible
		{
			get { return m_Visible; }
			set
			{
				if (m_Visible != value)
				{
					m_Visible = value;
					if (m_RadialMenuUI)
						m_RadialMenuUI.visible = value;
				}
			}
		}
		bool m_Visible;

		public event Action<Transform> itemWasSelected;

		public Transform rayOrigin { private get; set; }

		public Func<GameObject, GameObject> instantiateUI { get; set; }

		public Transform menuOrigin { get; set; }

		void Start()
		{
			m_RadialMenuUI = instantiateUI(m_RadialMenuPrefab.gameObject).GetComponent<RadialMenuUI>();
			m_RadialMenuUI.alternateMenuOrigin = alternateMenuOrigin;
			m_RadialMenuUI.actions = menuActions;
			m_RadialMenuUI.Setup();
			m_RadialMenuUI.visible = m_Visible;
		}

		void Update()
		{
			if (m_RadialMenuInput == null || !visible)
				return;

			m_RadialMenuUI.buttonInputDirection = m_RadialMenuInput.navigate.vector2;
			m_RadialMenuUI.highlighted = !m_RadialMenuInput.deselectItem.wasJustReleased; // Deselect any highlghted menu items when the thumbstick/trackpad-button is released
		}
	}
}