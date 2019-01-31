#if UNITY_EDITOR
using System;
using UnityEditor.Experimental.EditorVR.Helpers;
using UnityEditor.Experimental.EditorVR.Menus;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Tools
{
    sealed class CreatePrimitiveMenu : MonoBehaviour, IMenu
    {
        const int k_Priority = 1;
        const string k_BottomGradientProperty = "_ColorBottom";
        const string k_TopGradientProperty = "_ColorTop";

        [SerializeField]
        Renderer m_TitleIcon;

        [SerializeField]
        GameObject[] m_HighlightObjects;

        Material m_TitleIconMaterial;

        public Action<PrimitiveType, bool> selectPrimitive;
        public Action close;

        readonly GradientPair k_EmptyGradient = new GradientPair(UnityBrandColorScheme.light, UnityBrandColorScheme.darker);
        public Bounds localBounds { get; private set; }
        public int priority { get { return k_Priority; } }

        public MenuHideFlags menuHideFlags
        {
            get { return gameObject.activeSelf ? 0 : MenuHideFlags.Hidden; }
            set { gameObject.SetActive(value == 0); }
        }

        public GameObject menuContent { get { return gameObject; } }

        void Awake()
        {
            localBounds = ObjectUtils.GetBounds(transform);
            m_TitleIconMaterial = MaterialUtils.GetMaterialClone(m_TitleIcon);
            m_TitleIconMaterial.SetColor(k_TopGradientProperty, k_EmptyGradient.a);
            m_TitleIconMaterial.SetColor(k_BottomGradientProperty, k_EmptyGradient.b);
        }

        public void SelectPrimitive(int type)
        {
            selectPrimitive((PrimitiveType)type, false);

            // the order of the objects in m_HighlightObjects is matched to the values of the PrimitiveType enum elements
            for (var i = 0; i < m_HighlightObjects.Length; ++i)
            {
                var go = m_HighlightObjects[i];
                go.SetActive(i == type);
            }
        }

        public void SelectFreeformCuboid()
        {
            selectPrimitive(PrimitiveType.Cube, true);

            foreach (var go in m_HighlightObjects)
            {
                go.SetActive(false);
            }
        }

        public void Close()
        {
            close();
        }
    }
}
#endif
