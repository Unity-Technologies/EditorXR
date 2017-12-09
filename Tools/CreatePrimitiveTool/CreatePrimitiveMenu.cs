#if UNITY_EDITOR
using System;
using UnityEditor.Experimental.EditorVR.Menus;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Tools
{
    sealed class CreatePrimitiveMenu : MonoBehaviour, IMenu
    {
        [SerializeField]
        GameObject[] m_HighlightObjects;

        public Action<PrimitiveType, bool> selectPrimitive;
        public Action close;

        public Bounds localBounds { get; private set; }
        public int priority { get { return 1; } }

        public MenuHideFlags menuHideFlags
        {
            get { return gameObject.activeSelf ? 0 : MenuHideFlags.Hidden; }
            set { gameObject.SetActive(value == 0); }
        }

        public GameObject menuContent { get { return gameObject; } }

        void Awake()
        {
            localBounds = ObjectUtils.GetBounds(transform);
        }

        public void SelectPrimitive(int type)
        {
            selectPrimitive((PrimitiveType)type, false);

            // the order of the objects in m_HighlightObjects is matched to the values of the PrimitiveType enum elements
            for (var i = 0; i < m_HighlightObjects.Length; i++)
            {
                var go = m_HighlightObjects[i];
                go.SetActive(i == type);
            }
        }

        public void SelectFreeformCuboid()
        {
            selectPrimitive(PrimitiveType.Cube, true);

            foreach (var go in m_HighlightObjects)
                go.SetActive(false);
        }

        public void Close()
        {
            close();
        }
    }
}
#endif
