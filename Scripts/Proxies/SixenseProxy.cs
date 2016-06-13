using UnityEngine;
using System;
using System.Collections.Generic;
using System.Security.AccessControl;
using UnityEngine.InputNew;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.VR;
#endif

namespace UnityEngine.VR.Proxies
{
	#if UNITY_EDITOR
	[InitializeOnLoad]
	#endif
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

#if UNITY_EDITOR
		private static SixenseProxy s_Instance;
		private static readonly Type kType = typeof(SixenseProxy);

		static SixenseProxy()
		{
			EditorVR.onEnable += OnEVREnabled;
			EditorVR.onDisable += OnEVRDisabled;
		}

		private static void OnEVREnabled()
		{
			s_Instance =
				EditorUtility.CreateGameObjectWithHideFlags(kType.Name, EditorVR.kDefaultHideFlags, kType)
					.GetComponent<SixenseProxy>();
			s_Instance.runInEditMode = true;
		}

		private static void OnEVRDisabled()
		{
			U.Destroy(s_Instance.gameObject);
		}
#endif
	}
}
