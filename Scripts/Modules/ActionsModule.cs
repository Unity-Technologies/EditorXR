using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Experimental.EditorVR.Actions;
using UnityEngine.Experimental.EditorVR.Tools;
using UnityEngine.Experimental.EditorVR.Utilities;

namespace UnityEngine.Experimental.EditorVR.Modules
{
	internal class ActionsModule : MonoBehaviour, IConnectInterfaces
	{
		public List<ActionMenuData> menuActions { get { return m_MenuActions; } }
		List<ActionMenuData> m_MenuActions = new List<ActionMenuData>();
		List<IAction> m_Actions;

		public ConnectInterfacesDelegate connectInterfaces { get; set; }

		public void RemoveActions(List<IAction> actions)
		{
			m_MenuActions.Clear();
			m_MenuActions.AddRange(m_MenuActions.Where(a => !actions.Contains(a.action)));
		}

		void Start()
		{
			SpawnActions();
		}

		void SpawnActions()
		{
			IEnumerable<Type> actionTypes = U.Object.GetImplementationsOfInterface(typeof(IAction));
			m_Actions = new List<IAction>();
			foreach (Type actionType in actionTypes)
			{
				// Don't treat vanilla actions or tool actions as first class actions
				if (actionType.IsNested || !typeof(MonoBehaviour).IsAssignableFrom(actionType))
					continue;

				var action = U.Object.AddComponent(actionType, gameObject) as IAction;
				var attribute = (ActionMenuItemAttribute)actionType.GetCustomAttributes(typeof(ActionMenuItemAttribute), false).FirstOrDefault();

				connectInterfaces(action);

				if (attribute != null)
				{
					var actionMenuData = new ActionMenuData()
					{
						name = attribute.name,
						sectionName = attribute.sectionName,
						priority = attribute.priority,
						action = action,
					};

					m_MenuActions.Add(actionMenuData);
				}

				m_Actions.Add(action);
			}

			m_MenuActions.Sort((x, y) => y.priority.CompareTo(x.priority));
		}
	}
}