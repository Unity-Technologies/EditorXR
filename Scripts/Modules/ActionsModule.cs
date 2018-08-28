#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
    sealed class ActionsModule : MonoBehaviour, IConnectInterfaces
    {
        public List<ActionMenuData> menuActions { get { return m_MenuActions; } }

        List<ActionMenuData> m_MenuActions = new List<ActionMenuData>();
        readonly List<IAction> m_Actions = new List<IAction>();
        readonly List<IActionsMenu> m_ActionsMenus = new List<IActionsMenu>();

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
            IEnumerable<Type> actionTypes = ObjectUtils.GetImplementationsOfInterface(typeof(IAction));
            foreach (Type actionType in actionTypes)
            {
                // Don't treat vanilla actions or tool actions as first class actions
                if (actionType.IsNested || !typeof(MonoBehaviour).IsAssignableFrom(actionType))
                    continue;

                var action = ObjectUtils.AddComponent(actionType, gameObject) as IAction;
                var attribute = (ActionMenuItemAttribute)actionType.GetCustomAttributes(typeof(ActionMenuItemAttribute), false).FirstOrDefault();

                this.ConnectInterfaces(action);

                if (attribute != null)
                {
                    var actionMenuData = new ActionMenuData()
                    {
                        name = attribute.name,
                        sectionName = attribute.sectionName,
                        priority = attribute.priority,
                        action = action,
                        addToSpatialMenu = attribute.addToSpatialMenu
                    };

                    m_MenuActions.Add(actionMenuData);
                }

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
