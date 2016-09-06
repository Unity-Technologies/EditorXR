using System;
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

		[SerializeField]
		private RadialMenuUI m_RadialMenuUI;

		public Func<IAction, bool> performAction { set { m_RadialMenuUI.performAction = value; } }

		public List<IAction> allActions { get; set; }
		private List<IAction> currentlyApplicableActions { get; set; }
		public Func<GameObject, GameObject> instantiateUI { private get; set; }

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
				if (m_RadialMenuUI)
					m_RadialMenuUI.alternateMenuOrigin = value;
			}
		}
		private Transform m_AlternateMenuOrigin;

		/*
		public void Setup(List<IAction> menuActions, Func<IAction, bool> performAction, Transform parentTransform)
		{
			Debug.LogWarning("Setup called in Radial Menu");
			//allActions = menuActions;

			//StartCoroutine(DelayedTestCall());
		}
		*/

		public void Setup()
		{
			//m_RadialMenuUI = instantiateUI(m_RadialMenuPrefab.gameObject).GetComponent<RadialMenuUI>();
			m_RadialMenuUI.instantiateUI = instantiateUI;
			m_RadialMenuUI.alternateMenuOrigin = alternateMenuOrigin;
			m_RadialMenuUI.Setup();

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

		public void Show()//List<IAction> actions)
		{
			Debug.LogError("Show called in RadialMenu");

			//if (actions == null || actions.Count < 1)
			if (Selection.objects.Length == 0)
			{
				Debug.LogError("<color=red>Hide Radial Menu UI here - no objects selected</color>");
				m_RadialMenuUI.actions = null;
			}
			else
			{
				Debug.LogError("<color=green>Show Radial Menu UI here - objects are selected</color>");

				currentlyApplicableActions = new List<IAction>();
				return;
				//TODO support context filtering of actions, only set the currently active and applicable buttons in the UI
				foreach (var action in allActions)
				{
					if (UnityEngine.Random.Range(0, 2) > 0)
						currentlyApplicableActions.Add(action);
				}

				//TODO delete
				currentlyApplicableActions = allActions;

				// if list count is zero, hide UI
				// if greather, and the icons are not the same, then hide then show with new icons
				// if the same, dont hide, just stay showing

				m_RadialMenuUI.actions = currentlyApplicableActions;

				//m_RadialMenuUI.Show(currentlyApplicableActions);
			}
		}
	}
}