using System.Linq;
using System.Collections.Generic;
using UnityEngine.InputNew;

namespace Unity.Labs.EditorXR.Utilities
{
    class BindingDictionary : Dictionary<string, List<VRInputDevice.VRControl>>
    {
    }

    /// <summary>
    /// Input related EditorXR utilities
    /// </summary>
    static class InputUtils
    {
        public static List<InputDevice> CollectInputDevicesFromActionMaps(List<ActionMap> maps)
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
            return inputDevices.ToList();
        }

        public static void CollectDeviceSlotsFromActionMapInput(ActionMapInput actionMapInput, ref HashSet<DeviceSlot> deviceSlots)
        {
            foreach (var deviceSlot in actionMapInput.controlScheme.deviceSlots)
            {
                deviceSlots.Add(deviceSlot);
            }
        }

        public static void GetBindingDictionaryFromActionMap(ActionMap actionMap, BindingDictionary bindingDictionary)
        {
            var actions = actionMap.actions;
            foreach (var scheme in actionMap.controlSchemes)
            {
                var bindings = scheme.bindings;
                for (var i = 0; i < bindings.Count; i++)
                {
                    var binding = bindings[i];
                    var action = actions[i].name;
                    List<VRInputDevice.VRControl> controls;
                    if (!bindingDictionary.TryGetValue(action, out controls))
                    {
                        controls = new List<VRInputDevice.VRControl>();
                        bindingDictionary[action] = controls;
                    }

                    foreach (var source in binding.sources)
                    {
                        bindingDictionary[action].Add((VRInputDevice.VRControl)source.controlIndex);
                    }
                }
            }
        }
    }
}
