#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Input;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;
using UnityEngine.XR;

namespace UnityEditor.Experimental.EditorVR.Proxies
{
    sealed class ViveProxy : TwoHandedProxyBase
    {
        [SerializeField]
        GameObject m_LeftHandTouchProxyPrefab;

        [SerializeField]
        GameObject m_RightHandTouchProxyPrefab;

        bool m_IsOculus;

        public override void Awake()
        {
#if UNITY_2017_2_OR_NEWER
            m_IsOculus = XRDevice.model.IndexOf("oculus", StringComparison.OrdinalIgnoreCase) >= 0;
#endif

            if (m_IsOculus)
            {
                m_LeftHandProxyPrefab = m_LeftHandTouchProxyPrefab;
                m_RightHandProxyPrefab = m_RightHandTouchProxyPrefab;
            }

            base.Awake();
            m_InputToEvents = ObjectUtils.AddComponent<ViveInputToEvents>(gameObject);

            var proxyHelper = m_LeftHand.GetComponent<ViveProxyHelper>();
            if (proxyHelper)
            {
                foreach (var placementOverride in proxyHelper.leftPlacementOverrides)
                {
                    placementOverride.tooltip.placements = placementOverride.placements;
                }
            }

#if !ENABLE_STEAMVR_INPUT
            enabled = false;
#endif
        }

        static void PostAnimate(Affordance[] affordances, AffordanceDefinition[] affordanceDefinitions, Dictionary<Transform, ProxyAnimator.TransformInfo> transformInfos, ActionMapInput input)
        {
            var proxyInput = (ProxyAnimatorInput)input;
            foreach (var button in affordances)
            {
                AffordanceAnimationDefinition affordanceAnimationDefinition = null;
                foreach (var definition in affordanceDefinitions)
                {
                    if (definition.control == button.control)
                    {
                        affordanceAnimationDefinition = definition.animationDefinition;
                        break;
                    }
                }

                switch (button.control)
                {
                    case VRInputDevice.VRControl.LeftStickButton:
                        if (!proxyInput.stickButton.isHeld)
                        {
                            var buttonTransform = button.transform;
                            var info = transformInfos[buttonTransform];
                            info.rotationOffset = Vector3.zero;
                            info.Apply(buttonTransform);
                        }
                        break;
                    case VRInputDevice.VRControl.Analog0:
                        // Trackpad touch sphere
                        if (affordanceAnimationDefinition.translateAxes != 0)
                            button.renderer.enabled = !Mathf.Approximately(proxyInput.stickX.value, 0) || !Mathf.Approximately(proxyInput.stickY.value, 0);
                        break;
                }
            }
        }

#if ENABLE_STEAMVR_INPUT
        public override IEnumerator Start()
        {
            yield return base.Start();

            if (!m_IsOculus)
            {
                m_LeftHand.GetComponent<ProxyAnimator>().postAnimate += PostAnimate;
                m_RightHand.GetComponent<ProxyAnimator>().postAnimate += PostAnimate;
            }
        }
#endif
    }
}
#endif
