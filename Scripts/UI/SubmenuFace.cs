#if UNITY_EDITOR
using System;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Helpers;
using UnityEngine;
using UnityEngine.Events;

namespace UnityEditor.Experimental.EditorVR.Menus
{
    class SubmenuFace : MonoBehaviour, IControlHaptics, IRayToNode
    {
        [SerializeField]
        MainMenuButton m_BackButton;

        [SerializeField]
        HapticPulse m_ButtonClickPulse;

        [SerializeField]
        HapticPulse m_ButtonHoverPulse;

        public GradientPair gradientPair { get; set; }

        public void SetupBackButton(Action<Transform> backAction)
        {
            m_BackButton.hovered += OnButtonHovered;
            m_BackButton.clicked += OnButtonClicked;
            m_BackButton.clicked += backAction;
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
#endif
