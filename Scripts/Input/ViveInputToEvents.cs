using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.InputNew;
using UnityEngine.VR;
using Valve.VR;

public class ViveInputToEvents : MonoBehaviour
{
	private enum XorY { X, Y }
	public int[] steamDevice
	{
		get { return steamDeviceIndices; }
	}
	private readonly int[] steamDeviceIndices = new int[] { -1, -1 };

	public bool active { get; private set; }

	public void Update()
	{
		active = false;

		for (VRInputDevice.Handedness hand = VRInputDevice.Handedness.Left; (int)hand <= (int)VRInputDevice.Handedness.Right; hand++)
		{
			var steamDeviceIndex = steamDeviceIndices[(int)hand];

			if (steamDeviceIndex == -1)
			{
				steamDeviceIndex = SteamVR_Controller.GetDeviceIndex(hand == VRInputDevice.Handedness.Left
					 ? SteamVR_Controller.DeviceRelation.Leftmost
					 : SteamVR_Controller.DeviceRelation.Rightmost);

				if (steamDeviceIndex == -1)
					continue;

				active = true;

				if (hand == VRInputDevice.Handedness.Left)
					steamDeviceIndices[(int)hand] = steamDeviceIndex;
				else if(steamDeviceIndex != steamDeviceIndices[(int)VRInputDevice.Handedness.Left]) // Do not assign device to right hand if it is same device as left hand
					steamDeviceIndices[(int)hand] = steamDeviceIndex;
				else
					continue;
			}

			int deviceIndex = hand == VRInputDevice.Handedness.Left ? 3 : 4; // TODO change 3 and 4 based on virtual devices defined in InputDeviceManager (using actual hardware available)
			SendButtonEvents(steamDeviceIndex, deviceIndex);
			SendAxisEvents(steamDeviceIndex, deviceIndex);
			SendTrackingEvents(steamDeviceIndex, deviceIndex);
		}
	}

	public const int controllerCount = 10;
	public const int buttonCount = 64;
	public const int axisCount = 10; // 5 axes in openVR, each with X and Y.
	private float[,] m_LastAxisValues = new float[controllerCount, axisCount];
	private Vector3[] m_LastPositionValues = new Vector3[controllerCount];
	private Quaternion[] m_LastRotationValues = new Quaternion[controllerCount];

	private void SendAxisEvents(int steamDeviceIndex, int deviceIndex) {
		int a = 0;
		for (int axis = (int)EVRButtonId.k_EButton_Axis0; axis <= (int)EVRButtonId.k_EButton_Axis4; ++axis) {
			Vector2 axisVec = SteamVR_Controller.Input(steamDeviceIndex).GetAxis((EVRButtonId)axis);
			for (XorY xy = XorY.X; (int)xy <= (int)XorY.Y; xy++, a++) {
				var inputEvent = InputSystem.CreateEvent<GenericControlEvent>();
				inputEvent.deviceType = typeof(VRInputDevice);
				inputEvent.deviceIndex = deviceIndex;
				inputEvent.controlIndex = a;
				inputEvent.value = xy == XorY.X ? axisVec.x : axisVec.y;

				if (Mathf.Approximately(m_LastAxisValues[steamDeviceIndex, a], inputEvent.value)) { //TODO Does continue need to be commented out for some reason?
					//continue;
				}
				m_LastAxisValues[steamDeviceIndex, a] = inputEvent.value;

				InputSystem.QueueEvent(inputEvent);
			}
		}
	}

	private void SendButtonEvents(int steamDeviceIndex, int deviceIndex) {
		foreach (EVRButtonId button in Enum.GetValues(typeof(EVRButtonId))) {
			bool isDown = SteamVR_Controller.Input(steamDeviceIndex).GetPressDown(button);
			bool isUp = SteamVR_Controller.Input(steamDeviceIndex).GetPressUp(button);

			if (isDown || isUp) {
				var inputEvent = InputSystem.CreateEvent<GenericControlEvent>();
				inputEvent.deviceType = typeof(VRInputDevice);
				inputEvent.deviceIndex = deviceIndex; 
				inputEvent.controlIndex = axisCount + (int)button;
				inputEvent.value = isDown ? 1.0f : 0.0f;

				InputSystem.QueueEvent(inputEvent);
			}
		}
	}

	private void SendTrackingEvents(int steamDeviceIndex, int deviceIndex) {
		var inputEvent = InputSystem.CreateEvent<VREvent>();
		inputEvent.deviceType = typeof(VRInputDevice);
		inputEvent.deviceIndex = deviceIndex;
		var pose = new SteamVR_Utils.RigidTransform(SteamVR_Controller.Input(steamDeviceIndex).GetPose().mDeviceToAbsoluteTracking);
		inputEvent.localPosition = pose.pos;
		inputEvent.localRotation = pose.rot;

		if (inputEvent.localPosition == m_LastPositionValues[steamDeviceIndex] &&
			inputEvent.localRotation == m_LastRotationValues[steamDeviceIndex])
			return;

		m_LastPositionValues[steamDeviceIndex] = inputEvent.localPosition;
		m_LastRotationValues[steamDeviceIndex] = inputEvent.localRotation;

		InputSystem.QueueEvent(inputEvent);
	}
}