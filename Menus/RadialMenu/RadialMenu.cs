#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Menus
{
	sealed class RadialMenu : MonoBehaviour, IInstantiateUI, IAlternateMenu, IUsesMenuOrigins, ICustomActionMap
	{
		public ActionMap actionMap { get {return m_RadialMenuActionMap; } }
		[SerializeField]
		ActionMap m_RadialMenuActionMap;

		[SerializeField]
		RadialMenuUI m_RadialMenuPrefab;
		
		RadialMenuUI m_RadialMenuUI;

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

		public Transform menuOrigin { get; set; }

		public GameObject menuContent { get { return m_RadialMenuUI.gameObject; } }

		void Start()
		{
			m_RadialMenuUI = this.InstantiateUI(m_RadialMenuPrefab.gameObject).GetComponent<RadialMenuUI>();
			m_RadialMenuUI.alternateMenuOrigin = alternateMenuOrigin;
			m_RadialMenuUI.actions = menuActions;
			m_RadialMenuUI.Setup();
			m_RadialMenuUI.visible = m_Visible;
		}

		public void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl)
		{
			var radialMenuInput = (RadialMenuInput)input;
			if (radialMenuInput == null || !visible)
				return;
			
			var inputDirection = radialMenuInput.navigate.vector2;
			m_RadialMenuUI.buttonInputDirection = inputDirection;

			if (inputDirection != Vector2.zero)
			{
				// Composite controls need to be consumed separately
				consumeControl(radialMenuInput.navigateX);
				consumeControl(radialMenuInput.navigateY);
			}

			m_RadialMenuUI.pressedDown = radialMenuInput.selectItem.wasJustPressed;
			if (m_RadialMenuUI.pressedDown)
			{
				consumeControl(radialMenuInput.selectItem);
			}

			if (radialMenuInput.selectItem.wasJustReleased)
			{
				m_RadialMenuUI.SelectionOccurred();

				if (itemWasSelected != null)
					itemWasSelected(rayOrigin);

				consumeControl(radialMenuInput.selectItem);
			}
		}
	}
}
#endif
