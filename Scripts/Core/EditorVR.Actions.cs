#if !UNITY_EDITORVR
#pragma warning disable 67, 414, 649
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.EditorVR.Actions;
using UnityEngine.Experimental.EditorVR.Tools;
using UnityEngine.Experimental.EditorVR.Utilities;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR
{
	internal partial class EditorVR : MonoBehaviour
	{
		List<ActionMenuData> m_MenuActions = new List<ActionMenuData>();
		List<IAction> m_Actions;

#if UNITY_EDITORVR
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

		Dictionary<Type, List<ActionMap>> CollectToolActionMaps(IEnumerable<Type> toolTypes)
		{
			var toolMaps = new Dictionary<Type, List<ActionMap>>();

			foreach (var t in toolTypes)
			{
				if (!t.IsSubclassOf(typeof(MonoBehaviour)))
					continue;

				var tool = gameObject.AddComponent(t) as ITool;
				List<ActionMap> actionMaps = new List<ActionMap>();

				var customActionMap = tool as ICustomActionMap;
				if (customActionMap != null)
					actionMaps.Add(customActionMap.actionMap);

				var standardActionMap = tool as IStandardActionMap;
				if (standardActionMap != null)
					actionMaps.Add(m_StandardToolActionMap);

				toolMaps.Add(t, actionMaps);

				U.Object.Destroy(tool as MonoBehaviour);
			}
			return toolMaps;
		}

		void UpdateAlternateMenuActions()
		{
			foreach (var deviceData in m_DeviceData.Values)
			{
				var altMenu = deviceData.alternateMenu;
				if (altMenu != null)
					altMenu.menuActions = m_MenuActions;
			}
		}
#endif
	}
}
