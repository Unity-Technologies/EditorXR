using System;
using System.Collections.Generic;
using System.Linq;
using Unity.EditorXR.Core;
using Unity.EditorXR.Interfaces;
using Unity.EditorXR.Utilities;
using Unity.XRTools.ModuleLoader;
using Unity.XRTools.Utils;
using UnityEditor;
using UnityEngine;

namespace Unity.EditorXR.Modules
{
    sealed class ActionsModule : MonoBehaviour, IModule, IUsesConnectInterfaces, ISpatialMenuProvider, IInterfaceConnector, IUsesFunctionalityInjection
    {
        List<ActionMenuData> m_MenuActions = new List<ActionMenuData>();
        readonly List<IAction> m_Actions = new List<IAction>();
        readonly List<IActionsMenu> m_ActionsMenus = new List<IActionsMenu>();
        readonly List<SpatialMenu.SpatialMenuData> m_SpatialMenuData = new List<SpatialMenu.SpatialMenuData>();

        public List<ActionMenuData> menuActions { get { return m_MenuActions; } }

        public List<SpatialMenu.SpatialMenuData> spatialMenuData { get { return m_SpatialMenuData; } }

        public int connectInterfaceOrder { get { return 0; } }

#if !FI_AUTOFILL
        IProvidesFunctionalityInjection IFunctionalitySubscriber<IProvidesFunctionalityInjection>.provider { get; set; }
        IProvidesConnectInterfaces IFunctionalitySubscriber<IProvidesConnectInterfaces>.provider { get; set; }
#endif

        public void RemoveActions(List<IAction> actions)
        {
            for (var i = 0; i < m_MenuActions.Count; i++)
            {
                var menuAction = m_MenuActions[i];
                if (actions.Contains(menuAction.action))
                {
                    m_MenuActions.RemoveAt(i);
                    i--;
                }
            }
        }

        public void LoadModule()
        {
            SpawnActions();
        }

        public void UnloadModule()
        {
            m_MenuActions.Clear();
        }

        void SpawnActions()
        {
            m_SpatialMenuData.Clear();
            var spatialMenuActions = new List<SpatialMenu.SpatialMenuElementContainer>();
            var spatialMenuData = new SpatialMenu.SpatialMenuData("Actions", "Perform actions on selected object", spatialMenuActions);
            m_SpatialMenuData.Add(spatialMenuData);

            m_MenuActions.Clear();
            var actionTypes = CollectionPool<List<Type>, Type>.GetCollection();
            typeof(IAction).GetImplementationsOfInterface(actionTypes);
            foreach (var actionType in actionTypes)
            {
                // Don't treat vanilla actions or tool actions as first class actions
                if (actionType.IsNested || !typeof(MonoBehaviour).IsAssignableFrom(actionType))
                    continue;

                var action = EditorXRUtils.AddComponent(actionType, gameObject) as IAction;
                this.ConnectInterfaces(action);
                this.InjectFunctionalitySingle(action);

                var defaultActionAttribute = (ActionMenuItemAttribute)actionType.GetCustomAttributes(typeof(ActionMenuItemAttribute), false).FirstOrDefault();
                if (defaultActionAttribute != null)
                {
                    var actionMenuData = new ActionMenuData()
                    {
                        name = defaultActionAttribute.name,
                        sectionName = defaultActionAttribute.sectionName,
                        priority = defaultActionAttribute.priority,
                        action = action,
                    };

                    m_MenuActions.Add(actionMenuData);
                }

                var spatialMenuAttribute = (SpatialMenuItemAttribute)actionType.GetCustomAttributes(typeof(SpatialMenuItemAttribute), false).FirstOrDefault();
                if (spatialMenuAttribute != null)
                    spatialMenuActions.Add(new SpatialMenu.SpatialMenuElementContainer(spatialMenuAttribute.name, spatialMenuAttribute.description, (node) => action.ExecuteAction()));

                m_Actions.Add(action);
            }

            CollectionPool<List<Type>, Type>.RecycleCollection(actionTypes);
            m_MenuActions.Sort((x, y) => y.priority.CompareTo(x.priority));
        }

        internal void UpdateAlternateMenuActions()
        {
            foreach (var actionsMenu in m_ActionsMenus)
            {
                actionsMenu.menuActions = m_MenuActions;
            }
        }

        public void ConnectInterface(object target, object userData = null)
        {
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

                        UpdateAlternateMenuActions();
                    }
                };
            }

            var actionsMenu = target as IActionsMenu;
            if (actionsMenu != null)
            {
                actionsMenu.menuActions = menuActions;
                m_ActionsMenus.Add(actionsMenu);
            }
        }

        public void DisconnectInterface(object target, object userData = null)
        {
            var toolActions = target as IActions;
            if (toolActions != null)
            {
                var actions = toolActions.actions;
                if (actions != null)
                {
                    RemoveActions(actions);
                    UpdateAlternateMenuActions();
                }
            }

            var actionsMenu = target as IActionsMenu;
            if (actionsMenu != null)
            {
                m_ActionsMenus.Remove(actionsMenu);
            }
        }
    }
}
