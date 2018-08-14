#if UNITY_EDITOR
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Tools
{
    sealed class MouseMovementTool : MonoBehaviour, ITool, ILocomotor, IRayVisibilitySettings, IUsesViewerScale,
        IUsesDeviceType, IGetVRPlayerObjects, IBlockUIInteraction, IRequestFeedback, ICustomActionMap
    {
        static readonly Vector3 k_RingOffset = new Vector3(0f, -0.09f, 0.18f);

        [SerializeField]
        float m_MovementMultiplier = .1f;

        [SerializeField]
        GameObject m_RingPrefab;

        [SerializeField]
        GameObject m_Hole;

        [SerializeField]
        ActionMap m_ActionMap;

        Ring m_Ring;

        public ActionMap actionMap { get { return m_ActionMap; } }
        public bool ignoreLocking { get { return false; } }

        public Transform cameraRig { private get; set; }

        void Start()
        {
            var instance = ObjectUtils.Instantiate(m_RingPrefab, cameraRig, false);
            instance.transform.localPosition = k_RingOffset;
            ObjectUtils.Instantiate(m_Hole, cameraRig, false);
            instance.transform.localPosition = new Vector3(0f, 0f, .4f);
            m_Ring = instance.GetComponent<Ring>();
        }

        public void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl)
        {
            var mouseInput = (MouseInput)input;

            var mouse0 = mouseInput.button0;

            //if (mouse0.isHeld)
            {
                consumeControl(mouse0);

                var deltaX = UnityEngine.Input.GetAxis("Mouse X");
                var deltaY = UnityEngine.Input.GetAxis("Mouse Y");

                var forward = Vector3.Scale(cameraRig.forward, new Vector3(1f, 0f, 1f)).normalized;
                var right = new Vector3(-forward.z, 0f, forward.x);
                var delta = (deltaX * right + deltaY * forward)
                    * m_MovementMultiplier;

                consumeControl(mouseInput.positionX);
                consumeControl(mouseInput.positionY);

                cameraRig.position += delta;

                m_Ring.SetEffectWorldDirection(delta.normalized);
            }

            var deltaScroll = UnityEngine.Input.mouseScrollDelta.y;
            cameraRig.position += deltaScroll * Vector3.up * 0.1f;

            if (!Mathf.Approximately(deltaScroll, 0f))
            {
                m_Ring.SetEffectCore();

                if (deltaScroll > 0f)
                {
                    m_Ring.SetEffectCoreUp();
                }

                else
                {
                    m_Ring.SetEffectCoreDown();
                }
            }
        }
    }
}
#endif
