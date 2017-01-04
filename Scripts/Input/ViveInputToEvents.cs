using UnityEngine;
using UnityEngine.Experimental.EditorVR;
#if ENABLE_STEAMVR_INPUT
using System; 
using UnityEngine.InputNew;
using Valve.VR;
#endif

[assembly: OptionalDependency("Valve.VR.IVRSystem", "ENABLE_STEAMVR_INPUT")]

/// <summary>
/// Sends events to the input system based on native SteamVR SDK calls
/// </summary>
public class ViveInputToEvents : MonoBehaviour
{
#if ENABLE_STEAMVR_INPUT
	enum XorY { X, Y }

	public int[] steamDevice
	{
		get { return steamDeviceIndices; }
	}
	readonly int[] steamDeviceIndices = new int[] { -1, -1 };
#endif

	public bool active { get; private set; }

#if ENABLE_STEAMVR_INPUT
	public void Update()
	{
		active = false;
		TrackedDevicePose_t[] poses = null;
		var compositor = OpenVR.Compositor;
		if (compositor != null)
		{
			var render = SteamVR_Render.instance;
			compositor.GetLastPoses(render.poses, render.gamePoses);
			poses = render.poses;
		}

		var leftSteamDeviceIndex = SteamVR_Controller.GetDeviceIndex(SteamVR_Controller.DeviceRelation.Leftmost);
		var rightSteamDeviceIndex = SteamVR_Controller.GetDeviceIndex(SteamVR_Controller.DeviceRelation.Rightmost);

		if (leftSteamDeviceIndex == -1 || rightSteamDeviceIndex == -1 || leftSteamDeviceIndex == rightSteamDeviceIndex)
			return;

		for (VRInputDevice.Handedness hand = VRInputDevice.Handedness.Left; (int)hand <= (int)VRInputDevice.Handedness.Right; hand++)
		{
			var steamDeviceIndex = steamDeviceIndices[(int)hand];

			if (steamDeviceIndex == -1)
			{
				steamDeviceIndices[(int)hand] = hand == VRInputDevice.Handedness.Left ? leftSteamDeviceIndex : rightSteamDeviceIndex;
				steamDeviceIndex = steamDeviceIndices[(int)hand];
			}

			active = true;

			int deviceIndex = hand == VRInputDevice.Handedness.Left ? 3 : 4; // TODO change 3 and 4 based on virtual devices defined in InputDeviceManager (using actual hardware available)
			SendButtonEvents(steamDeviceIndex, deviceIndex);
			SendAxisEvents(steamDeviceIndex, deviceIndex);
			SendTrackingEvents(steamDeviceIndex, deviceIndex, poses);
		}
	}

	public const int controllerCount = 10;
	public const int buttonCount = (int)EVRButtonId.k_EButton_Max + 1;
	public const int axisCount = 10; // 5 axes in openVR, each with X and Y.
	private float[,] m_LastAxisValues = new float[controllerCount, axisCount + buttonCount];
	private Vector3[] m_LastPositionValues = new Vector3[controllerCount];
	private Quaternion[] m_LastRotationValues = new Quaternion[controllerCount];

	private void SendAxisEvents(int steamDeviceIndex, int deviceIndex)
	{
		int a = 0;
		for (int axis = (int)EVRButtonId.k_EButton_Axis0; axis <= (int)EVRButtonId.k_EButton_Axis4; ++axis)
		{
			Vector2 axisVec = SteamVR_Controller.Input(steamDeviceIndex).GetAxis((EVRButtonId)axis);
			for (XorY xy = XorY.X; (int)xy <= (int)XorY.Y; xy++, a++)
			{
				var value = xy == XorY.X ? axisVec.x : axisVec.y;
				const float kDeadZone = 0.05f;
				if (Mathf.Abs(value) < kDeadZone)
					value = 0f;

				if (Mathf.Approximately(m_LastAxisValues[steamDeviceIndex, a], value))
					continue;

				var inputEvent = InputSystem.CreateEvent<GenericControlEvent>();
				inputEvent.deviceType = typeof(VRInputDevice);
				inputEvent.deviceIndex = deviceIndex;
				inputEvent.controlIndex = a;
				inputEvent.value = value;

				m_LastAxisValues[steamDeviceIndex, a] = inputEvent.value;

				InputSystem.QueueEvent(inputEvent);
			}
		}
	}

	private void SendButtonEvents(int steamDeviceIndex, int deviceIndex)
	{
		foreach (EVRButtonId button in Enum.GetValues(typeof(EVRButtonId)))
		{
			// Don't double count the trigger
			if (button == EVRButtonId.k_EButton_SteamVR_Trigger)
				continue;

			bool isDown = SteamVR_Controller.Input(steamDeviceIndex).GetPressDown(button);
			bool isUp = SteamVR_Controller.Input(steamDeviceIndex).GetPressUp(button);
			var value = isDown ? 1.0f : 0.0f;
			var controlIndex = axisCount + (int)button;

			if (Mathf.Approximately(m_LastAxisValues[steamDeviceIndex, controlIndex], value))
				continue;

			if (isDown || isUp)
			{
				var inputEvent = InputSystem.CreateEvent<GenericControlEvent>();
				inputEvent.deviceType = typeof(VRInputDevice);
				inputEvent.deviceIndex = deviceIndex;
				inputEvent.controlIndex = controlIndex;
				inputEvent.value = value;

				m_LastAxisValues[steamDeviceIndex, controlIndex] = value;

				InputSystem.QueueEvent(inputEvent);
			}
		}
	}

	private void SendTrackingEvents(int steamDeviceIndex, int deviceIndex, TrackedDevicePose_t[] poses)
	{
		var pose = new SteamVR_Utils.RigidTransform(poses[steamDeviceIndex].mDeviceToAbsoluteTracking);
		var localPosition = pose.pos;
		var localRotation = pose.rot;

		if (localPosition == m_LastPositionValues[steamDeviceIndex] && localRotation == m_LastRotationValues[steamDeviceIndex])
			return;

		var inputEvent = InputSystem.CreateEvent<VREvent>();
		inputEvent.deviceType = typeof(VRInputDevice);
		inputEvent.deviceIndex = deviceIndex;
		inputEvent.localPosition = localPosition;
		inputEvent.localRotation = localRotation;

		m_LastPositionValues[steamDeviceIndex] = inputEvent.localPosition;
		m_LastRotationValues[steamDeviceIndex] = inputEvent.localRotation;

		InputSystem.QueueEvent(inputEvent);
	}
#endif
}