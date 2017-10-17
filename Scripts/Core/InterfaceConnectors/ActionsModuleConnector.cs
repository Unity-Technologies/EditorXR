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
                    var actions = target as IActions;
                    if (actions != null)
                        actionsModule.ConnectActions(actions, Menus.UpdateAlternateMenuActions);

                    var alternateMenu = target as IUsesMenuActions;
                    if (alternateMenu != null)
                        alternateMenu.menuActions = actionsModule.menuActions;
                }
            }

            public void DisconnectInterface(object target, object userData = null)
            {
                var toolActions = target as IActions;
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
