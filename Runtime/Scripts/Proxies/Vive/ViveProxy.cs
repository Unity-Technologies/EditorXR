using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Labs.EditorXR.Core;
using Unity.Labs.EditorXR.Input;
using Unity.Labs.EditorXR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;
using UnityEngine.XR;

namespace Unity.Labs.EditorXR.Proxies
{
    sealed class ViveProxy : TwoHandedProxyBase
    {
#pragma warning disable 649
        [SerializeField]
        GameObject m_LeftHandTouchProxyPrefab;

        [SerializeField]
        GameObject m_RightHandTouchProxyPrefab;
#pragma warning restore 649

        bool m_IsOculus;

        protected override void Awake()
        {
            m_IsOculus = XRDevice.model.IndexOf("oculus", StringComparison.OrdinalIgnoreCase) >= 0;

            if (m_IsOculus)
            {
                m_LeftHandProxyPrefab = m_LeftHandTouchProxyPrefab;
                m_RightHandProxyPrefab = m_RightHandTouchProxyPrefab;
            }

            base.Awake();
            m_InputToEvents = EditorXRUtils.AddComponent<ViveInputToEvents>(gameObject);

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
