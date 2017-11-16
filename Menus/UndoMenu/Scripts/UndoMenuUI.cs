#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Menus
{
    sealed class UndoMenuUI : MonoBehaviour, IConnectInterfaces
    {
        public Transform alternateMenuOrigin
        {
            get { return m_AlternateMenuOrigin; }
            set
            {
                if (m_AlternateMenuOrigin == value)
                    return;

                m_AlternateMenuOrigin = value;
                transform.SetParent(m_AlternateMenuOrigin);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
            }
        }

        Transform m_AlternateMenuOrigin;

        public bool visible
        {
            get { return m_Visible; }
            set
            {
                if (m_Visible == value)
                    return;

                m_Visible = value;

                gameObject.SetActive(value);
            }
        }

        bool m_Visible;

        public List<ActionMenuData> actions
        {
            get { return m_Actions; }
            set
            {
                if (value != null)
                {
                    m_Actions = value
                        .Where(a => a.sectionName != null && a.sectionName == ActionMenuItemAttribute.DefaultActionSectionName)
                        .OrderBy(a => a.priority)
                        .ToList();
                }
                else if (visible)
                    visible = false;
            }
        }

        List<ActionMenuData> m_Actions;

        public void Setup()
        {
            gameObject.SetActive(false);
        }
    }
}
#endif
