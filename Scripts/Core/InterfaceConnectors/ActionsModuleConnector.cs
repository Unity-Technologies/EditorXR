#if UNITY_EDITOR && UNITY_EDITORVR
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Core
{
	partial class EditorVR
	{
		class ActionsModuleConnector : Nested, IInterfaceConnector
		{
			public void ConnectInterface(object obj, Transform rayOrigin = null)
			{
				var actionsModule = evr.GetModule<ActionsModule>();
				if (actionsModule)
				{
					var menuActions = actionsModule.menuActions;

					var toolActions = obj as IActions;
					if (toolActions != null)
					{
						// Delay connecting actions to allow tool / module to initialize first
						EditorApplication.delayCall += () =>
						{
							var actions = toolActions.actions;
							if (actions != null)
							{
								foreach (var action in actions)
								{
									var actionMenuData = new ActionMenuData()
									{
										name = action.GetType().Name,
										sectionName = ActionMenuItemAttribute.DefaultActionSectionName,
										priority = int.MaxValue,
										action = action,
									};
									menuActions.Add(actionMenuData);
								}
								Menus.UpdateAlternateMenuActions();
							}
						};
					}

					var alternateMenu = obj as IAlternateMenu;
					if (alternateMenu != null)
						alternateMenu.menuActions = menuActions;
				}
			}

			public void DisconnectInterface(object obj)
			{
				var toolActions = obj as IActions;
				if (toolActions != null)
				{
					var evrActionsModule = evr.GetModule<ActionsModule>();

					evrActionsModule.RemoveActions(toolActions.actions);
					Menus.UpdateAlternateMenuActions();
				}
			}
		}
	}
}
#endif
