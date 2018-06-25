#if UNITY_EDITOR
using System;
using UnityEditor.Experimental.EditorVR;
using UnityEngine;
using UnityEngine.InputNew;

[assembly: OptionalDependency("OVRInput", "ENABLE_OVR_INPUT")]

namespace UnityEditor.Experimental.EditorVR.Input
{
    /// <summary>
    /// Sends events to the input system based on native Oculus SDK calls
    /// </summary>
    sealed class OVRTouchInputToEvents : BaseInputToEvents
    {
#if ENABLE_OVR_INPUT
        const uint k_ControllerCount = 2;
        const int k_AxisCount = (int)VRInputDevice.VRControl.Analog9 + 1;

        float[,] m_LastAxisValues = new float[k_ControllerCount, k_AxisCount];
        Vector3[] m_LastPositionValues = new Vector3[k_ControllerCount];
        Quaternion[] m_LastRotationValues = new Quaternion[k_ControllerCount];

        static readonly OVRInput.Button[] k_Buttons;

        static OVRTouchInputToEvents()
        {
            k_Buttons = (OVRInput.Button[])Enum.GetValues(typeof(OVRInput.Button));
        }

        public void Update()
        {
            // Manually update the Touch input
            OVRInput.Update();

            if ((OVRInput.GetActiveController() & OVRInput.Controller.Touch) == 0)
            {
                active = false;
                return;
            }
            active = true;

            for (VRInputDevice.Handedness hand = VRInputDevice.Handedness.Left;
                (int)hand <= (int)VRInputDevice.Handedness.Right;
                hand++)
            {
                OVRInput.Controller controller = hand == VRInputDevice.Handedness.Left
                    ? OVRInput.Controller.LTouch
                    : OVRInput.Controller.RTouch;
                int ovrIndex = controller == OVRInput.Controller.LTouch ? 0 : 1;
                int deviceIndex = hand == VRInputDevice.Handedness.Left ? 3 : 4;

                // TODO change 3 and 4 based on virtual devices defined in InputDeviceManager (using actual hardware available)
                SendButtonEvents(controller, deviceIndex);
                SendAxisEvents(controller, ovrIndex, deviceIndex);
                SendTrackingEvents(controller, ovrIndex, deviceIndex);
            }
        }

        private bool GetAxis(OVRInput.Controller controller, VRInputDevice.VRControl axis, out float value)
        {
            switch (axis)
            {
                case VRInputDevice.VRControl.Trigger1:
                    value = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, controller);
                    return true;
                case VRInputDevice.VRControl.Trigger2:
                    value = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, controller);
                    return true;
                case VRInputDevice.VRControl.LeftStickX:
                    value = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, controller).x;
                    return true;
                case VRInputDevice.VRControl.LeftStickY:
                    value = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, controller).y;
                    return true;
            }

            value = 0f;
            return false;
        }

        private void SendAxisEvents(OVRInput.Controller controller, int ovrIndex, int deviceIndex)
        {
            for (var axis = 0; axis < k_AxisCount; ++axis)
            {
                float value;
                if (GetAxis(controller, (VRInputDevice.VRControl)axis, out value))
                {
                    if (Mathf.Approximately(m_LastAxisValues[ovrIndex, axis], value))
                        continue;

                    var inputEvent = InputSystem.CreateEvent<GenericControlEvent>();
                    inputEvent.deviceType = typeof(VRInputDevice);
                    inputEvent.deviceIndex = deviceIndex;
                    inputEvent.controlIndex = axis;
                    inputEvent.value = value;

                    m_LastAxisValues[ovrIndex, axis] = inputEvent.value;

                    InputSystem.QueueEvent(inputEvent);
                }
            }
        }

        private int GetButtonIndex(OVRInput.Button button)
        {
            switch (button)
            {
                case OVRInput.Button.One:
                    return (int)VRInputDevice.VRControl.Action1;

                case OVRInput.Button.Two:
                    return (int)VRInputDevice.VRControl.Action2;

                case OVRInput.Button.PrimaryThumbstick:
                    return (int)VRInputDevice.VRControl.LeftStickButton;
            }

            // Not all buttons are currently mapped
            return -1;
        }

        private void SendButtonEvents(OVRInput.Controller ovrController, int deviceIndex)
        {
            foreach (OVRInput.Button button in k_Buttons)
            {
                int buttonIndex = GetButtonIndex(button);
                if (buttonIndex >= 0)
                {
                    bool isDown = OVRInput.GetDown(button, ovrController);
                    bool isUp = OVRInput.GetUp(button, ovrController);

                    if (isDown || isUp)
                    {
                        var inputEvent = InputSystem.CreateEvent<GenericControlEvent>();
                        inputEvent.deviceType = typeof(VRInputDevice);
                        inputEvent.deviceIndex = deviceIndex;
                        inputEvent.controlIndex = buttonIndex;
                        inputEvent.value = isDown ? 1.0f : 0.0f;

                        InputSystem.QueueEvent(inputEvent);
                    }
                }
            }
        }

        private void SendTrackingEvents(OVRInput.Controller ovrController, int ovrIndex, int deviceIndex)
        {
            if (!OVRInput.GetControllerPositionTracked(ovrController))
                return;

            var localPosition = OVRInput.GetLocalControllerPosition(ovrController);
            var localRotation = OVRInput.GetLocalControllerRotation(ovrController);

            if (localPosition == m_LastPositionValues[ovrIndex] && localRotation == m_LastRotationValues[ovrIndex])
                return;

            var inputEvent = InputSystem.CreateEvent<VREvent>();
            inputEvent.deviceType = typeof(VRInputDevice);
            inputEvent.deviceIndex = deviceIndex;
            inputEvent.localPosition = localPosition;
            inputEvent.localRotation = localRotation;

            m_LastPositionValues[ovrIndex] = inputEvent.localPosition;
            m_LastRotationValues[ovrIndex] = inputEvent.localRotation;

            InputSystem.QueueEvent(inputEvent);
        }
#endif
    }
}
#endif
