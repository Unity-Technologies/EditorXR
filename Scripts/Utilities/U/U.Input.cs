namespace UnityEngine.Experimental.EditorVR.Utilities
{
	using System.Collections.Generic;
	using UnityEngine.InputNew;

	/// <summary>
	/// EditorVR Utilities
	/// </summary>
	public static partial class U
	{
		/// <summary>
		/// Input related EditorVR utilities
		/// </summary>
		public static class Input
		{
			public static HashSet<InputDevice> CollectInputDevicesFromActionMaps(List<ActionMap> maps)
			{
				var inputDevices = new HashSet<InputDevice>();
				var systemDevices = InputSystem.devices;

				foreach (var map in maps)
				{
					foreach (var scheme in map.controlSchemes)
					{
						foreach (var deviceSlot in scheme.deviceSlots)
						{
							foreach (var systemDevice in systemDevices)
							{
								if (systemDevice.GetType() == deviceSlot.type.value &&
									(deviceSlot.tagIndex == -1 || deviceSlot.tagIndex == systemDevice.tagIndex))
								{
									inputDevices.Add(systemDevice);
								}
							}
						}
					}
				}
				return inputDevices;
			}

			public static void CollectDeviceSlotsFromActionMapInput(ActionMapInput actionMapInput, ref HashSet<DeviceSlot> deviceSlots)
			{
				foreach (var deviceSlot in actionMapInput.controlScheme.deviceSlots)
				{
					deviceSlots.Add(deviceSlot);
				}
			}
		}
	}
}