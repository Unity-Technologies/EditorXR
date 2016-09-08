using System;
using UnityEngine.VR.Handles;
using UnityEngine.VR.Modules;

namespace UnityEngine.VR.Utilities
{
	using System.Collections.Generic;
	using UnityEngine.InputNew;

	[Flags]
	public enum HandleFlags
	{
		Ray = 1 << 0,
		Direct = 1 << 1
	}

	/// <summary>
	/// EditorVR Utilities
	/// </summary>
	public partial class U
	{
		/// <summary>
		/// Input related EditorVR utilities
		/// </summary>
		public class Input
		{
			private const float kDoubleClickIntervalMax = 0.3f;
			private const float kDoubleClickIntervalMin = 0.15f;

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

			public static bool DoubleClick(float timeSinceLastClick)
			{
				return timeSinceLastClick <= kDoubleClickIntervalMax && timeSinceLastClick >= kDoubleClickIntervalMin;
			}

			public static bool IsDirectEvent(RayEventData eventData)
			{
				return eventData.pointerCurrentRaycast.isValid && eventData.pointerCurrentRaycast.distance <= eventData.pointerLength;
			}

			public static bool IsValidEvent(RayEventData eventData, HandleFlags handleFlags)
			{
				if ((handleFlags & HandleFlags.Direct) != 0 && IsDirectEvent(eventData))
					return true;

				if ((handleFlags & HandleFlags.Ray) != 0)
					return true;

				return false;
			}
		}
	}
}