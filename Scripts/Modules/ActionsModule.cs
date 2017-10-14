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
        public List<ActionMenuData> menuActions
        {
            get
            {
                if (Selection.gameObjects.Length > 0)
                {
                    // Show only default actions
                    return m_MenuActions
                        .Where(a => a.sectionName != null && a.sectionName == ActionMenuItemAttribute.DefaultActionSectionName)
                        .OrderBy(a => a.priority)
                        .ToList();
                }
                else
                {
                    return m_MenuActions
                        .Where(a => a.action is Actions.Undo || a.action is Actions.Redo)
                        .OrderBy(a => a.priority)
                        .ToList();
                }
            }
        }

        readonly List<ActionMenuData> m_MenuActions = new List<ActionMenuData>();

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
                    };

                    m_MenuActions.Add(actionMenuData);
                }
            }

            m_MenuActions.Sort((x, y) => y.priority.CompareTo(x.priority));
        }

        public void ConnectActions(IActions target)
        {
            // Delay connecting actions to allow tool / module to initialize first
            EditorApplication.delayCall += () =>
            {
                var actions = target.actions;
                if (actions != null)
                {
                    foreach (var action in actions)
                    {
                        var actionMenuData = new ActionMenuData
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
    }
}
#endif
