using System;
using Unity.Labs.EditorXR.Interfaces;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Menus;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Tools
{
    sealed class CreatePrimitiveMenu : MonoBehaviour, IMenu, IUsesControlHaptics, IRayToNode
    {
        const int k_Priority = 1;
        const string k_BottomGradientProperty = "_ColorBottom";
        const string k_TopGradientProperty = "_ColorTop";

#pragma warning disable 649
        [SerializeField]
        Renderer m_TitleIcon;

        [SerializeField]
        MainMenuButton[] m_Buttons;

        [SerializeField]
        HapticPulse m_ButtonClickPulse;

        [SerializeField]
        HapticPulse m_ButtonHoverPulse;
#pragma warning restore 649

        Material m_TitleIconMaterial;

        public Action<PrimitiveType, bool> selectPrimitive;
        public Action close;

        public Bounds localBounds { get; private set; }
        public int priority { get { return k_Priority; } }

        public MenuHideFlags menuHideFlags
        {
            get { return gameObject.activeSelf ? 0 : MenuHideFlags.Hidden; }
            set { gameObject.SetActive(value == 0); }
        }

        public GameObject menuContent { get { return gameObject; } }

#if !FI_AUTOFILL
        IProvidesControlHaptics IFunctionalitySubscriber<IProvidesControlHaptics>.provider { get; set; }
#endif

        void Awake()
        {
            localBounds = BoundsUtils.GetBounds(transform);
            m_TitleIconMaterial = MaterialUtils.GetMaterialClone(m_TitleIcon);
            m_TitleIconMaterial.SetColor(k_TopGradientProperty, UnityBrandColorScheme.saturatedSessionGradient.a);
            m_TitleIconMaterial.SetColor(k_BottomGradientProperty, UnityBrandColorScheme.saturatedSessionGradient.b);

            foreach (var button in m_Buttons)
            {
                button.hovered += OnButtonHovered;
                button.clicked += OnButtonClicked;
            }
        }

        void OnDestroy()
        {
            foreach (var button in m_Buttons)
            {
                button.hovered -= OnButtonHovered;
                button.clicked -= OnButtonClicked;
            }
        }

        public void SelectPrimitive(int type)
        {
            selectPrimitive((PrimitiveType)type, false);
        }

        public void SelectFreeformCuboid()
        {
            selectPrimitive(PrimitiveType.Cube, true);
        }

        public void Close()
        {
            close();
        }

        void OnButtonClicked(Transform rayOrigin)
        {
            this.Pulse(this.RequestNodeFromRayOrigin(rayOrigin), m_ButtonClickPulse);
        }

        void OnButtonHovered(Transform rayOrigin, Type buttonType, string buttonDescription)
        {
            this.Pulse(this.RequestNodeFromRayOrigin(rayOrigin), m_ButtonHoverPulse);
        }
    }
}
