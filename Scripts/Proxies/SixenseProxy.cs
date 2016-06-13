using UnityEngine;
using System;
using System.Collections.Generic;
using System.Security.AccessControl;
using UnityEngine.InputNew;

namespace UnityEngine.VR.Proxies
{
	public class SixenseProxy : MonoBehaviour
	{
		[SerializeField]
		private GameObject m_HandProxyPrefab;
		
		private SixenseInputToEvents m_SixenseInput;
		private Transform m_LeftHand;
		private Transform m_RightHand;

		void Awake()
		{
			m_SixenseInput = gameObject.AddComponent<SixenseInputToEvents>();
		}

		void Start()
		{
			m_LeftHand = U.InstantiateAndSetActive(m_HandProxyPrefab, transform).transform;
			m_RightHand = U.InstantiateAndSetActive(m_HandProxyPrefab, transform).transform;
		}

		void Update()
		{

		}

	}
}
