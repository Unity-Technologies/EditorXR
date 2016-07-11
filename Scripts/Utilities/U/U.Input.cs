namespace UnityEngine.VR.Utilities
{
	using System.Collections.Generic;
	using UnityEngine.InputNew;

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
			public static HashSet<InputDevice> CollectInputDevicesFromActionMaps(List<ActionMap> maps)
			{
				var inputDevices = new HashSet<InputDevice>();
				var systemDevices = InputSystem.devices;

				foreach (var map in maps)
				{
					foreach (var scheme in map.controlSchemes)
					{
						foreach (var deviceType in scheme.serializableDeviceTypes)
						{
							foreach (var systemDevice in systemDevices)
							{
								if (systemDevice.GetType() == deviceType.value &&
									(deviceType.TagIndex == -1 || deviceType.TagIndex == systemDevice.TagIndex))
								{
									inputDevices.Add(systemDevice);
								}
							}
						}
					}
				}
				return inputDevices;
			}

			public static void CollectSerializableTypesFromActionMapInput(ActionMapInput actionMapInput, ref HashSet<SerializableType> types)
			{
				foreach (var deviceType in actionMapInput.controlScheme.serializableDeviceTypes)
				{
					types.Add(deviceType);
				}
			}
		}
	}
}