#if UNITY_EDITOR && UNITY_2017_2_OR_NEWER
using UnityEditor.Experimental.EditorVR.Modules;

namespace UnityEditor.Experimental.EditorVR.Core
{
    partial class EditorVR
    {
        class ActionsModuleConnector : Nested, IInterfaceConnector
        {
            public void ConnectInterface(object target, object userData = null)
            {
                var actionsModule = evr.GetModule<ActionsModule>();
                if (actionsModule)
                {
                    var menuActions = actionsModule.menuActions;

                    var toolActions = target as IActions;
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
                                actionsModule.UpdateAlternateMenuActions();
                            }
                        };
                    }

                    var actionsMenu = target as IActionsMenu;
                    if (actionsMenu != null)
                    {
                        actionsMenu.menuActions = menuActions;
                        actionsModule.AddActionsMenu(actionsMenu);
                    }
                }
            }

            public void DisconnectInterface(object target, object userData = null)
            {
                var toolActions = target as IActions;
                if (toolActions != null)
                {
                    var evrActionsModule = evr.GetModule<ActionsModule>();

                    evrActionsModule.RemoveActions(toolActions.actions);
                    evrActionsModule.UpdateAlternateMenuActions();
                }
            }
        }
    }
}
#endif
