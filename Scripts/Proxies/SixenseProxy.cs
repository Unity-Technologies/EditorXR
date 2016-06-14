using UnityEngine;
using System;
using System.Collections.Generic;
using System.Security.AccessControl;
using UnityEngine.InputNew;

namespace UnityEngine.VR.Proxies
{
	public class SixenseProxy : MonoBehaviour, IProxy
	{
		public TrackedObject TrackedObjectInput { private get; set; }

		[SerializeField]
		private GameObject m_HandProxyPrefab;
		[SerializeField]
		public PlayerInput m_PlayerInput;

		private SixenseInputToEvents m_SixenseInput;
		private Transform m_LeftHand;
		private Transform m_RightHand;

		void Awake()
		{
			m_SixenseInput = U.AddComponent<SixenseInputToEvents>(gameObject);
		}

		void Start()
		{
			m_LeftHand = U.InstantiateAndSetActive(m_HandProxyPrefab, transform).transform;
			m_RightHand = U.InstantiateAndSetActive(m_HandProxyPrefab, transform).transform;

			// In standalone play-mode usage, attempt to get the TrackedObjectInput 
			if (TrackedObjectInput == null && m_PlayerInput)
				TrackedObjectInput = m_PlayerInput.GetActions<TrackedObject>();
		}

		void Update()
		{
			m_LeftHand.position = TrackedObjectInput.leftPosition.vector3;
			m_LeftHand.rotation = TrackedObjectInput.leftRotation.quaternion;

			m_RightHand.position = TrackedObjectInput.rightPosition.vector3;
			m_RightHand.rotation = TrackedObjectInput.rightRotation.quaternion;
		}
	}
}
