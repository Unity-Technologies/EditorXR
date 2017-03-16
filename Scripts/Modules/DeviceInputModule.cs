#if UNITY_EDITOR && UNITY_EDITORVR
using System;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Modules
{
	sealed class DeviceInputModule : MonoBehaviour
	{
		public TrackedObject trackedObjectInput { get; private set; }
		[SerializeField]
		ActionMap m_TrackedObjectActionMap;

		[SerializeField]
		ActionMap m_StandardToolActionMap;

		public ActionMap directSelectActionMap { get { return m_DirectSelectActionMap; } }
		[SerializeField]
		ActionMap m_DirectSelectActionMap;

		PlayerHandle m_PlayerHandle;

		readonly HashSet<InputControl> m_LockedControls = new HashSet<InputControl>();

		readonly Dictionary<string, Node> m_TagToNode = new Dictionary<string, Node>
		{
			{ "Left", Node.LeftHand },
			{ "Right", Node.RightHand }
		};

		// Local method use only -- created here to reduce garbage collection
		readonly HashSet<IProcessInput> m_ProcessedInputs = new HashSet<IProcessInput>();
		readonly List<InputDevice> m_SystemDevices = new List<InputDevice>();
		readonly Dictionary<Type, string[]> m_DeviceTypeTags = new Dictionary<Type, string[]>();

		public Action<HashSet<IProcessInput>, ConsumeControlDelegate> processInput;
		public Action<List<ActionMapInput>>  updatePlayerHandleMaps;

		public List<InputDevice> GetSystemDevices()
		{
			// For now let's filter out any other devices other than VR controller devices; Eventually, we may support mouse / keyboard etc.
			m_SystemDevices.Clear();
			var devices = InputSystem.devices;
			for (int i = 0; i < devices.Count; i++)
			{
				var device = devices[i];
				if (device is VRInputDevice && device.tagIndex != -1)
					m_SystemDevices.Add(device);
			}

			return m_SystemDevices;
		}

		public void InitializePlayerHandle()
		{
			m_PlayerHandle = PlayerHandleManager.GetNewPlayerHandle();
			m_PlayerHandle.global = true;
			m_PlayerHandle.processAll = true;
		}

		void OnDestroy()
		{
			PlayerHandleManager.RemovePlayerHandle(m_PlayerHandle);
		}

		public void ProcessInput()
		{
			// Maintain a consumed control, so that other AMIs don't pick up the input, until it's no longer used
			var removeList = new List<InputControl>();
			foreach (var lockedControl in m_LockedControls)
			{
				if (Mathf.Approximately(lockedControl.rawValue, lockedControl.provider.GetControlData(lockedControl.index).defaultValue))
					removeList.Add(lockedControl);
				else
					ConsumeControl(lockedControl);
			}

			// Remove separately, since we cannot remove while iterating
			foreach (var inputControl in removeList)
			{
				m_LockedControls.Remove(inputControl);
			}

			m_ProcessedInputs.Clear();

			// TODO: Replace this with a map of ActionMap,IProcessInput and go through those
			if (processInput != null)
				processInput(m_ProcessedInputs, ConsumeControl);
		}

		public void CreateDefaultActionMapInputs()
		{
			trackedObjectInput = (TrackedObject)CreateActionMapInput(m_TrackedObjectActionMap, null);
		}

		public ActionMapInput CreateActionMapInput(ActionMap map, InputDevice device)
		{
			// Check for improper use of action maps first
			if (device != null && !IsValidActionMapForDevice(map, device))
				return null;

			var devices = device == null ? GetSystemDevices() : new List<InputDevice> { device };

			var actionMapInput = ActionMapInput.Create(map);

			// It's possible that there are no suitable control schemes for the device that is being initialized,
			// so ActionMapInput can't be marked active
			var successfulInitialization = false;
			if (actionMapInput.TryInitializeWithDevices(devices))
			{
				successfulInitialization = true;
			}
			else
			{
				// For two-handed tools, the single device won't work, so collect the devices from the action map
				devices = InputUtils.CollectInputDevicesFromActionMaps(new List<ActionMap>() { map });
				if (actionMapInput.TryInitializeWithDevices(devices))
					successfulInitialization = true;
			}

			if (successfulInitialization)
			{
				actionMapInput.autoReinitialize = false;
				// Resetting AMIs cause all AMIs (active or not) that use the same sources to be reset, which causes 
				// problems (e.g. dropping objects because wasJustPressed becomes true when reset)
				actionMapInput.resetOnActiveChanged = false;
				actionMapInput.active = true;
			}

			return actionMapInput;
		}

		public ActionMapInput CreateActionMapInputForObject(object obj, InputDevice device)
		{
			var customMap = obj as ICustomActionMap;
			if (customMap != null)
			{
				if (customMap is IStandardActionMap)
					Debug.LogWarning("Cannot use IStandardActionMap and ICustomActionMap together in " + obj.GetType());

				return CreateActionMapInput(customMap.actionMap, device);
			}

			var standardMap = obj as IStandardActionMap;
			if (standardMap != null)
				return CreateActionMapInput(m_StandardToolActionMap, device);

			return null;
		}

		// TODO: Order doesn't matter any more ostensibly, so let's simply add when AMIs are created
		public void UpdatePlayerHandleMaps()
		{
			var maps = m_PlayerHandle.maps;
			maps.Clear();

			if (updatePlayerHandleMaps != null)
				updatePlayerHandleMaps(maps);
		}

		static bool IsValidActionMapForDevice(ActionMap actionMap, InputDevice device)
		{
			var untaggedDevicesFound = 0;
			var taggedDevicesFound = 0;
			var nonMatchingTagIndices = 0;
			var matchingTagIndices = 0;

			if (actionMap == null)
				return false;

			foreach (var scheme in actionMap.controlSchemes)
			{
				foreach (var serializableDeviceType in scheme.deviceSlots)
				{
					if (serializableDeviceType.tagIndex != -1)
					{
						taggedDevicesFound++;
						if (serializableDeviceType.tagIndex != device.tagIndex)
							nonMatchingTagIndices++;
						else
							matchingTagIndices++;
					}
					else
					{
						untaggedDevicesFound++;
					}
				}
			}

			if (nonMatchingTagIndices > 0 && matchingTagIndices == 0)
			{
				LogError(string.Format("The action map {0} contains a specific device tag, but is being spawned on the wrong device tag", actionMap));
				return false;
			}

			if (taggedDevicesFound > 0 && untaggedDevicesFound != 0)
			{
				LogError(string.Format("The action map {0} contains both a specific device tag and an unspecified tag, which is not supported", actionMap.name));
				return false;
			}

			return true;
		}

		static void LogError(string error)
		{
			Debug.LogError(string.Format("DeviceInputModule: {0}", error));
		}

		void ConsumeControl(InputControl control)
		{
			// Consuming a control inherently locks it (for now), since consuming a control for one frame only might leave
			// another AMI to pick up a wasPressed the next frame, since it's own input would have been cleared. The
			// control is released when it returns to it's default value
			m_LockedControls.Add(control);

			var ami = control.provider as ActionMapInput;
			var playerHandleMaps = m_PlayerHandle.maps;
			for (int i = 0; i < playerHandleMaps.Count; i++)
			{
				var input = playerHandleMaps[i];
				if (input != ami)
					input.ResetControl(control);
			}
		}

		public Node? GetDeviceNode(InputDevice device)
		{
			string[] tags;

			var deviceType = device.GetType();
			if (!m_DeviceTypeTags.TryGetValue(deviceType, out tags))
			{
				tags = InputDeviceUtility.GetDeviceTags(deviceType);
				m_DeviceTypeTags[deviceType] = tags;
			}

			if (tags != null && device.tagIndex != -1)
			{
				var tag = tags[device.tagIndex];
				Node node;
				if (m_TagToNode.TryGetValue(tag, out node))
					return node;
			}

			return null;
		}
	}
}
#endif
