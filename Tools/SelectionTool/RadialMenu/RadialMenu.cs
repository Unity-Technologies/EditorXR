using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.VR.Actions;

namespace UnityEngine.VR.Menus
{
	public class RadialMenu : MonoBehaviour, IInstantiateUI
	{
		//[SerializeField]
		//private RadialMenuUI m_RadialMenuPrefab;

		private Copy m_CopyAction;
		private Paste m_PasteAction;
		private object m_ObjectSelected;

		[SerializeField]
		private RadialMenuUI m_RadialMenuUI;
		public RadialMenuUI radialMenuUI { get; private set; }

		public Func<IAction, bool> performAction { set { m_RadialMenuUI.performAction = value; } }

		private const string kActionSectionName = "DefaultActions";

		private static List<RadialMenu> sRadialMenus = new List<RadialMenu>();
		private static RadialMenu sActiveRadialMenu;
		private static RadialMenu sRadialMenu
		{
			get { return sActiveRadialMenu; }
			set
			{
				if (sActiveRadialMenu != value) // verify that another radial menu is not currently being displayed
				{
					if (sActiveRadialMenu != null)
						sActiveRadialMenu.radialMenuUI.actions = null; // hide an existing radial menu

					sActiveRadialMenu = value; // set this radial menu as the current radial menu
				}
			}
		}

		private List<IAction> m_RadialMenuActions;
		public List<IAction> actions
		{
			private get { return m_RadialMenuActions; }
			set
			{
				Debug.Log("Setting actions in Radial Menu");

				m_RadialMenuActions = new List<IAction>();

				if (value != null && value.Count > 0)
				{
					foreach (var action in value)
					{
						if (action.sectionName == kActionSectionName) // Verify that the action is in the DefaulActions category
						{
							Debug.Log("Adding action in section : " + action.sectionName  + " - order number : " + action.indexPosition);

							m_RadialMenuActions.Add(action);

							var copyAction = action as Copy;
							if (copyAction != null)
								m_CopyAction = copyAction;

							var pasteAction = action as Paste;
							if (pasteAction != null)
								m_PasteAction = pasteAction;
						}
					}

					m_RadialMenuActions = m_RadialMenuActions.OrderByDescending(x => x.indexPosition).ToList(); // Order DefaultActions by their indexPosition
				}
			}
		}

		private List<IAction> currentlyApplicableActions { get; set; }

		public Func<GameObject, GameObject> instantiateUI
		{
			set
			{
				m_RadialMenuUI.instantiateUI = value;
				m_RadialMenuUI.Setup();
			}
		}

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
			get
			{
				return m_SelectMenuItem;
			}

			set
			{
				if (m_SelectMenuItem != value)
				{
					m_SelectMenuItem = value;

					if (m_SelectMenuItem == false)
					{
						Debug.LogError("<color=green>Select was pressed in the radial menu!</color>");
						// perform menu item action
						// close menu depending on action
						// filter available button's being enabled, based on the action performed(or previous actions performed) - enable the paste button based on copy having alredy been performed, close the menu if delete was performed, etc
						Selection.activeGameObject = null;
						m_RadialMenuUI.actions = null;
					}
				}
			}
		}

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

				if (m_RadialMenuUI != null)
				{
					transform.SetParent(value);
					transform.localPosition = Vector3.zero;
					transform.localRotation = Quaternion.identity;
					transform.localScale = Vector3.one;
					m_RadialMenuUI.alternateMenuOrigin = value;
				}
			}
		}
		private Transform m_AlternateMenuOrigin;

		public Action onRadialMenuShow { get; set; }
		public Action onRadialMenuHide { get; set; }

		/*
		public void Setup(List<IAction> menuActions, Func<IAction, bool> performAction, Transform parentTransform)
		{
			Debug.LogWarning("Setup called in Radial Menu");
			//allActions = menuActions;

			//StartCoroutine(DelayedTestCall());
		}
		*/

		private void Awake()
		{
			Debug.LogError("Setting up RadialMenu");
			radialMenuUI = m_RadialMenuUI;
			sRadialMenus.Add(this); // Add this radial menu to the collection of radial menus, allowing for "pushing" of the radial menu to another hand if the menu is opened on a hand currently displaying the radial menu
			//m_RadialMenuUI = instantiateUI(m_RadialMenuPrefab.gameObject).GetComponent<RadialMenuUI>();

			//m_RadialMenuUI.alternateMenuOrigin = m_AlternateMenuOrigin;
			//CreateToolButtons(menuTools);
		}

		private IEnumerator DelayedTestCall()
		{
			float duration = 0;

			while (duration < 4)
			{
				duration += Time.unscaledDeltaTime;
				yield return null;
			}

			//Show();
		}

		public bool Show()//List<IAction> actions)
		{
			bool isShowing = false;

			Debug.LogError("Show called in RadialMenu");

			//if (actions == null || actions.Count < 1)
			if (Selection.objects.Length == 0)
			{
				Debug.LogError("<color=red>Hide Radial Menu UI here - no objects selected</color>");
				
				foreach (var radialMenu in sRadialMenus)
					radialMenu.Hide();
			}
			else
			{
				if (m_ObjectSelected != Selection.activeGameObject)
					Hide();

				m_ObjectSelected = Selection.activeGameObject;
				onRadialMenuShow(); // Raises the event that notifies the main menu to move its menu activator button

				sRadialMenu = this;
				Debug.LogError("<color=green>Show Radial Menu UI here - objects are selected</color>");

				currentlyApplicableActions = new List<IAction>();
				foreach (var action in actions)
				{
					//if (UnityEngine.Random.Range(0, 2) > 0)
						currentlyApplicableActions.Add(action);

					currentlyApplicableActions.Add(action);
					currentlyApplicableActions.Add(action);
				}

				//TODO delete
				//currentlyApplicableActions = allActions;

				// if list count is zero, hide UI
				// if greather, and the icons are not the same, then hide then show with new icons
				// if the same, dont hide, just stay showing

				m_RadialMenuUI.actions = actions;
				isShowing = true;
			}

			return isShowing;
		}

		public void Hide()
		{
			if (Selection.objects.Length > 0 && m_RadialMenuUI.actions != null)
			{
				// Show the radial menu on the opposite hand if an object is currently selected, and this radial menu is being hidden
				foreach (var radialMenu in sRadialMenus)
				{
					if (radialMenu != this)
						radialMenu.Show();
				}

				m_RadialMenuUI.actions = null;
			}
			else if (Selection.objects.Length == 0)
			{
				onRadialMenuHide(); // Raises the event that notifies the main menu to move its menu activator button back to its original position
				m_RadialMenuUI.actions = null; // Hide the radial menu
				m_ObjectSelected = null;
			}

			//Selection.activeGameObject = null;
			//Show();
		}
	}
}