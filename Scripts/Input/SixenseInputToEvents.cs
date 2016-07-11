using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor.VR;
using UnityEngine;
using UnityEngine.InputNew;

public class SixenseInputToEvents : MonoBehaviour
{
	public bool Active { get; private set; }

	public const uint kControllerCount = SixenseInput.MAX_CONTROLLERS;
	public const int kAxisCount = (int)VRInputDevice.VRControl.Analog9 + 1;
	public const int kDeviceOffset = 3; // magic number for device location in InputDeviceManager.cs

	private const float kHydraUnits = 0.001f; // input is in mm

	private readonly float [,] m_LastAxisValues = new float[kControllerCount, kAxisCount];
	private readonly Vector3[] m_LastPositionValues = new Vector3[kControllerCount];
	private readonly Quaternion[] m_LastRotationValues = new Quaternion[kControllerCount];

	private Vector3[] m_ControllerOffsets = new Vector3[SixenseInput.MAX_CONTROLLERS];

	private Quaternion m_RotationOffset = Quaternion.identity;

	public Vector3[] ControllerOffsets
	{
		get { return m_ControllerOffsets; }
	}

	private void Awake()
	{
		if (!FindObjectOfType<SixenseInput>())
			gameObject.AddComponent<SixenseInput>();
	}

	private void Update()
	{
		Active = false;
		if (!SixenseInput.IsBaseConnected(0))
			return;

		for (var i = 0; i < SixenseInput.MAX_CONTROLLERS; i++)
		{
			if (SixenseInput.Controllers[i] == null || !SixenseInput.Controllers[i].Enabled)
				continue;

			Active = true;

			int deviceIndex = kDeviceOffset + (SixenseInput.Controllers[i].Hand == SixenseHands.LEFT ? 0 : 1);
			SendButtonEvents(i, deviceIndex);
			SendAxisEvents(i, deviceIndex);
			SendTrackingEvents(i, deviceIndex);
		}

		//Check for calibrate
		if (SixenseInput.Controllers[0] != null && SixenseInput.Controllers[1] != null)
		{
			if (SixenseInput.Controllers[0].GetButton(SixenseButtons.START) &&
				SixenseInput.Controllers[1].GetButton(SixenseButtons.START))
			{
				CalibrateControllers();
			}

		}

	}


	private float GetAxis(int deviceIndex, VRInputDevice.VRControl axis)
	{
		var controller = SixenseInput.Controllers[deviceIndex];
		if (controller != null)
		{
			switch (axis)
			{
				case VRInputDevice.VRControl.Trigger1:
					return controller.Trigger;
				case VRInputDevice.VRControl.LeftStickX:
					return controller.JoystickX;
				case VRInputDevice.VRControl.LeftStickY:
					return controller.JoystickY;
			}
		}

		return 0f;
	}

	private void SendAxisEvents(int sixenseDeviceIndex, int deviceIndex)
	{        
		for (var axis = 0; axis < kAxisCount; ++axis)
		{
			var inputEvent = InputSystem.CreateEvent<GenericControlEvent>();
			inputEvent.deviceType = typeof(VRInputDevice);
			inputEvent.deviceIndex = deviceIndex;
			inputEvent.controlIndex = axis;
			inputEvent.value = GetAxis(sixenseDeviceIndex, (VRInputDevice.VRControl)axis);

			if (Mathf.Approximately(m_LastAxisValues[sixenseDeviceIndex, axis], inputEvent.value))
				continue;

			m_LastAxisValues[sixenseDeviceIndex, axis] = inputEvent.value;

			InputSystem.QueueEvent(inputEvent);
		}
	}

	private int GetButtonIndex(SixenseButtons button)
	{
		switch (button)
		{
			case SixenseButtons.ONE:
				return (int) VRInputDevice.VRControl.Action1;

			case SixenseButtons.TWO:
				return (int)VRInputDevice.VRControl.Action2;

			case SixenseButtons.THREE:
				return (int)VRInputDevice.VRControl.Action3;

			case SixenseButtons.FOUR:
				return (int)VRInputDevice.VRControl.Action4;

			case SixenseButtons.BUMPER:
				return (int)VRInputDevice.VRControl.Action5;

			case SixenseButtons.TRIGGER:
				return (int)VRInputDevice.VRControl.Trigger1;

			case SixenseButtons.START:
				return (int)VRInputDevice.VRControl.Start;

			case SixenseButtons.JOYSTICK:
				return (int)VRInputDevice.VRControl.LeftStickButton;
		}

		// Not all buttons are currently mapped
		return -1;
	}

	private void SendButtonEvents(int sixenseDeviceIndex, int deviceIndex)
	{
		var controller = SixenseInput.Controllers[sixenseDeviceIndex];
		foreach (SixenseButtons button in Enum.GetValues(typeof(SixenseButtons)))
		{
			bool isDown = controller.GetButtonDown(button);
			bool isUp = controller.GetButtonUp(button);

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

	private void SendTrackingEvents(int sixenseDeviceIndex, int deviceIndex)
	{
		var controller = SixenseInput.Controllers[sixenseDeviceIndex];
		var inputEvent = InputSystem.CreateEvent<VREvent>();
		inputEvent.deviceType = typeof (VRInputDevice);
		inputEvent.deviceIndex = deviceIndex;
		inputEvent.localPosition = (m_RotationOffset * controller.Position * kHydraUnits) + m_ControllerOffsets[sixenseDeviceIndex];
		inputEvent.localRotation = m_RotationOffset * controller.Rotation;

		if (inputEvent.localPosition == m_LastPositionValues[sixenseDeviceIndex] &&
			inputEvent.localRotation == m_LastRotationValues[sixenseDeviceIndex])
			return;

		m_LastPositionValues[sixenseDeviceIndex] = inputEvent.localPosition;
		m_LastRotationValues[sixenseDeviceIndex] = inputEvent.localRotation;

		InputSystem.QueueEvent(inputEvent);
	}

	void CalibrateControllers()
	{
		//Assume controllers are on the side of the HMD and facing forward (aligned with base)
		float span = (SixenseInput.Controllers[1].Position * kHydraUnits - SixenseInput.Controllers[0].Position * kHydraUnits).magnitude; //Distance between controllers
		Transform headPivot = VRView.viewerCamera.transform;
		Vector3 lookDirection = headPivot.forward;
		lookDirection.y = 0f;
		lookDirection = VRView.viewerPivot.InverseTransformDirection(lookDirection.normalized);
		if (lookDirection != Vector3.zero)
			m_RotationOffset = Quaternion.LookRotation(lookDirection);
		m_ControllerOffsets[0] = VRView.viewerPivot.InverseTransformPoint(headPivot.position + (-headPivot.right * span * 0.5f)) - (m_RotationOffset * SixenseInput.Controllers[0].Position * kHydraUnits);
		m_ControllerOffsets[1] = VRView.viewerPivot.InverseTransformPoint(headPivot.position + (headPivot.right * span * 0.5f)) - (m_RotationOffset * SixenseInput.Controllers[1].Position * kHydraUnits);
	}
}