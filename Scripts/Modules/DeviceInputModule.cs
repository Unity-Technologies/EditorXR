using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Labs.ModuleLoader;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Modules
{
    sealed class DeviceInputModule : MonoBehaviour, IModuleDependency<Core.EditorVR>,
        IModuleDependency<EditorXRToolModule>, IInterfaceConnector
    {
        class InputProcessor
        {
            public IProcessInput processor;
            public ActionMapInput input;
            public int order;
        }

#pragma warning disable 649
        [SerializeField]
        ActionMap m_TrackedObjectActionMap;

        [SerializeField]
        ActionMap m_StandardToolActionMap;
#pragma warning restore 649

        PlayerHandle m_PlayerHandle;

        readonly HashSet<InputControl> m_LockedControls = new HashSet<InputControl>();
        readonly Dictionary<ActionMapInput, ICustomActionMap> m_IgnoreLocking = new Dictionary<ActionMapInput, ICustomActionMap>();

        readonly Dictionary<string, Node> m_TagToNode = new Dictionary<string, Node>
        {
            { "Left", Node.LeftHand },
            { "Right", Node.RightHand }
        };

        readonly List<InputProcessor> m_InputProcessors = new List<InputProcessor>();

        public TrackedObject trackedObjectInput { get; private set; }
        public Action<HashSet<IProcessInput>, ConsumeControlDelegate> processInput;
        public Action<List<ActionMapInput>> updatePlayerHandleMaps;
        public Func<Transform, InputDevice> inputDeviceForRayOrigin;

        // Local method use only -- created here to reduce garbage collection
        readonly HashSet<IProcessInput> m_ProcessedInputs = new HashSet<IProcessInput>();
        readonly List<InputDevice> m_SystemDevices = new List<InputDevice>();
        readonly Dictionary<Type, string[]> m_DeviceTypeTags = new Dictionary<Type, string[]>();
        readonly List<InputProcessor> m_InputProcessorsCopy = new List<InputProcessor>();
        readonly List<InputProcessor> m_RemoveInputProcessorsCopy = new List<InputProcessor>();
        static readonly List<InputControl> k_RemoveList = new List<InputControl>();
        ConsumeControlDelegate m_ConsumeControl;

        public void ConnectDependency(Core.EditorVR dependency)
        {
            processInput = dependency.ProcessInput;
            inputDeviceForRayOrigin = rayOrigin =>
                (from deviceData in dependency.deviceData
                    where deviceData.rayOrigin == rayOrigin
                    select deviceData.inputDevice).FirstOrDefault();
        }

        public void ConnectDependency(EditorXRToolModule dependency)
        {
            updatePlayerHandleMaps = dependency.UpdatePlayerHandleMaps;
        }

        public void LoadModule()
        {
            m_ConsumeControl = ConsumeControl;

            InitializePlayerHandle();
            CreateDefaultActionMapInputs();
            EditingContextManager.InitializeInputManager();
        }

        public void UnloadModule() { }

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

        /// <summary>
        /// Called in the EditorVR Update() function
        /// </summary>
        public void ProcessInput()
        {
            k_RemoveList.Clear();

            // Maintain a consumed control, so that other AMIs don't pick up the input, until it's no longer used
            foreach (var lockedControl in m_LockedControls)
            {
                if (!lockedControl.provider.active || Mathf.Approximately(lockedControl.rawValue,
                    lockedControl.provider.GetControlData(lockedControl.index).defaultValue))
                    k_RemoveList.Add(lockedControl);
                else
                    ConsumeControl(lockedControl);
            }

            // Remove separately, since we cannot remove while iterating
            foreach (var inputControl in k_RemoveList)
            {
                if (!inputControl.provider.active)
                    ResetControl(inputControl);

                m_LockedControls.Remove(inputControl);
            }

            k_RemoveList.Clear();
            m_ProcessedInputs.Clear();

            m_InputProcessorsCopy.Clear();
            m_InputProcessorsCopy.AddRange(m_InputProcessors);
            foreach (var processor in m_InputProcessorsCopy)
            {
                processor.processor.ProcessInput(processor.input, m_ConsumeControl);
            }

            if (processInput != null)
                processInput(m_ProcessedInputs, m_ConsumeControl);
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

        internal ActionMapInput CreateActionMapInputForObject(object obj, InputDevice device)
        {
            var customMap = obj as ICustomActionMap;
            if (customMap != null)
            {
                if (customMap is IStandardActionMap)
                    Debug.LogWarning("Cannot use IStandardActionMap and ICustomActionMap together in " + obj.GetType());

                var input = CreateActionMapInput(customMap.actionMap, device);
                if (customMap.ignoreActionMapInputLocking)
                    m_IgnoreLocking[input] = customMap;

                return input;
            }

            var standardMap = obj as IStandardActionMap;
            if (standardMap != null)
            {
                standardMap.standardActionMap = m_StandardToolActionMap;
                return CreateActionMapInput(m_StandardToolActionMap, device);
            }

            return null;
        }

        // TODO: Order doesn't matter any more ostensibly, so let's simply add when AMIs are created
        public void UpdatePlayerHandleMaps()
        {
            var maps = m_PlayerHandle.maps;
            maps.Clear();

            foreach (var processor in m_InputProcessors)
            {
                var input = processor.input;
                if (input != null)
                    maps.Add(input);
            }

            maps.Add(trackedObjectInput);

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
            ResetControl(control);
        }

        void ResetControl(InputControl control)
        {
            var ami = control.provider as ActionMapInput;
            var playerHandleMaps = m_PlayerHandle.maps;
            for (int i = 0; i < playerHandleMaps.Count; i++)
            {
                var input = playerHandleMaps[i];
                if (m_IgnoreLocking.ContainsKey(input))
                    continue;

                if (input != ami)
                    input.ResetControl(control);
            }
        }

        public Node GetDeviceNode(InputDevice device)
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

            return Node.None;
        }

        public void AddInputProcessor(IProcessInput processInput, object userData)
        {
            var rayOrigin = userData as Transform;
            var inputDevice = inputDeviceForRayOrigin(rayOrigin);
            var input = CreateActionMapInputForObject(processInput, inputDevice);

            var order = 0;
            var processInputAttribute = (ProcessInputAttribute)processInput.GetType().GetCustomAttributes(typeof(ProcessInputAttribute), true).FirstOrDefault();
            if (processInputAttribute != null)
                order = processInputAttribute.order;

            m_InputProcessors.Add(new InputProcessor { processor = processInput, input = input, order = order });
            m_InputProcessors.Sort((a, b) => b.order.CompareTo(a.order));
        }

        public void RemoveInputProcessor(IProcessInput processInput)
        {
            m_RemoveInputProcessorsCopy.Clear();
            m_RemoveInputProcessorsCopy.AddRange(m_InputProcessors);
            foreach (var processor in m_RemoveInputProcessorsCopy)
            {
                if (processor.processor == processInput)
                {
                    m_InputProcessors.Remove(processor);
                    var input = processor.input;
                    for (var i = 0; i < input.controlCount; i++)
                    {
                        m_LockedControls.Remove(input[i]);
                    }

                    var customActionMap = processInput as ICustomActionMap;
                    if (customActionMap != null)
                        m_IgnoreLocking.Remove(processor.input);
                }
            }
        }

        public void ConnectInterface(object target, object userData = null)
        {
            var trackedObjectMap = target as ITrackedObjectActionMap;
            if (trackedObjectMap != null)
                trackedObjectMap.trackedObjectInput = trackedObjectInput;

            var processInput = target as IProcessInput;
            if (processInput != null && !(target is ITool)) // Tools have their input processed separately
                AddInputProcessor(processInput, userData);
        }

        public void DisconnectInterface(object target, object userData = null)
        {
            var processInput = target as IProcessInput;
            if (processInput != null)
                RemoveInputProcessor(processInput);
        }
    }
}
