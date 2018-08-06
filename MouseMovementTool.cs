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
        /*ICustomActionMap,*/ ILinkedObject, IUsesViewerScale,
        IUsesDeviceType, IGetVRPlayerObjects, IBlockUIInteraction, IRequestFeedback
    {
        public List<ILinkedObject> linkedObjects
        {
            set
            {
                throw new NotImplementedException();
            }
        }

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

        //public ActionMap actionMap
        //{
        //    get
        //    {
        //        throw new NotImplementedException();
        //    }
        //}

        public bool ignoreLocking
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        //public void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl)
        //{
        //    throw new NotImplementedException();
        //}

        public GameObject instructions;
        public float movementMultiplier = .1f;
        public GameObject ringPrefab;
        public Ring ring;

        private void ToggleInstructions()
        {
            if(instructions)
                instructions.SetActive(!instructions.activeSelf);
        }

        private void Start()
        {
            GameObject instance = Instantiate(ringPrefab, cameraRig, false);
            instance.transform.localPosition = new Vector3(0f, -0.09f, 0.18f);
            ring = instance.GetComponent<Ring>();
        }

        private void FixedUpdate()
        {

            if (UnityEngine.Input.GetMouseButtonDown(0) && UnityEngine.Input.GetMouseButtonDown(1) ||
           UnityEngine.Input.GetMouseButton(0) && UnityEngine.Input.GetMouseButtonDown(1) ||
           UnityEngine.Input.GetMouseButtonDown(0) && UnityEngine.Input.GetMouseButton(1))
            {
                ToggleInstructions();
            }
            if (UnityEngine.Input.GetMouseButton(0))
            {
                Vector3 forward = Vector3.Scale(cameraRig.forward, new Vector3(1f, 0f, 1f)).normalized;
                Vector3 right = Vector3.Scale(cameraRig.right, new Vector3(1f, 0f, 1f)).normalized;
                Vector3 delta = (UnityEngine.Input.GetAxis("Mouse X") * right + UnityEngine.Input.GetAxis("Mouse Y") * forward) * movementMultiplier;
                //Debug.Log(delta.normalized);
                cameraRig.position += delta;

                //Update trackball
                //ball.rotation = Quaternion.AngleAxis(-UnityEngine.Input.GetAxis("Mouse X") * movementMultiplier * 10f, trackball.forward)
                //    * Quaternion.AngleAxis(UnityEngine.Input.GetAxis("Mouse Y") * movementMultiplier * 10f, trackball.right) * ball.rotation;

                //Send info to Ring
                ring.SetEffectWorldDirection(delta.normalized);
            }

            float deltaScroll = UnityEngine.Input.mouseScrollDelta.y;
            cameraRig.position += deltaScroll * Vector3.up * 0.1f;
            if (deltaScroll != 0f)
            {
                //Send info to Ring
                ring.SetEffectCore();
                if (deltaScroll > 0f)
                {
                    ring.SetEffectCoreUp();
                }
                else
                {
                    ring.SetEffectCoreDown();
                }
            }
        }
}
}
#endif
