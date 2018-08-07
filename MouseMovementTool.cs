#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
//using UnityEditor.Experimental.EditorVR.Core;
//using UnityEditor.Experimental.EditorVR.Proxies;
//using UnityEditor.Experimental.EditorVR.UI;
//using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
//using UnityEngine.InputNew;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR.Tools
{
    sealed class MouseMovementTool : MonoBehaviour, ITool, ILocomotor, IRayVisibilitySettings,
        /*ICustomActionMap, ILinkedObject,*/ IUsesViewerScale,
        IUsesDeviceType, IGetVRPlayerObjects, IBlockUIInteraction, IRequestFeedback
    {

        public Transform cameraRig
        {
            private get {
                return cameraRig;
            }
            set
            {
                throw new NotImplementedException();
            }
        }
        [SerializeField]
        GameObject m_instructions;
        [SerializeField]
        float m_movementMultiplier = .1f;
        [SerializeField]
        GameObject m_ringPrefab;
        [SerializeField]
        Ring m_ring;

        void Start()
        {
            GameObject instance = Instantiate(m_ringPrefab, cameraRig, false);
            instance.transform.localPosition = new Vector3(0f, -0.09f, 0.18f);
            m_ring = instance.GetComponent<Ring>();
        }

        void FixedUpdate()
        {
            bool k_mouse0 = UnityEngine.Input.GetMouseButton(0);
            bool k_mouse0down = UnityEngine.Input.GetMouseButtonDown(0);
            bool k_mouse1 = UnityEngine.Input.GetMouseButton(1);
            bool k_mouse1down = UnityEngine.Input.GetMouseButtonDown(1);

            if (k_mouse0down && k_mouse1down ||
           k_mouse0 && k_mouse1down ||
           k_mouse0down && k_mouse1)
            {
                ToggleInstructions();
            }
            if (k_mouse0)
            {
                const string k_mouseX = "Mouse X";
                const string k_mouseY = "Mouse Y";

                Vector3 k_forward = Vector3.Scale(cameraRig.forward, new Vector3(1f, 0f, 1f)).normalized;
                Vector3 k_right = Vector3.Scale(cameraRig.right, new Vector3(1f, 0f, 1f)).normalized;
                Vector3 delta = (UnityEngine.Input.GetAxis(k_mouseX) * k_right + UnityEngine.Input.GetAxis(k_mouseY) * k_forward) * m_movementMultiplier;
                
                cameraRig.position += delta;

                m_ring.SetEffectWorldDirection(delta.normalized);
            }

            float deltaScroll = UnityEngine.Input.mouseScrollDelta.y;
            cameraRig.position += deltaScroll * Vector3.up * 0.1f;
            if (deltaScroll != 0f)
            {
                m_ring.SetEffectCore();
                if (deltaScroll > 0f)
                {
                    m_ring.SetEffectCoreUp();
                }
                else
                {
                    m_ring.SetEffectCoreDown();
                }
            }
        }

        void ToggleInstructions()
        {
            if (m_instructions)
                m_instructions.SetActive(!m_instructions.activeSelf);
        }

    }
}
#endif
