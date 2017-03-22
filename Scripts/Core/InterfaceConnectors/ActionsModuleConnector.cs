using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Core
{
	partial class EditorVR
	{
		class ActionsModuleConnector : Nested, IInterfaceConnector
		{
			public void ConnectInterface(object obj, Transform rayOrigin = null)
			{
				var evrActionsModule = evr.m_ActionsModule;
				var evrMenus = evr.GetNestedModule<Menus>();

				var toolActions = obj as IActions;
				if (toolActions != null)
				{
					var actions = toolActions.actions;
					foreach (var action in actions)
					{
						var actionMenuData = new ActionMenuData()
						{
							name = action.GetType().Name,
							sectionName = ActionMenuItemAttribute.DefaultActionSectionName,
							priority = int.MaxValue,
							action = action,
						};
						evrActionsModule.menuActions.Add(actionMenuData);
					}
					evrMenus.UpdateAlternateMenuActions();
				}

				var alternateMenu = obj as IAlternateMenu;
				if (alternateMenu != null)
					alternateMenu.menuActions = evrActionsModule.menuActions;
			}

			public void DisconnectInterface(object obj)
			{
				var evrActionsModule = evr.m_ActionsModule;
				var evrMenus = evr.GetNestedModule<Menus>();

				var toolActions = obj as IActions;
				if (toolActions != null)
				{
					evrActionsModule.RemoveActions(toolActions.actions);
					evrMenus.UpdateAlternateMenuActions();
				}
			}
		}
	}
}
