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

	    public bool Active
	    {
	        get
	        {
	            return SixenseInput.IsBaseConnected(0);
	        }
	    }

	    public Dictionary<Node, Transform> RayOrigins
	    {
	        get
	        {
	            return new Dictionary<Node, Transform>
	            {
                    { Node.Left, m_LeftHandRayOrigin },
                    { Node.Right, m_RightHandRayOrigin }
	            };
	        }
	    }

	    public bool Hidden
	    {
	        set
	        {
	            gameObject.SetActive(!value);
	        }
	    }

	    [SerializeField]
		private GameObject m_HandProxyPrefab;
		[SerializeField]
		public PlayerInput m_PlayerInput;
        [SerializeField]
        private Transform m_RayOrigin;
        
		private Transform m_LeftHand;
		private Transform m_RightHand;
	    private Transform m_LeftHandRayOrigin;
	    private Transform m_RightHandRayOrigin;
        
		void Awake()
		{
			U.AddComponent<SixenseInputToEvents>(gameObject);
		}

		void Start()
		{
			m_LeftHand = U.InstantiateAndSetActive(m_HandProxyPrefab, transform).transform;
			m_RightHand = U.InstantiateAndSetActive(m_HandProxyPrefab, transform).transform;
            m_LeftHandRayOrigin = m_LeftHand.FindChild("RayOrigin");
            m_RightHandRayOrigin = m_RightHand.FindChild("RayOrigin");

			// In standalone play-mode usage, attempt to get the TrackedObjectInput 
			if (TrackedObjectInput == null && m_PlayerInput)
				TrackedObjectInput = m_PlayerInput.GetActions<TrackedObject>();
		}

		void Update()
		{
			m_LeftHand.localPosition = TrackedObjectInput.leftPosition.vector3;
			m_LeftHand.localRotation = TrackedObjectInput.leftRotation.quaternion;

			m_RightHand.localPosition = TrackedObjectInput.rightPosition.vector3;
			m_RightHand.localRotation = TrackedObjectInput.rightRotation.quaternion;

		}
	}
}
