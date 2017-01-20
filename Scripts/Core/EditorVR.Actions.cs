#if UNITY_EDITORVR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.EditorVR.Actions;
using UnityEngine.Experimental.EditorVR.Utilities;

namespace UnityEditor.Experimental.EditorVR
{
	partial class EditorVR : MonoBehaviour
	{
		List<ActionMenuData> m_MenuActions = new List<ActionMenuData>();
		List<IAction> m_Actions;

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

				ConnectInterfaces(action);

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

		void UpdateAlternateMenuActions()
		{
			foreach (var deviceData in m_DeviceData)
			{
				var altMenu = deviceData.alternateMenu;
				if (altMenu != null)
					altMenu.menuActions = m_MenuActions;
			}
		}
	}
}
#endif