#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
    sealed class ActionsModule : MonoBehaviour, IConnectInterfaces, ISpatialMenuProvider
    {
        List<ActionMenuData> m_MenuActions = new List<ActionMenuData>();
        readonly List<IAction> m_Actions = new List<IAction>();
        readonly List<IActionsMenu> m_ActionsMenus = new List<IActionsMenu>();
        readonly List<SpatialMenu.SpatialMenuData> m_SpatialMenuData = new List<SpatialMenu.SpatialMenuData>();

        public List<ActionMenuData> menuActions { get { return m_MenuActions; } }
        public List<SpatialMenu.SpatialMenuData> spatialMenuData { get { return m_SpatialMenuData; } }

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
            m_SpatialMenuData.Clear();
            var spatialMenuActions = new List<SpatialMenu.SpatialMenuElementContainer>();
            var spatialMenuData = new SpatialMenu.SpatialMenuData("Actions", "Perform actions on selected object", spatialMenuActions);
            m_SpatialMenuData.Add(spatialMenuData);

            IEnumerable<Type> actionTypes = ObjectUtils.GetImplementationsOfInterface(typeof(IAction));
            foreach (Type actionType in actionTypes)
            {
                // Don't treat vanilla actions or tool actions as first class actions
                if (actionType.IsNested || !typeof(MonoBehaviour).IsAssignableFrom(actionType))
                    continue;

                var action = ObjectUtils.AddComponent(actionType, gameObject) as IAction;
                this.ConnectInterfaces(action);

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

            m_MenuActions.Sort((x, y) => y.priority.CompareTo(x.priority));
        }

        public void AddActionsMenu(IActionsMenu actionsMenu)
        {
            m_ActionsMenus.Add(actionsMenu);
        }

        internal void UpdateAlternateMenuActions()
        {
            foreach (var actionsMenu in m_ActionsMenus)
            {
                actionsMenu.menuActions = m_MenuActions;
            }
        }
    }
}
#endif
