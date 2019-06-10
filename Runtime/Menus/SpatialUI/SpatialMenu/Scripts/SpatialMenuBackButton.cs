using System;
using System.Collections;
using Unity.Labs.EditorXR.Interfaces;
using Unity.Labs.ModuleLoader;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR.Menus
{
    class SpatialMenuBackButton : MonoBehaviour, IUsesControlHaptics, IRayEnterHandler, IRayExitHandler
    {
#pragma warning disable 649
        [SerializeField]
        Button m_Button;
#pragma warning restore 649

        Image m_ButtonImage;
        Vector3 m_VisibleLocalScale;
        Coroutine m_AllowInteractionCoroutine;
        bool m_Highlighted;

        /// <summary>
        /// Action performed when this element is initially hovered
        /// </summary>
        public Action OnHoverEnter { get; set; }

        /// <summary>
        /// Action performed when this element is no longer hovered
        /// </summary>
        public Action OnHoverExit { get; set; }

        /// <summary>
        /// Action performed when this element is selected
        /// </summary>
        public Action OnSelected { get; set; }

        public bool allowInteraction
        {
            set
            {
                if (m_ButtonImage.raycastTarget == value)
                    return;

                m_ButtonImage.raycastTarget = value;
                this.RestartCoroutine(ref m_AllowInteractionCoroutine, AnimateInteractionState());
            }
        }

        public bool highlighted
        {
            set
            {
                if (!m_ButtonImage.raycastTarget || m_Highlighted == value)
                    return;

                m_Highlighted = value;

                this.RestartCoroutine(ref m_AllowInteractionCoroutine, AnimateHighlight());
            }
        }

#if !FI_AUTOFILL
        IProvidesControlHaptics IFunctionalitySubscriber<IProvidesControlHaptics>.provider { get; set; }
#endif

        void Awake()
        {
            m_ButtonImage = m_Button.image;
            m_Button.onClick.AddListener(Selected);
            m_VisibleLocalScale = transform.localScale;
            transform.localScale = Vector3.zero;
            m_ButtonImage.raycastTarget = false;
        }

        void OnDestroy()
        {
            m_Button.onClick.RemoveAllListeners();
        }

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

        void Selected()
        {
            if (OnSelected != null)
                OnSelected();
        }

        IEnumerator AnimateInteractionState()
        {
            var allowInteraction = m_ButtonImage.raycastTarget;
            var currentLocalScale = transform.localScale;
            var targetLocalScale = allowInteraction ? m_VisibleLocalScale : Vector3.zero;
            var transitionAmount = 0f;
            var transitionSpeedMultiplier = allowInteraction ? 3f : 4f; // faster hide, slower reveal
            while (transitionAmount < 1)
            {
                var smoothTransition = MathUtilsExt.SmoothInOutLerpFloat(transitionAmount);
                transform.localScale = Vector3.Lerp(currentLocalScale, targetLocalScale, smoothTransition);
                transitionAmount += Time.unscaledDeltaTime * transitionSpeedMultiplier;
                yield return null;
            }

            transform.localScale = targetLocalScale;
            m_AllowInteractionCoroutine = null;
        }

        IEnumerator AnimateHighlight()
        {
            const float kLargerSizeScalar = 2.5f;
            var currentBackButtonIconSize = transform.localScale;
            var targetBackButtonIconLocalScale = m_Highlighted ? m_VisibleLocalScale * kLargerSizeScalar : m_VisibleLocalScale; // Larger if highlighted
            var transitionAmount = 0f;
            var transitionSpeedMultiplier = m_Highlighted ? 10f : 5f; // Faster when revealing, slower when hiding
            while (transitionAmount < 1)
            {
                var smoothTransition = MathUtilsExt.SmoothInOutLerpFloat(transitionAmount);
                transform.localScale = Vector3.Lerp(currentBackButtonIconSize, targetBackButtonIconLocalScale, smoothTransition);
                transitionAmount += Time.unscaledDeltaTime * transitionSpeedMultiplier;
                yield return null;
            }

            transform.localScale = targetBackButtonIconLocalScale;
            m_AllowInteractionCoroutine = null;
        }
    }
}
