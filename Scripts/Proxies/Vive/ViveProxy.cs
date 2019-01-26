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

        protected override void OnEnable()
        {
#if UNITY_2017_2_OR_NEWER
            m_IsOculus = XRDevice.model.IndexOf("oculus", StringComparison.OrdinalIgnoreCase) >= 0;
#endif

            if (m_IsOculus)
            {
                m_LeftHandProxyPrefab = m_LeftHandTouchProxyPrefab;
                m_RightHandProxyPrefab = m_RightHandTouchProxyPrefab;
            }

            base.OnEnable();
            m_InputToEvents = ObjectUtils.AddComponent<ViveInputToEvents>(gameObject);

            var proxyHelper = m_LeftHand.GetComponent<ViveProxyHelper>();
            if (proxyHelper)
            {
                foreach (var placementOverride in proxyHelper.leftPlacementOverrides)
                {
                    placementOverride.tooltip.placements = placementOverride.placements;
                }
            }
        }

        static void PostAnimate(Affordance[] affordances, AffordanceDefinition[] affordanceDefinitions, Dictionary<Transform, ProxyAnimator.TransformInfo> transformInfos, ActionMapInput input)
        {
            var proxyInput = (ProxyAnimatorInput)input;
            foreach (var affordance in affordances)
            {
                AffordanceAnimationDefinition[] definitions = null;
                foreach (var definition in affordanceDefinitions)
                {
                    if (definition.control == affordance.control)
                    {
                        definitions = definition.animationDefinitions;
                        break;
                    }
                }

                if (definitions == null)
                    continue;

                var transforms = affordance.transforms;
                for (var i = 0; i < transforms.Length; i++)
                {
                    var transform = transforms[i];
                    switch (affordance.control)
                    {
                        case VRInputDevice.VRControl.LeftStickButton:
                            if (!proxyInput.stickButton.isHeld)
                            {
                                var info = transformInfos[transform];
                                info.rotationOffset = Vector3.zero;
                                info.Apply(transform);
                            }

                            break;
                        case VRInputDevice.VRControl.Analog0:
                            // Trackpad touch sphere
                            if (definitions.Length > i && definitions[i].translateAxes != 0)
                                affordance.renderers[i].enabled = !Mathf.Approximately(proxyInput.stickX.value, 0) || !Mathf.Approximately(proxyInput.stickY.value, 0);
                            break;
                    }
                }
            }
        }

        protected override IEnumerator Start()
        {
            yield return base.Start();

            // No oculus proxy uses postAnimate
            if (!m_IsOculus)
            {
                m_LeftHand.GetComponent<ProxyAnimator>().postAnimate += PostAnimate;
                m_RightHand.GetComponent<ProxyAnimator>().postAnimate += PostAnimate;
            }
        }
    }
}
