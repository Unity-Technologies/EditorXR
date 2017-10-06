#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Input;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;
using UnityEngine.VR;

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
			m_IsOculus = VRDevice.model.IndexOf("oculus", StringComparison.OrdinalIgnoreCase) >= 0;

			if (m_IsOculus)
			{
				m_LeftHandProxyPrefab = m_LeftHandTouchProxyPrefab;
				m_RightHandProxyPrefab = m_RightHandTouchProxyPrefab;
			}
			
			base.Awake();
			m_InputToEvents = ObjectUtils.AddComponent<ViveInputToEvents>(gameObject);

#if !ENABLE_STEAMVR_INPUT
			enabled = false;
#endif
		}

		static void PostAnimate(ProxyHelper.ButtonObject[] buttons, Dictionary<Transform, ProxyAnimator.TransformInfo> transformInfos, ActionMapInput input)
		{
			var proxyInput = (ProxyAnimatorInput)input;

			foreach (var button in buttons)
			{
				switch (button.control)
				{
					case VRInputDevice.VRControl.LeftStickButton:
						if (!proxyInput.stickButton.isHeld)
						{
							var buttonTransform = button.transform;
							var info = transformInfos[buttonTransform];
							info.ResetRotationOffset();
							info.Apply(buttonTransform);
						}
						break;
					case VRInputDevice.VRControl.Analog0:
						// Trackpad touch sphere
						if (button.translateAxes != 0)
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
