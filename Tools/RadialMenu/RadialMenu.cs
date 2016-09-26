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
	public class RadialMenu : MonoBehaviour, IInstantiateUI, IAlternateMenu, IUsesActions, IMenuOrigins, ICustomActionMap
	{
		//[SerializeField]
		//private RadialMenuUI m_RadialMenuPrefab;

		private Copy m_CopyAction;
		private Paste m_PasteAction;
		private object m_ObjectSelected;

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

		public List<IAction> menuActions { get; set; }
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

					if (value == true)
						Show();
					else
						Hide();
				}
			}
		}

		private static List<RadialMenu> sRadialMenus = new List<RadialMenu>();
		private Func<IAction, bool> m_performAction;
		public Func<IAction, bool> performAction { get { return m_performAction; } set { m_performAction = value; } }

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
			get; set;
			/*
			set
			{
				// TODO finish removal of iinstantiateUI content, in favor of the pressing of the thumbstick button down for selecting items(performing actions)
				m_RadialMenuUI.instantiateUI = value;
				m_RadialMenuUI.Setup();
			}
			*/
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
			get { return m_SelectMenuItem; }

			set
			{
				if (m_SelectMenuItem != value)
				{
					m_SelectMenuItem = value;

					if (m_SelectMenuItem == false)
					{
						Debug.LogError("<color=green>Select was pressed in the radial menu!</color>");
						m_RadialMenuUI.SelectionOccurred();
						Selection.activeGameObject = null; // TODO remove this, and allow the menu to stay active after an action is performed
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

		public Action onRadialMenuShow { get; set; }
		public Action onRadialMenuHide { get; set; }

		private void Awake()
		{
			Debug.LogError("Setting up RadialMenu");
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

		public void Setup()
		{
			Debug.LogError("Setup was just called in RadialMenu");

			if (m_RadialMenuUI == null) // remove null check.  This should only be called in connect interfaces once after creation
			{
				m_RadialMenuUI = instantiateUI(m_RadialMenuPrefab.gameObject).GetComponent<RadialMenuUI>();
				m_RadialMenuUI.instantiateUI = instantiateUI;
				m_RadialMenuUI.alternateMenuOrigin = alternateMenuOrigin;
				m_RadialMenuUI.performAction = performAction;
			}

			m_RadialMenuUI.Setup();
		}

		private void Show()
		{
			if (Selection.objects.Length == 0)
			{
				Debug.LogError("<color=red>Hide Radial Menu UI here - no objects selected</color>");

				Hide();
			}
			else
			{
				m_ObjectSelected = Selection.activeGameObject;

				if (onRadialMenuShow != null)
					onRadialMenuShow(); // Raises the event that notifies the main menu to move its menu activator button

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
			}
		}

		public void Hide()
		{
			if (onRadialMenuHide != null)
				onRadialMenuHide(); // Raises the event that notifies the main menu to move its menu activator button back to its original position

			m_RadialMenuUI.actions = null; // Hide the radial menu
			m_ObjectSelected = null;
		}
	}
}