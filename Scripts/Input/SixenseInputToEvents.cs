using System;
using UnityEditor.Experimental.EditorVR;
using UnityEngine.Experimental.EditorVR;
using UnityEngine;
using UnityEngine.InputNew;

[assembly: OptionalDependency("SixenseInput", "ENABLE_SIXENSE_INPUT")]

/// <summary>
/// Sends events to the input system based on native Sixense SDK calls
/// </summary>
public class SixenseInputToEvents : MonoBehaviour
{
#if ENABLE_SIXENSE_INPUT
	public const uint kControllerCount = SixenseInput.MAX_CONTROLLERS;
	public const int kAxisCount = (int)VRInputDevice.VRControl.Analog9 + 1;
	public const int kDeviceOffset = 3; // magic number for device location in InputDeviceManager.cs

	const float kHydraUnits = 0.001f; // input is in mm

	readonly float[,] m_LastAxisValues = new float[kControllerCount, kAxisCount];
	readonly Vector3[] m_LastPositionValues = new Vector3[kControllerCount];
	readonly Quaternion[] m_LastRotationValues = new Quaternion[kControllerCount];

	Vector3[] m_ControllerOffsets = new Vector3[SixenseInput.MAX_CONTROLLERS];
	Quaternion m_RotationOffset = Quaternion.identity;
#endif

	public bool active { get; private set; }

#if ENABLE_SIXENSE_INPUT
	private void Awake()
	{
		if (!FindObjectOfType<SixenseInput>())
			gameObject.AddComponent<SixenseInput>();
	}

	private void Update()
	{
		active = false;
		if (!SixenseInput.IsBaseConnected(0))
			return;

		for (var i = 0; i < SixenseInput.MAX_CONTROLLERS; i++)
		{
			if (SixenseInput.Controllers[i] == null || !SixenseInput.Controllers[i].Enabled)
				continue;

			active = true;

			int deviceIndex = kDeviceOffset + (SixenseInput.Controllers[i].Hand == SixenseHands.LEFT ? 0 : 1);
			SendButtonEvents(i, deviceIndex);
			SendAxisEvents(i, deviceIndex);
			SendTrackingEvents(i, deviceIndex);
		}

		// Check for calibrate
		if (SixenseInput.Controllers[0] != null && SixenseInput.Controllers[1] != null)
		{
			if (SixenseInput.Controllers[0].GetButton(SixenseButtons.START) &&
				SixenseInput.Controllers[1].GetButton(SixenseButtons.START))
				CalibrateControllers();
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
			var value = GetAxis(sixenseDeviceIndex, (VRInputDevice.VRControl)axis);

			if (Mathf.Approximately(m_LastAxisValues[sixenseDeviceIndex, axis], value))
				continue;

			var inputEvent = InputSystem.CreateEvent<GenericControlEvent>();
			inputEvent.deviceType = typeof(VRInputDevice);
			inputEvent.deviceIndex = deviceIndex;
			inputEvent.controlIndex = axis;
			inputEvent.value = value;

			m_LastAxisValues[sixenseDeviceIndex, axis] = inputEvent.value;

			InputSystem.QueueEvent(inputEvent);
		}
	}

	private int GetButtonIndex(SixenseButtons button)
	{
		switch (button)
		{
			case SixenseButtons.ONE:
				return (int)VRInputDevice.VRControl.Action1;

			case SixenseButtons.TWO:
				return (int)VRInputDevice.VRControl.Action2;

			case SixenseButtons.THREE:
				return (int)VRInputDevice.VRControl.Action3;

			case SixenseButtons.FOUR:
				return (int)VRInputDevice.VRControl.Action4;

			case SixenseButtons.BUMPER:
				return (int)VRInputDevice.VRControl.Trigger2;

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
		var localPosition = (m_RotationOffset * controller.Position * kHydraUnits) + m_ControllerOffsets[sixenseDeviceIndex];
		var localRotation = m_RotationOffset * controller.Rotation;

		if (localPosition == m_LastPositionValues[sixenseDeviceIndex] && localRotation == m_LastRotationValues[sixenseDeviceIndex])
			return;

		var inputEvent = InputSystem.CreateEvent<VREvent>();
		inputEvent.deviceType = typeof(VRInputDevice);
		inputEvent.deviceIndex = deviceIndex;
		inputEvent.localPosition = localPosition;
		inputEvent.localRotation = localRotation;
		
		m_LastPositionValues[sixenseDeviceIndex] = inputEvent.localPosition;
		m_LastRotationValues[sixenseDeviceIndex] = inputEvent.localRotation;

		InputSystem.QueueEvent(inputEvent);
	}

	void CalibrateControllers()
	{
#if UNITY_EDITORVR
		// Assume controllers are on the side of the HMD and facing forward (aligned with base)
		var  span = (SixenseInput.Controllers[1].Position*kHydraUnits - SixenseInput.Controllers[0].Position*kHydraUnits).magnitude;
		// Distance between controllers
		var headPivot = VRView.viewerCamera.transform;
		var lookDirection = headPivot.forward;
		lookDirection.y = 0f;
		lookDirection = VRView.viewerPivot.InverseTransformDirection(lookDirection.normalized);
		if (lookDirection != Vector3.zero)
			m_RotationOffset = Quaternion.LookRotation(lookDirection);
		m_ControllerOffsets[0] = VRView.viewerPivot.InverseTransformPoint(headPivot.position + (-headPivot.right*span*0.5f)) -
								(m_RotationOffset*SixenseInput.Controllers[0].Position*kHydraUnits);
		m_ControllerOffsets[1] = VRView.viewerPivot.InverseTransformPoint(headPivot.position + (headPivot.right*span*0.5f)) -
								(m_RotationOffset*SixenseInput.Controllers[1].Position*kHydraUnits);
#endif
	}
#endif
	}