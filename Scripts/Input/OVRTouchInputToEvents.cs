using System;
using UnityEngine;
using UnityEngine.InputNew;

public class OVRTouchInputToEvents : MonoBehaviour
{
	public const uint kControllerCount = 2;
	public const int kAxisCount = (int) VRInputDevice.VRControl.Analog9 + 1;
	public const int kDeviceOffset = 3; // magic number for device location in InputDeviceManager.cs

	private float[,] m_LastAxisValues = new float[kControllerCount, kAxisCount];
	private Vector3[] m_LastPositionValues = new Vector3[kControllerCount];
	private Quaternion[] m_LastRotationValues = new Quaternion[kControllerCount];

	public bool active { get; private set; }

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
			(int) hand <= (int) VRInputDevice.Handedness.Right;
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

	private float GetAxis(OVRInput.Controller controller, VRInputDevice.VRControl axis)
	{
		switch (axis)
		{
			case VRInputDevice.VRControl.Trigger1:
				return OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, controller);
			case VRInputDevice.VRControl.LeftStickX:
				return OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, controller).x;
			case VRInputDevice.VRControl.LeftStickY:
				return OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, controller).y;
		}

		return 0f;
	}

	private void SendAxisEvents(OVRInput.Controller controller, int ovrIndex, int deviceIndex)
	{
		for (var axis = 0; axis < kAxisCount; ++axis)
		{
			var inputEvent = InputSystem.CreateEvent<GenericControlEvent>();
			inputEvent.deviceType = typeof(VRInputDevice);
			inputEvent.deviceIndex = deviceIndex;
			inputEvent.controlIndex = axis;
			inputEvent.value = GetAxis(controller, (VRInputDevice.VRControl) axis);

			if (Mathf.Approximately(m_LastAxisValues[ovrIndex, axis], inputEvent.value))
				continue;

			m_LastAxisValues[ovrIndex, axis] = inputEvent.value;

			InputSystem.QueueEvent(inputEvent);
		}
	}

	private int GetButtonIndex(OVRInput.Button button)
	{
		switch (button)
		{
			case OVRInput.Button.One:
				return (int) VRInputDevice.VRControl.Action1;

			case OVRInput.Button.Two:
				return (int) VRInputDevice.VRControl.Action2;

			case OVRInput.Button.PrimaryIndexTrigger:
				return (int) VRInputDevice.VRControl.Trigger1;

			case OVRInput.Button.PrimaryHandTrigger:
				return (int) VRInputDevice.VRControl.Trigger2;

			case OVRInput.Button.PrimaryThumbstick:
				return (int) VRInputDevice.VRControl.LeftStickButton;
		}

		// Not all buttons are currently mapped
		return -1;
	}

	private void SendButtonEvents(OVRInput.Controller ovrController, int deviceIndex)
	{
		foreach (OVRInput.Button button in Enum.GetValues(typeof(OVRInput.Button)))
		{
			bool isDown = OVRInput.GetDown(button, ovrController);
			bool isUp = OVRInput.GetUp(button, ovrController);

			if (isDown || isUp)
			{
				int buttonIndex = GetButtonIndex(button);
				if (buttonIndex >= 0)
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

		var inputEvent = InputSystem.CreateEvent<VREvent>();
		inputEvent.deviceType = typeof(VRInputDevice);
		inputEvent.deviceIndex = deviceIndex;
		inputEvent.localPosition = OVRInput.GetLocalControllerPosition(ovrController);
		inputEvent.localRotation = OVRInput.GetLocalControllerRotation(ovrController);

		if (inputEvent.localPosition == m_LastPositionValues[ovrIndex] &&
			inputEvent.localRotation == m_LastRotationValues[ovrIndex])
			return;

		m_LastPositionValues[ovrIndex] = inputEvent.localPosition;
		m_LastRotationValues[ovrIndex] = inputEvent.localRotation;

		InputSystem.QueueEvent(inputEvent);
	}
}