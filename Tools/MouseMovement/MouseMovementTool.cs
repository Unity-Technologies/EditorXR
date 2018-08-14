#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR.Tools
{
    sealed class MouseMovementTool : MonoBehaviour, ITool, ILocomotor, IRayVisibilitySettings, IUsesViewerScale,
        IUsesDeviceType, IGetVRPlayerObjects, IBlockUIInteraction, IRequestFeedback, IProcessInput
    {

        private MouseActionMap m_mouseInput;

        public Transform cameraRig { private get; set; }

        [SerializeField]
        float m_MovementMultiplier = .1f;

        [SerializeField]
        GameObject m_RingPrefab;

        [SerializeField]
        GameObject m_Hole;

        Ring m_Ring;

        static readonly Vector3 ringOffset = new Vector3(0f, -0.09f, 0.18f);

        void Start()
        {
            GameObject instance = ObjectUtils.Instantiate(m_RingPrefab, cameraRig, false);
            instance.transform.localPosition = ringOffset;
            GameObject instanceHole = ObjectUtils.Instantiate(m_Hole, cameraRig, false);
            instance.transform.localPosition = new Vector3(0f, 0f, .4f);
            m_Ring = instance.GetComponent<Ring>();
        }

        void Update()
        {
            
        }

        public void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl)
        {
            m_mouseInput = (MouseActionMap)input;

            var mouse0 = m_mouseInput.mB0;
            //bool mouse0Down = UnityEngine.Input.GetMouseButtonDown(0);
            //bool mouse1 = UnityEngine.Input.GetMouseButton(1);
            //bool mouse1Down = UnityEngine.Input.GetMouseButtonDown(1);

            if (mouse0.isHeld)
            {
                consumeControl(mouse0);

                const string mouseX = "Mouse X";
                const string mouseY = "Mouse Y";

                var k_forward = Vector3.Scale(cameraRig.forward, new Vector3(1f, 0f, 1f)).normalized;
                var k_right = new Vector3(-k_forward.z, 0f, k_forward.x);
                var delta = (m_mouseInput.mX.value * k_right + m_mouseInput.mY.value * k_forward)
                    * m_MovementMultiplier;

                consumeControl(m_mouseInput.mX);
                consumeControl(m_mouseInput.mY);

                cameraRig.position += delta;

                m_Ring.SetEffectWorldDirection(delta.normalized);
            }

            float deltaScroll = UnityEngine.Input.mouseScrollDelta.y;
            cameraRig.position += deltaScroll * Vector3.up * 0.1f;

            if (deltaScroll != 0f)
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
