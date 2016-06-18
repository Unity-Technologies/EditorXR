using UnityEngine;
using System.Collections;
using UnityEditor.VR;
using UnityEngine.InputNew;

namespace UnityEngine.VR.Proxies
{
    public class ViveProxy : MonoBehaviour, IProxy
    {
        public TrackedObject TrackedObjectInput { private get; set; }

        [SerializeField]
        private GameObject m_HandProxyPrefab;
        [SerializeField]
        public PlayerInput m_PlayerInput;

        private ViveInputToEvents m_ViveInput;
        private Transform m_LeftHand;
        private Transform m_RightHand;

        private SteamVR_RenderModel m_RightModel;
        private SteamVR_RenderModel m_LeftModel;
        void Awake()
        {
            m_ViveInput = U.AddComponent<ViveInputToEvents>(gameObject);
        }

        void Start()
        {
            SteamVR_Render.instance.transform.parent = gameObject.transform;
            m_LeftHand = U.InstantiateAndSetActive(m_HandProxyPrefab, transform).transform;
            m_LeftModel = m_LeftHand.GetComponent<SteamVR_RenderModel>(); // TODO: AddComponent at runtime and remove it from the prefab (requires the steam device model loading to work properly in editor)

            m_RightHand = U.InstantiateAndSetActive(m_HandProxyPrefab, transform).transform;
            m_RightModel = m_RightHand.GetComponent<SteamVR_RenderModel>();

            // In standalone play-mode usage, attempt to get the TrackedObjectInput 
            if (TrackedObjectInput == null && m_PlayerInput)
                TrackedObjectInput = m_PlayerInput.GetActions<TrackedObject>();
        }

        void Update()
        {

            //If proxy is not mapped to a physical input device, check if one has been assigned
            if ((int)m_LeftModel.index == -1 && m_ViveInput.SteamDevice[0] != -1)
            {
                // HACK set device index individually instead of calling SetDeviceIndex because loading device mesh dynamically does not work in editor. Prefab has Model Override set and mesh generated, calling SetDeviceIndex clears the model.
                m_LeftModel.index = (SteamVR_TrackedObject.EIndex) m_ViveInput.SteamDevice[0];
            }
            if ((int)m_RightModel.index == -1 && m_ViveInput.SteamDevice[1] != -1)
            {
                m_RightModel.index = (SteamVR_TrackedObject.EIndex) m_ViveInput.SteamDevice[1];
            }

            m_LeftHand.localPosition = TrackedObjectInput.leftPosition.vector3;
            m_LeftHand.localRotation = TrackedObjectInput.leftRotation.quaternion;

            m_RightHand.localPosition = TrackedObjectInput.rightPosition.vector3;
            m_RightHand.localRotation = TrackedObjectInput.rightRotation.quaternion;
        }
    }
}