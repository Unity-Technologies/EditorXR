#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.InputNew;
using UnityEngine.XR.WSA.Input;

namespace UnityEditor.Experimental.EditorVR.Input
{
    /// <summary>
    /// Sends events to the input system based on native Windows calls
    /// </summary>
    sealed class WindowsMRInputToEvents : BaseInputToEvents
    {
#if UNITY_WSA
        const uint k_ControllerCount = 2;
        const int k_AxisCount = (int)VRInputDevice.VRControl.Analog9 + 1;
 
        float[,] m_LastAxisValues = new float[k_ControllerCount, k_AxisCount];
        Vector3[] m_LastPositionValues = new Vector3[k_ControllerCount];
        Quaternion[] m_LastRotationValues = new Quaternion[k_ControllerCount];
 
        InteractionSourceState[] m_States = new InteractionSourceState[3];
 
        public void Awake()
        {

            InteractionManager.GetCurrentReading(m_States);

            InteractionManager.InteractionSourceUpdated += OnUpdated;
            InteractionManager.InteractionSourcePressed += OnSourcePressed;
            InteractionManager.InteractionSourceReleased += OnSourceReleased;
        }

        //Temp debug function to expose data.
        private void LogStates(string function)
        {
            foreach (var state in m_States)
            {
                Debug.Log("ID: [ " + state.source.id + " ] - Type: [" + state.source.kind + "] - Handedness: [" + state.source.handedness + "] - Function: [" + function + "]" );
            }
        }


        public void Update()
        {
            m_States = InteractionManager.GetCurrentReading();
        }


#region ButtonEvents

        public void OnSourcePressed(InteractionSourcePressedEventArgs args)
        {
            if (args.state.source.kind == InteractionSourceKind.Hand)
            {
                int deviceIndex = args.state.source.handedness == InteractionSourceHandedness.Left ? 3 : 4;

                SendButtonEvents(args.pressType, true, deviceIndex);
            }
            //Debug.Log("interaction source pressed");
            //Debug.Log(args);
        }

        private void OnSourceReleased(InteractionSourceReleasedEventArgs args)
        {
            if (args.state.source.kind == InteractionSourceKind.Hand)
            {
                int deviceIndex = args.state.source.handedness == InteractionSourceHandedness.Left ? 3 : 4;

                SendButtonEvents(args.pressType, false, deviceIndex);
            }
        }


        private int GetButtonIndex(InteractionSourcePressType button)
        {
            switch (button)
            {
                case InteractionSourcePressType.Select:
                    return (int)VRInputDevice.VRControl.Back;
                case InteractionSourcePressType.Menu:
                    return (int)VRInputDevice.VRControl.Start;
                case InteractionSourcePressType.Grasp:
                    return (int)VRInputDevice.VRControl.Action1;
                case InteractionSourcePressType.Touchpad:
                    break;
                case InteractionSourcePressType.Thumbstick:
                    return (int)VRInputDevice.VRControl.Action2;
            }

            // Not all buttons are currently mapped
            return -1;
        }

        private void SendButtonEvents(InteractionSourcePressType button, bool pressed, int deviceIndex)
        {
            int buttonIndex = GetButtonIndex(button);
            if (buttonIndex >= 0)
            {
                var inputEvent = InputSystem.CreateEvent<GenericControlEvent>();
                inputEvent.deviceType = typeof(VRInputDevice);
                inputEvent.deviceIndex = deviceIndex;
                inputEvent.controlIndex = buttonIndex;
                inputEvent.value = pressed ? 1.0f : 0.0f;

                InputSystem.QueueEvent(inputEvent);
            }
        }

#endregion


#region Controller Positional Handling

        //Handle Axis Movement
        public void OnUpdated(InteractionSourceUpdatedEventArgs args)
        {
            if (args.state.source.kind == InteractionSourceKind.Hand)
            {
                //TODO Kept this way to align with other SDK's only.  Possibly needs to change
                int mrDeviceIndex = args.state.source.handedness == InteractionSourceHandedness.Left ? 0 : 1;
                int deviceIndex = args.state.source.handedness == InteractionSourceHandedness.Left ? 3 : 4;

                // TODO change 3 and 4 based on virtual devices defined in InputDeviceManager (using actual hardware available)
                SendAxisEvents(args.state, mrDeviceIndex, deviceIndex);
                SendTrackingEvents(args.state, mrDeviceIndex, deviceIndex);
            }

            //Debug.Log("interaction source updated");
            //Debug.Log(args);
        }

        private bool GetAxis(InteractionSourceState state, VRInputDevice.VRControl axis, out float value)
        {
            switch (axis)
            {
                case VRInputDevice.VRControl.Trigger1:
                    value = state.selectPressedAmount;
                    return true;
                case VRInputDevice.VRControl.LeftStickX:
                    value = state.thumbstickPosition.x;
                    return true;
                case VRInputDevice.VRControl.LeftStickY:
                    value = state.thumbstickPosition.y;
                    return true;
            }

            value = 0f;
            return false;
        }

        private void SendAxisEvents(InteractionSourceState state, int mrDeviceIndex, int deviceIndex)
        {
            for (var axis = 0; axis < k_AxisCount; ++axis)
            {
                float value;
                if (GetAxis(state, (VRInputDevice.VRControl)axis, out value))
                {
                    if (Mathf.Approximately(m_LastAxisValues[mrDeviceIndex, axis], value))
                        continue;

                    var inputEvent = InputSystem.CreateEvent<GenericControlEvent>();
                    inputEvent.deviceType = typeof(VRInputDevice);
                    inputEvent.deviceIndex = deviceIndex;
                    inputEvent.controlIndex = axis;
                    inputEvent.value = value;

                    m_LastAxisValues[mrDeviceIndex, axis] = inputEvent.value;

                    InputSystem.QueueEvent(inputEvent);
                }
            }
        }


        private void SendTrackingEvents(InteractionSourceState state, int mrDeviceIndex, int deviceIndex)
        {
            if (state.sourcePose.positionAccuracy == InteractionSourcePositionAccuracy.None)
                return;

            Vector3 localPosition;
            state.sourcePose.TryGetPosition(out localPosition);
            Quaternion localRotation;
            state.sourcePose.TryGetRotation(out localRotation);

            if (localPosition == m_LastPositionValues[mrDeviceIndex] && localRotation == m_LastRotationValues[mrDeviceIndex])
                return;

            var inputEvent = InputSystem.CreateEvent<VREvent>();
            inputEvent.deviceType = typeof(VRInputDevice);
            inputEvent.deviceIndex = deviceIndex;
            inputEvent.localPosition = localPosition;
            inputEvent.localRotation = localRotation;

            m_LastPositionValues[mrDeviceIndex] = inputEvent.localPosition;
            m_LastRotationValues[mrDeviceIndex] = inputEvent.localRotation;

            InputSystem.QueueEvent(inputEvent);
        }
#endregion
#endif
    }
}
#endif