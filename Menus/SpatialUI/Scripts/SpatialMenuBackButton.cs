#if UNITY_EDITOR
using System;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR
{
    internal class SpatialMenuBackButton : MonoBehaviour, IControlHaptics, IRayEnterHandler, IRayExitHandler
    {
        [SerializeField]
        Button m_Button;

        public Action OnHoverEnter { get; set; }

        public Action OnHoverExit { get; set; }

        public Action OnSelected { get; set; }

        public void OnRayEnter(RayEventData eventData)
        {
            if (OnHoverEnter != null)
                OnHoverEnter();
        }

        public void OnRayExit(RayEventData eventData)
        {
            if (OnHoverExit != null)
                OnHoverExit();
        }

        private void Selected()
        {
            if (OnSelected != null)
                OnSelected();
        }

        void Awake()
        {
            m_Button.onClick.AddListener(Selected);
        }

        void OnDestroy()
        {
            m_Button.onClick.RemoveAllListeners();
        }
    }
}
#endif
